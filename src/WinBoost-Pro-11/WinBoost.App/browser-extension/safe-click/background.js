// WinBoost Safe Click
// Background service worker.

const TEMP_RULE_PREFIX =
    "winboost-safe-click-rule-";

const TEMP_RULE_DURATION_MS =
    30000;

const TEMP_RULE_START_ID =
    10000;
    const RECENT_CLICK_WINDOW_MS =
    1500;

const recentUserClicks =
    new Map();


// ---------------------------------------------
// INITIAL SETTINGS
// ---------------------------------------------

chrome.runtime.onInstalled.addListener(
    () => {

        chrome.storage.local.get(
            [
                "safeClickEnabled",
                "trustedSites",
                "blockedCount"
            ],
            (result) => {

                const defaults = {};

                if (
                    typeof result.safeClickEnabled !==
                    "boolean"
                ) {
                    defaults.safeClickEnabled = true;
                }

                if (
                    !Array.isArray(
                        result.trustedSites
                    )
                ) {
                    defaults.trustedSites = [];
                }

                if (
                    typeof result.blockedCount !==
                    "number"
                ) {
                    defaults.blockedCount = 0;
                }

                if (
                    Object.keys(defaults).length > 0
                ) {
                    chrome.storage.local.set(
                        defaults
                    );
                }
            }
        );
    }
);


// ---------------------------------------------
// BLOCKED ACTION COUNTER
// ---------------------------------------------

function incrementBlockedCount() {

    chrome.storage.local.get(
        ["blockedCount"],
        (result) => {

            const currentCount =
                typeof result.blockedCount ===
                "number"
                    ? result.blockedCount
                    : 0;

            chrome.storage.local.set({
                blockedCount:
                    currentCount + 1
            });
        }
    );
}


// ---------------------------------------------
// TEMPORARY SAME-TAB REDIRECT GUARD
// ---------------------------------------------

function getHttpUrl(
    value,
    baseUrl = undefined) {

    try {

        const url =
            baseUrl
                ? new URL(
                    value,
                    baseUrl
                )
                : new URL(value);

        if (
            url.protocol !== "http:" &&
            url.protocol !== "https:"
        ) {
            return null;
        }

        return url;
    }
    catch {
        return null;
    }
}


async function createTemporaryRedirectGuard(
    targetValue,
    sender) {

    const tabId =
        sender?.tab?.id;

    const sourceValue =
        sender?.tab?.url;

    if (
        !Number.isInteger(tabId) ||
        !sourceValue
    ) {
        return {
            success: false,
            reason: "Tab information unavailable."
        };
    }

    const sourceUrl =
        getHttpUrl(sourceValue);

    if (!sourceUrl) {
        return {
            success: false,
            reason: "Source URL is unavailable."
        };
    }

    const targetUrl =
        getHttpUrl(
            targetValue,
            sourceUrl.href
        );

    if (!targetUrl) {
        return {
            success: false,
            reason: "Target URL is unavailable."
        };
    }

    // Nu blocam navigarea legitima
    // in interiorul aceluiasi domeniu.
    if (
        targetUrl.hostname ===
        sourceUrl.hostname
    ) {
        return {
            success: false,
            reason: "Same-domain navigation."
        };
    }

    const targetDomain =
        targetUrl.hostname
            .toLowerCase();

    const rules =
        await chrome
            .declarativeNetRequest
            .getSessionRules();

    let existingRule =
        rules.find(
            (rule) =>
                rule.condition
                    ?.requestDomains
                    ?.includes(
                        targetDomain
                    ) &&
                rule.condition
                    ?.tabIds
                    ?.includes(
                        tabId
                    )
        );

    let ruleId;

    if (existingRule) {

        ruleId =
            existingRule.id;

        // Refacem regula folosind
        // URL-ul curent al paginii.
        await chrome
            .declarativeNetRequest
            .updateSessionRules({
                removeRuleIds: [
                    ruleId
                ],
                addRules: [
                    {
                        id: ruleId,
                        priority: 1,

                        action: {
                            type: "redirect",

                            redirect: {
                                url:
                                    sourceUrl.href
                            }
                        },

                        condition: {
                            requestDomains: [
                                targetDomain
                            ],

                            resourceTypes: [
                                "main_frame"
                            ],

                            tabIds: [
                                tabId
                            ]
                        }
                    }
                ]
            });
    }
    else {

        const highestRuleId =
            rules.reduce(
                (
                    highest,
                    rule
                ) =>
                    Math.max(
                        highest,
                        rule.id
                    ),
                TEMP_RULE_START_ID - 1
            );

        ruleId =
            highestRuleId + 1;

        await chrome
            .declarativeNetRequest
            .updateSessionRules({
                addRules: [
                    {
                        id: ruleId,
                        priority: 1,

                        action: {
                            type: "redirect",

                            redirect: {
                                url:
                                    sourceUrl.href
                            }
                        },

                        condition: {
                            requestDomains: [
                                targetDomain
                            ],

                            resourceTypes: [
                                "main_frame"
                            ],

                            tabIds: [
                                tabId
                            ]
                        }
                    }
                ]
            });
    }

    const alarmName =
        TEMP_RULE_PREFIX +
        ruleId;

    await chrome.alarms.create(
        alarmName,
        {
            when:
                Date.now() +
                TEMP_RULE_DURATION_MS
        }
    );

    console.log(
        "[WinBoost Safe Click] " +
        "Temporary redirect guard:",
        {
            tabId:
                tabId,

            blockedDomain:
                targetDomain,

            returnUrl:
                sourceUrl.href,

            durationSeconds:
                30
        }
    );

    return {
        success: true,
        domain: targetDomain
    };
}


