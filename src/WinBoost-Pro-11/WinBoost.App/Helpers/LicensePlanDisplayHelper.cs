using WinBoost.App.Localization;
using WinBoost.App.Models;

namespace WinBoost.App.Helpers
{
    public static class LicensePlanDisplayHelper
    {
        public static string GetDisplayName(
            LicensePlan plan)
        {
            string resourceKey =
                plan switch
                {
                    LicensePlan.PromotionalLifetime =>
                        "LicensePlanPromotionalLifetime",

                    LicensePlan.OneMonth =>
                        "LicensePlanOneMonth",

                    LicensePlan.ThreeMonths =>
                        "LicensePlanThreeMonths",

                    LicensePlan.SixMonths =>
                        "LicensePlanSixMonths",

                    LicensePlan.OneYear =>
                        "LicensePlanOneYear",

                    _ =>
                        "LicensePlanUnknown"
                };

            return LocalizationHelper.Get(
                resourceKey);
        }
    }
}