using System;
using System.Globalization;

namespace WinBoost.App.Models
{
    public class InstalledAppInfo
    {
        public string DisplayName { get; set; } =
            string.Empty;

        public string Publisher { get; set; } =
            string.Empty;

        public string Version { get; set; } =
            string.Empty;

        public string InstallDate { get; set; } =
            string.Empty;

        public string InstallLocation { get; set; } =
            string.Empty;

        public bool HasInstallLocation =>
            !string.IsNullOrWhiteSpace(
                InstallLocation);

        public DateTime InstallDateValue
        {
            get
            {
                if (DateTime.TryParseExact(
                        InstallDate,
                        "dd.MM.yyyy",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out DateTime date))
                {
                    return date;
                }

                return DateTime.MinValue;
            }
        }
    }
}