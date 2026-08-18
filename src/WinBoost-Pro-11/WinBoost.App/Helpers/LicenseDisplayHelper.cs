using WinBoost.App.Localization;
using WinBoost.App.Models;

namespace WinBoost.App.Helpers
{
    public static class LicenseDisplayHelper
    {
        public static string GetStatusText(
            LicenseStatus status)
        {
            string resourceKey =
                status switch
                {
                    LicenseStatus.Unlicensed =>
                        "LicenseStatusUnlicensed",

                    LicenseStatus.Trial =>
                        "LicenseStatusTrial",

                    LicenseStatus.Licensed =>
                        "LicenseStatusLicensed",

                    LicenseStatus.Expired =>
                        "LicenseStatusExpired",

                    LicenseStatus.Invalid =>
                        "LicenseStatusInvalid",

                    _ =>
                        "LicenseStatusUnlicensed"
                };

            return LocalizationHelper.Get(
                resourceKey);
        }
    }
}