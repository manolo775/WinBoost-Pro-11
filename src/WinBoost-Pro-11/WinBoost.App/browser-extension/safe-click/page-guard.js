// WinBoost Safe Click
// MAIN world protection against unwanted popup windows.

(() => {
    "use strict";

    const EXTENSION_SOURCE =
        "WINBOOST_SAFE_CLICK_EXTENSION";

    const PAGE_SOURCE =
        "WINBOOST_SAFE_CLICK_PAGE";

    let protectionEnabled = true;
    let trustedSite = false;

    function reportBlocked(
        kind,
        url) {

        window.postMessage(
            {
                source: PAGE_SOURCE,
                type: "BLOCKED",
                kind: kind,
                url:
                    typeof url === "string"
                        ? url
                        : ""
            },
            "*"
        );
    }

    window.addEventListener(
        "message",
        (event) => {

            if (event.source !== window) {
                return;
            }

            const data = event.data;

            if (!data ||
                data.source !== EXTENSION_SOURCE ||
                data.type !== "STATE") {
                return;
            }

            protectionEnabled =
                data.enabled !== false;

            trustedSite =
                data.trusted === true;
        }
    );

    const originalWindowOpen =
        window.open.bind(window);

    function safeWindowOpen(
        url,
        target,
        features) {

        if (!protectionEnabled ||
            trustedSite) {

            return originalWindowOpen(
                url,
                target,
                features
            );
        }

        reportBlocked(
            "window.open",
            url
        );

        console.log(
            "[WinBoost Safe Click] " +
            "Blocked unwanted window.open:",
            url
        );

        return null;
    }

    try {
        Object.defineProperty(
            window,
            "open",
            {
                value: safeWindowOpen,
                writable: false,
                configurable: false
            }
        );
    }
    catch {
        window.open =
            safeWindowOpen;
    }

    const originalAnchorClick =
        HTMLAnchorElement
            .prototype
            .click;

    HTMLAnchorElement
        .prototype
        .click =
        function (...args) {

            if (protectionEnabled &&
                !trustedSite &&
                this.target
                    ?.toLowerCase() ===
                    "_blank") {

                reportBlocked(
                    "programmatic-link",
                    this.href
                );

                console.log(
                    "[WinBoost Safe Click] " +
                    "Blocked programmatic " +
                    "new-tab link:",
                    this.href
                );

                return;
            }

            return originalAnchorClick
                .apply(
                    this,
                    args
                );
        };
})();