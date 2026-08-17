using System;
using WinBoost.App.Localization;

namespace WinBoost.App.Services.Monitoring
{
    public sealed class UptimeService
    {
        public string GetWindowsUptime()
        {
            TimeSpan uptime =
                TimeSpan.FromMilliseconds(
                    Environment.TickCount64);

            if (uptime.Days > 0)
            {
                string resourceKey;

                if (uptime.Days == 1 &&
                    uptime.Hours == 1)
                {
                    resourceKey =
                        "UptimeDayHour";
                }
                else if (uptime.Days == 1)
                {
                    resourceKey =
                        "UptimeDayHours";
                }
                else if (uptime.Hours == 1)
                {
                    resourceKey =
                        "UptimeDaysHour";
                }
                else
                {
                    resourceKey =
                        "UptimeDaysHours";
                }

                return LocalizationHelper.Format(
                    resourceKey,
                    uptime.Days,
                    uptime.Hours);
            }

            return LocalizationHelper.Format(
                uptime.Hours == 1
                    ? "UptimeHourMinutes"
                    : "UptimeHoursMinutes",
                uptime.Hours,
                uptime.Minutes);
        }
    }
}