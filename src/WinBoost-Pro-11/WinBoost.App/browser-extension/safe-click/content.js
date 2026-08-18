// WinBoost Safe Click
// Stable protection + Extreme Overlay Wrapper Guard.
//
// Protectii:
// 1. Comunicarea cu page-guard.js pentru popup/new-tab.
// 2. Overlay-uri transparente care fura clickul.
// 3. Wrapper-uri publicitare extreme:
//    position: fixed + z-index foarte mare + iframe.
//
// IMPORTANT:
// Reclama ramane vizibila.
// Doar nu mai poate intercepta clickurile.

(() => {
    "use strict";

    let safeClickEnabled = true;
    let trustedSites = [];

    const EXTENSION_SOURCE =
        "WINBOOST_SAFE_CLICK_EXTENSION";

    const PAGE_SOURCE =
        "WINBOOST_SAFE_CLICK_PAGE";

    const EXTREME_Z_INDEX =
        2000000000;

    const CLICKABLE_SELECTOR = [
        "a[href]",
        "button",
        "input[type='button']",
        "input[type='submit']",
        "input[type='image']",
        "[role='button']",
        "[onclick]"
    ].join(",");

    // Pastram stilul original ca sa-l putem
    // restaura daca Safe Click este dezactivat.
    const neutralizedWrappers =
        new Map();

    // ---------------------------------------------
    // EXTENSION CONTEXT
    // ---------------------------------------------

    function isExtensionContextAlive() {
        try {
            return Boolean(
                chrome &&
                chrome.runtime &&
                chrome.runtime.id
            );
        }
        catch {
            return false;
        }
    }

    // ---------------------------------------------
    // SETTINGS
    // ---------------------------------------------

    function loadSettings() {

        if (!isExtensionContextAlive()) {
            return;
        }

        try {
            chrome.runtime.sendMessage(
                {
                    type:
                        "GET_SAFE_CLICK_SETTINGS"
                },
                (response) => {

                    if (
                        chrome.runtime.lastError ||
                        !response
                    ) {
                        return;
                    }

                    safeClickEnabled =
                        response.safeClickEnabled !==
                        false;

                    trustedSites =
                        Array.isArray(
                            response.trustedSites
                        )
                            ? response.trustedSites
                            : [];

                    sendProtectionState();

                    if (
                        safeClickEnabled &&
                        !isTrustedSite()
                    ) {
                        scanExistingWrappers();
                    }
                }
            );
        }
        catch {
            // Context vechi dupa Reload.
            // Refresh-ul paginii va incarca
            // noul content script.
        }
    }

    function isTrustedSite() {

        const host =
            window.location.hostname
                .toLowerCase();

        return trustedSites.some(
            (site) => {

                const trusted =
                    String(site)
                        .toLowerCase()
                        .trim();

                return (
                    host === trusted ||
                    host.endsWith(
                        "." + trusted
                    )
                );
            }
        );
    }

    function sendProtectionState() {

        window.postMessage(
            {
                source:
                    EXTENSION_SOURCE,

                type:
                    "STATE",

                enabled:
                    safeClickEnabled,

                trusted:
                    isTrustedSite()
            },
            "*"
        );
    }

    // ---------------------------------------------
    // REPORTING
    // ---------------------------------------------

    function reportBlockedAction() {

        if (!isExtensionContextAlive()) {
            return;
        }

        try {
            chrome.runtime.sendMessage(
                {
                    type:
                        "SAFE_CLICK_BLOCKED"
                },
                () => {
                    void chrome.runtime.lastError;
                }
            );
        }
        catch {
            // Ignoram contextul vechi
            // dupa Reload.
        }
    }

    // ---------------------------------------------
    // GENERIC HELPERS
    // ---------------------------------------------

    function isVisible(element) {

        if (!(element instanceof Element)) {
            return false;
        }

        const style =
            window.getComputedStyle(
                element
            );

        const rect =
            element.getBoundingClientRect();

        return (
            style.display !== "none" &&
            style.visibility !== "hidden" &&
            rect.width > 4 &&
            rect.height > 4
        );
    }

    function getClickableElement(
        element) {

        if (!(element instanceof Element)) {
            return null;
        }

        return element.closest(
            CLICKABLE_SELECTOR
        );
    }

    function getNumericZIndex(
        element) {

        const style =
            window.getComputedStyle(
                element
            );

        const value =
            Number.parseInt(
                style.zIndex,
                10
            );

        return Number.isFinite(value)
            ? value
            : 0;
    }

    // ---------------------------------------------
    // SECURITY EXCLUSIONS
    // ---------------------------------------------

    function getElementHints(element) {

        if (!(element instanceof Element)) {
            return "";
        }

        return [
            element.id,
            element.className,
             element.getAttribute("src"),
             element.getAttribute("title"),
            element.getAttribute("name"),
            element.getAttribute(
                "aria-label"
            )
        ]
            .filter(
                (value) =>
                    typeof value === "string"
            )
            .join(" ")
            .toLowerCase();
    }

    function isSecurityRelated(
        wrapper) {

        const securityWords = [
            "captcha",
            "recaptcha",
            "hcaptcha",
            "turnstile",
            "cloudflare",
            "challenge",
            "verification",
            "authentication",
            "login",
            "signin",
            "sign-in",
            "checkout",
            "payment",
            "paypal",
            "stripe"
        ];

        let combinedHints =
            getElementHints(wrapper);

        const frames =
            wrapper.querySelectorAll(
                "iframe"
            );

        for (const frame of frames) {

            combinedHints += " " + [
                frame.getAttribute("src"),
                frame.getAttribute("title"),
                frame.getAttribute("name"),
                frame.getAttribute("id"),
                frame.getAttribute("class")
            ]
                .filter(Boolean)
                .join(" ")
                .toLowerCase();
        }

        return securityWords.some(
            (word) =>
                combinedHints.includes(
                    word
                )
        );
    }

    // ---------------------------------------------
    // EXTREME OVERLAY WRAPPER GUARD
    // ---------------------------------------------

    function isExtremeOverlayWrapper(
        element) {

        if (
            !(element instanceof
                HTMLElement)
        ) {
            return false;
        }

        if (
            element === document.body ||
            element ===
                document.documentElement
        ) {
            return false;
        }

        if (
            neutralizedWrappers.has(
                element
            )
        ) {
            return false;
        }

        if (!isVisible(element)) {
            return false;
        }

       const isIframe =
    element instanceof HTMLIFrameElement;

const iframe =
    isIframe
        ? element
        : element.querySelector("iframe");

if (!iframe) {
    return false;
}

        if (
            isSecurityRelated(
                element
            )
        ) {
            return false;
        }

        const style =
            window.getComputedStyle(
                element
            );

        // Exact cazul gasit prin DevTools:
        // position: fixed
        if (
            style.position !== "fixed"
        ) {
            return false;
        }

        const zIndex =
            getNumericZIndex(
                element
            );

        // Exemplul real avea:
        // 2147483647
        if (
            zIndex <
            EXTREME_Z_INDEX
        ) {
            return false;
        }

        const rect =
            element.getBoundingClientRect();

        const viewportArea =
            Math.max(
                1,
                window.innerWidth *
                window.innerHeight
            );

        const elementArea =
            rect.width *
            rect.height;

        const areaRatio =
            elementArea /
            viewportArea;

        // Evitam widgeturi mici aflate
        // intr-un colt al ecranului.
        if (areaRatio < 0.10) {
            return false;
        }

        return true;
    }

    function neutralizeWrapper(
        element) {

        if (
            !isExtremeOverlayWrapper(
                element
            )
        ) {
            return false;
        }

        const originalValue =
            element.style
                .getPropertyValue(
                    "pointer-events"
                );

        const originalPriority =
            element.style
                .getPropertyPriority(
                    "pointer-events"
                );

        neutralizedWrappers.set(
            element,
            {
                value:
                    originalValue,

                priority:
                    originalPriority
            }
        );

        // Aceasta este comanda pe care
        // am testat-o manual in DevTools.
        element.style.setProperty(
            "pointer-events",
            "none",
            "important"
        );

        reportBlockedAction();

        console.log(
            "[WinBoost Safe Click] " +
            "Extreme overlay wrapper " +
            "made click-through.",
            {
                element:
                    element,

                zIndex:
                    getNumericZIndex(
                        element
                    )
            }
        );

        return true;
    }

    function restoreWrapper(
        element) {

        const original =
            neutralizedWrappers.get(
                element
            );

        if (!original) {
            return;
        }

        if (original.value) {

            element.style.setProperty(
                "pointer-events",
                original.value,
                original.priority || ""
            );
        }
        else {

            element.style.removeProperty(
                "pointer-events"
            );
        }

        neutralizedWrappers.delete(
            element
        );
    }

    function restoreAllWrappers() {

        for (
            const element
            of Array.from(
                neutralizedWrappers.keys()
            )
        ) {
            restoreWrapper(
                element
            );
        }
    }

    function inspectElement(
        element) {

        if (
            !safeClickEnabled ||
            isTrustedSite()
        ) {
            return;
        }

        if (!(element instanceof Element)) {
            return;
        }

        neutralizeWrapper(
            element
        );

        // Daca iframe-ul a fost adaugat
        // intr-un wrapper existent,
        // verificam si parintii.
        let parent =
            element.parentElement;

        let depth = 0;

        while (
            parent &&
            depth < 6
        ) {

            if (
                neutralizeWrapper(
                    parent
                )
            ) {
                break;
            }

            parent =
                parent.parentElement;

            depth++;
        }
    }

    function inspectAddedTree(
        root) {

        if (!(root instanceof Element)) {
            return;
        }

        inspectElement(root);

        const frames =
            root.querySelectorAll(
                "iframe"
            );

        for (
            const frame
            of frames
        ) {
            inspectElement(
                frame
            );
        }
    }

    function scanExistingWrappers() {

        if (
            !safeClickEnabled ||
            isTrustedSite()
        ) {
            return;
        }

        const frames =
            document.querySelectorAll(
                "iframe"
            );

        for (
            const frame
            of frames
        ) {
            inspectElement(
                frame
            );
        }
    }

    // ---------------------------------------------
    // DOM OBSERVER
    // ---------------------------------------------

    const overlayObserver =
        new MutationObserver(
            (mutations) => {

                if (
                    !safeClickEnabled ||
                    isTrustedSite()
                ) {
                    return;
                }

                for (
                    const mutation
                    of mutations
                ) {

                    inspectElement(
                        mutation.target
                    );

                    for (
                        const node
                        of mutation.addedNodes
                    ) {
                        inspectAddedTree(
                            node
                        );
                    }
                }
            }
        );

    function startOverlayObserver() {

        if (
            !document.documentElement
        ) {
            return;
        }

        overlayObserver.observe(
            document.documentElement,
            {
                childList: true,
                subtree: true,
                attributes: true,
                attributeFilter: [
                    "style",
                    "class"
                ]
            }
        );

        scanExistingWrappers();
    }

    // ---------------------------------------------
    // TRANSPARENT CLICK OVERLAY GUARD
    // ---------------------------------------------

    function hasVisibleContent(
        element) {

        const text =
            (
                element.innerText ||
                ""
            ).trim();

        if (text.length > 0) {
            return true;
        }

        return Boolean(
            element.querySelector(
                "img, svg, video, canvas"
            )
        );
    }

    function hasTransparentBackground(
        element) {

        const style =
            window.getComputedStyle(
                element
            );

        const background =
            style.backgroundColor;

        const opacity =
            Number.parseFloat(
                style.opacity
            );

        const transparentBackground =
            background ===
                "transparent" ||
            background ===
                "rgba(0, 0, 0, 0)";

        return (
            opacity <= 0.15 ||
            (
                transparentBackground &&
                !hasVisibleContent(
                    element
                )
            )
        );
    }

    function isOverlayCandidate(
        overlay,
        elementBelow) {

        if (
            !overlay ||
            !elementBelow ||
            overlay === elementBelow
        ) {
            return false;
        }

        if (
            !isVisible(overlay) ||
            !isVisible(elementBelow)
        ) {
            return false;
        }

        const style =
            window.getComputedStyle(
                overlay
            );

        const positioned =
            style.position === "fixed" ||
            style.position === "absolute";

        if (!positioned) {
            return false;
        }

        if (
            !hasTransparentBackground(
                overlay
            )
        ) {
            return false;
        }

        const overlayRect =
            overlay.getBoundingClientRect();

        const belowRect =
            elementBelow
                .getBoundingClientRect();

        const overlayArea =
            overlayRect.width *
            overlayRect.height;

        const belowArea =
            belowRect.width *
            belowRect.height;

        const viewportArea =
            Math.max(
                1,
                window.innerWidth *
                window.innerHeight
            );

        const coversLargeArea =
            overlayArea >=
            viewportArea * 0.20;

        const coversClickedElement =
            overlayArea >=
            belowArea * 0.80;

        return (
            coversLargeArea ||
            coversClickedElement
        );
    }

    function findClickLayers(
        x,
        y) {

        const elements =
            document.elementsFromPoint(
                x,
                y
            );

        const clickables = [];

        for (
            const element
            of elements
        ) {

            const clickable =
                getClickableElement(
                    element
                );

            if (!clickable) {
                continue;
            }

            if (
                !clickables.includes(
                    clickable
                )
            ) {
                clickables.push(
                    clickable
                );
            }
        }

        return clickables;
    }

    function activateElement(
        element) {

        if (
            !(element instanceof
                HTMLElement)
        ) {
            return;
        }

        setTimeout(
            () => {
                element.click();
            },
            0
        );
    }

// ---------------------------------------------
// REAL USER CLICK INTENT
// ---------------------------------------------

document.addEventListener(
    "pointerdown",
    (event) => {

        if (!event.isTrusted) {
            return;
        }

        if (
            !safeClickEnabled ||
            isTrustedSite() ||
            !isExtensionContextAlive()
        ) {
            return;
        }

        const clickable =
            getClickableElement(
                event.target
            );

        let expectedUrl = "";

        if (
            clickable instanceof
            HTMLAnchorElement
        ) {
            expectedUrl =
                clickable.href || "";
        }

        try {
            chrome.runtime.sendMessage(
                {
                    type:
                        "SAFE_CLICK_USER_INTENT",

                    sourceUrl:
                        window.location.href,

                    expectedUrl:
                        expectedUrl
                },
                () => {
                    void chrome.runtime
                        .lastError;
                }
            );
        }
        catch {
            // Context vechi după Reload.
        }
    },
    true
);

    document.addEventListener(
        "click",
        (event) => {

            if (!event.isTrusted) {
                return;
            }

            if (
                !safeClickEnabled ||
                isTrustedSite()
            ) {
                return;
            }

            const clickables =
                findClickLayers(
                    event.clientX,
                    event.clientY
                );

            if (
                clickables.length < 2
            ) {
                return;
            }

            const topElement =
                clickables[0];

            const elementBelow =
                clickables[1];

            if (
                !isOverlayCandidate(
                    topElement,
                    elementBelow
                )
            ) {
                return;
            }

            event.preventDefault();
            event.stopPropagation();
            event.stopImmediatePropagation();

            reportBlockedAction();

            console.log(
                "[WinBoost Safe Click] " +
                "Suspicious transparent " +
                "overlay blocked."
            );

            activateElement(
                elementBelow
            );
        },
        true
    );

    // ---------------------------------------------
    // PAGE-GUARD COMMUNICATION
    // ---------------------------------------------

window.addEventListener(
    "message",
    (event) => {

        if (
            event.source !== window
        ) {
            return;
        }

        const data =
            event.data;

        if (
            !data ||
            data.source !==
                PAGE_SOURCE ||
            data.type !==
                "BLOCKED"
        ) {
            return;
        }

        reportBlockedAction();

        // Dacă page-guard.js tocmai a blocat
        // o tentativă către alt URL,
        // protejăm temporar același domeniu
        // și împotriva redirectului în tabul curent.
        if (
            typeof data.url === "string" &&
            data.url.trim().length > 0 &&
            isExtensionContextAlive()
        ) {
            try {

                chrome.runtime.sendMessage(
                    {
                        type:
                            "SAFE_CLICK_TEMP_BLOCK",

                        url:
                            data.url
                    },
                    (response) => {

                        void chrome.runtime
                            .lastError;

                        if (
                            response?.success
                        ) {
                            console.log(
                                "[WinBoost Safe Click] " +
                                "Temporary redirect " +
                                "guard armed:",
                                response.domain
                            );
                        }
                    }
                );
            }
            catch {
                // Context vechi după Reload.
            }
        }

        console.log(
            "[WinBoost Safe Click] " +
            "Blocked page action:",
            data.kind,
            data.url
        );

        scanExistingWrappers();
    }
);

    // ---------------------------------------------
    // INITIALIZATION
    // ---------------------------------------------

    loadSettings();

    if (document.documentElement) {
        startOverlayObserver();
    }
    else {
        document.addEventListener(
            "DOMContentLoaded",
            startOverlayObserver,
            {
                once: true
            }
        );
    }

    try {
        chrome.storage.onChanged
            .addListener(
                (
                    changes,
                    areaName
                ) => {

                    if (
                        areaName !== "local"
                    ) {
                        return;
                    }

                    if (
                        changes
                            .safeClickEnabled
                    ) {

                        safeClickEnabled =
                            changes
                                .safeClickEnabled
                                .newValue !== false;
                    }

                    if (
                        changes.trustedSites
                    ) {

                        trustedSites =
                            Array.isArray(
                                changes
                                    .trustedSites
                                    .newValue
                            )
                                ? changes
                                    .trustedSites
                                    .newValue
                                : [];
                    }

                    sendProtectionState();

                    if (
                        safeClickEnabled &&
                        !isTrustedSite()
                    ) {
                        scanExistingWrappers();
                    }
                    else {
                        restoreAllWrappers();
                    }
                }
            );
    }
    catch {
        // Context vechi dupa Reload.
    }
})();