// ---------------------------------------------
// REMOVE EXPIRED RULE
// ---------------------------------------------

chrome.alarms.onAlarm.addListener(
    async (alarm) => {

        if (
            !alarm.name.startsWith(
                TEMP_RULE_PREFIX
            )
        ) {
            return;
        }

        const ruleId =
            Number.parseInt(
                alarm.name.substring(
                    TEMP_RULE_PREFIX.length
                ),
                10
            );

        if (
            !Number.isInteger(ruleId)
        ) {
            return;
        }

        try {

            await chrome
                .declarativeNetRequest
                .updateSessionRules({
                    removeRuleIds: [
                        ruleId
                    ]
                });

            console.log(
                "[WinBoost Safe Click] " +
                "Temporary redirect guard expired:",
                ruleId
            );
        }
        catch {
            // Regula poate sa nu mai existe
            // dupa inchiderea browserului.
        }
    }
);


// ---------------------------------------------
// MESSAGES
// ---------------------------------------------

chrome.runtime.onMessage.addListener(
    (
        message,
        sender,
        sendResponse
    ) => {

    if (
    message?.type ===
    "SAFE_CLICK_USER_INTENT"
) {

    const tabId =
        sender?.tab?.id;

    if (
        Number.isInteger(tabId)
    ) {
        recentUserClicks.set(
            tabId,
            {
                time:
                    Date.now(),

                sourceUrl:
                    message.sourceUrl ?? "",

                expectedUrl:
                    message.expectedUrl ?? ""
            }
        );
    }

    sendResponse({
        success: true
    });

    return;
}

        if (
            message?.type ===
            "SAFE_CLICK_BLOCKED"
        ) {

            incrementBlockedCount();

            sendResponse({
                success: true
            });

            return;
        }


        if (
            message?.type ===
            "GET_SAFE_CLICK_SETTINGS"
        ) {

            chrome.storage.local.get(
                {
                    safeClickEnabled: true,
                    trustedSites: [],
                    blockedCount: 0
                },
                (result) => {

                    sendResponse(
                        result
                    );
                }
            );

            return true;
        }


        if (
            message?.type ===
            "SAFE_CLICK_TEMP_BLOCK"
        ) {

            createTemporaryRedirectGuard(
                message.url,
                sender
            )
                .then(
                    (result) => {
                        sendResponse(
                            result
                        );
                    }
                )
                .catch(
                    (error) => {

                        console.error(
                            "[WinBoost Safe Click] " +
                            "Redirect guard error:",
                            error
                        );

                        sendResponse({
                            success: false,
                            reason:
                                error?.message ??
                                "Unknown error."
                        });
                    }
                );

            return true;
        }
    }
);

chrome.webNavigation
    .onCreatedNavigationTarget
    .addListener(
        async (details) => {

        await new Promise(
    (resolve) =>
        setTimeout(resolve, 200)
);

            const clickInfo =
                recentUserClicks.get(
                    details.sourceTabId
                );

            if (!clickInfo) {
                return;
            }

            const elapsed =
                Date.now() -
                clickInfo.time;

            if (
                elapsed >
                RECENT_CLICK_WINDOW_MS
            ) {
                recentUserClicks.delete(
                    details.sourceTabId
                );

                return;
            }

            const sourceUrl =
                getHttpUrl(
                    clickInfo.sourceUrl
                );

            const targetUrl =
                getHttpUrl(
                    details.url
                );

            if (
                !sourceUrl ||
                !targetUrl
            ) {
                return;
            }

            const expectedUrl =
                clickInfo.expectedUrl
                    ? getHttpUrl(
                        clickInfo.expectedUrl,
                        sourceUrl.href
                    )
                    : null;

            let unwantedTarget =
                false;

            if (expectedUrl) {

                unwantedTarget =
                    targetUrl.hostname !==
                    expectedUrl.hostname;
            }
            else {

                unwantedTarget =
                    targetUrl.hostname !==
                    sourceUrl.hostname;
            }

            if (!unwantedTarget) {

                recentUserClicks.delete(
                    details.sourceTabId
                );

                return;
            }

            try {

                await chrome.tabs.remove(
                    details.tabId
                );

                incrementBlockedCount();

                recentUserClicks.delete(
                    details.sourceTabId
                );

                console.log(
                    "[WinBoost Safe Click] " +
                    "Closed unwanted tab:",
                    {
                        source:
                            sourceUrl.href,

                        blocked:
                            targetUrl.href
                    }
                );
            }
            catch (error) {

                console.error(
                    "[WinBoost Safe Click] " +
                    "Unable to close unwanted tab:",
                    error
                );
            }
        }
    );