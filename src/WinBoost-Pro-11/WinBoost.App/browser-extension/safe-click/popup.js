// WinBoost Safe Click
// Popup logic.

document.addEventListener(
    "DOMContentLoaded",
    () => {

        const protectionToggle =
            document.getElementById(
                "protectionToggle"
            );

        const statusText =
            document.getElementById(
                "statusText"
            );

        const blockedCount =
            document.getElementById(
                "blockedCount"
            );

        function updateStatusText(
            enabled) {

            statusText.textContent =
                enabled
                    ? "Protection enabled"
                    : "Protection disabled";
        }

        chrome.storage.local.get(
            {
                safeClickEnabled: true,
                blockedCount: 0
            },
            (result) => {

                const enabled =
                    result.safeClickEnabled
                        !== false;

                protectionToggle.checked =
                    enabled;

                updateStatusText(
                    enabled
                );

                blockedCount.textContent =
                    result.blockedCount ?? 0;
            }
        );

        protectionToggle.addEventListener(
            "change",
            () => {

                const enabled =
                    protectionToggle.checked;

                chrome.storage.local.set({
                    safeClickEnabled:
                        enabled
                });

                updateStatusText(
                    enabled
                );
            }
        );

        chrome.storage.onChanged.addListener(
            (changes, areaName) => {

                if (areaName !== "local") {
                    return;
                }

                if (changes.blockedCount) {
                    blockedCount.textContent =
                        changes.blockedCount
                            .newValue ?? 0;
                }

                if (changes.safeClickEnabled) {

                    const enabled =
                        changes.safeClickEnabled
                            .newValue !== false;

                    protectionToggle.checked =
                        enabled;

                    updateStatusText(
                        enabled
                    );
                }
            }
        );
    }
);