using System;

namespace WinBoost.App.Services
{
    public sealed class UptimeService
    {
        public string GetWindowsUptime()
        {
            TimeSpan uptime =
                TimeSpan.FromMilliseconds(Environment.TickCount64);

            if (uptime.Days > 0)
            {
                return $"{uptime.Days} zile {uptime.Hours} ore";
            }

            return $"{uptime.Hours} ore {uptime.Minutes} min";
        }
    }
}