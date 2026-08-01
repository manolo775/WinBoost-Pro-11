using System;
using System.Collections.Generic;

namespace WinBoost.App.Services.ServicesManager
{
    public class WindowsServiceRecommendationEngine
    {
        private static readonly HashSet<string>
            CriticalServices =
            new(
                StringComparer.OrdinalIgnoreCase)
            {
                "RpcSs",
                "DcomLaunch",
                "PlugPlay",
                "Power",
                "EventLog",
                "Winmgmt",
                "Dhcp",
                "Dnscache",
                "NlaSvc",
                "LanmanWorkstation",
                "W32Time",
                "WinDefend",
                "SecurityHealthService",
                "wscsvc"
            };

        private static readonly HashSet<string>
            KeepEnabledServices =
            new(
                StringComparer.OrdinalIgnoreCase)
            {
                "BITS",
                "wuauserv",
                "CryptSvc",
                "UsoSvc",
                "Schedule",
                "AudioSrv",
                "AudioEndpointBuilder",
                "ProfSvc",
                "UserManager"
            };

        private static readonly HashSet<string>
            OptionalServices =
            new(
                StringComparer.OrdinalIgnoreCase)
            {
                "Spooler",
                "bthserv",
                "TabletInputService",
                "WSearch",
                "MapsBroker"
            };

        private static readonly HashSet<string>
            SafeToDisableIfUnusedServices =
            new(
                StringComparer.OrdinalIgnoreCase)
            {
                "Fax",
                "RemoteRegistry",
                "XblAuthManager",
                "XblGameSave",
                "XboxGipSvc",
                "XboxNetApiSvc",
                "SharedAccess",
                "lfsvc"
            };

        public string GetRecommendation(
            string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                return "Review";
            }

            if (CriticalServices.Contains(serviceName))
            {
                return "Critical";
            }

            if (KeepEnabledServices.Contains(serviceName))
            {
                return "Keep enabled";
            }

            if (OptionalServices.Contains(serviceName))
            {
                return "Optional";
            }

            if (SafeToDisableIfUnusedServices.Contains(
                    serviceName))
            {
                return "Safe to disable if unused";
            }

            return "Review";
        }
    }
}