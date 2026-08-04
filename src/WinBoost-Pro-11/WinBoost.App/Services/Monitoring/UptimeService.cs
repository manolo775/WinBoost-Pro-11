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
                return LocalizationHelper.Format(
                    "UptimeDaysHours",
                    uptime.Days,
                    uptime.Hours);
            }

            return LocalizationHelper.Format(
                "UptimeHoursMinutes",
                uptime.Hours,
                uptime.Minutes);
        }
    }
}