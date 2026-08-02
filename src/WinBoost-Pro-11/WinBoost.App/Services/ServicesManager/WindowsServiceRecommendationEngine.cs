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
                "RpcEptMapper",
                "DcomLaunch",
                "PlugPlay",
                "Power",
                "EventLog",
                "Winmgmt",
                "Dhcp",
                "Dnscache",
                "NlaSvc",
                "LanmanWorkstation",
                "ProfSvc",
                "UserManager",
                "SamSs",
                "LSM",
                "BFE",
                "mpssvc",
                "gpsvc",
                "Schedule",
                "CryptSvc",
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
                "UsoSvc",
                "W32Time",
                "AudioSrv",
                "AudioEndpointBuilder",
                "Themes",
                "Appinfo",
                "ShellHWDetection"
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
                "MapsBroker",
                "PhoneSvc",
                "SensorService",
                "WalletService"
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
                "lfsvc",
                "RetailDemo",
                "WMPNetworkSvc"
            };

        public string GetRecommendation(
            string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                return "Review";
            }

            if (IsCritical(serviceName))
            {
                return "Critical - do not stop";
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

        public bool IsCritical(
            string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                return false;
            }

            return CriticalServices.Contains(serviceName);
        }

        public string GetRiskLevel(
            string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                return "Unknown";
            }

            if (CriticalServices.Contains(serviceName))
            {
                return "Critical";
            }

            if (KeepEnabledServices.Contains(serviceName))
            {
                return "High";
            }

            if (OptionalServices.Contains(serviceName))
            {
                return "Medium";
            }

            if (SafeToDisableIfUnusedServices.Contains(
                    serviceName))
            {
                return "Low";
            }

            return "Unknown";
        }

        public bool CanBeStoppedSafely(
            string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                return false;
            }

            /*
             * WinBoost blochează doar serviciile cunoscute
             * ca fiind critice.
             *
             * Pentru serviciile necunoscute, utilizatorul va
             * primi în continuare un avertisment înainte de oprire.
             */
            return !CriticalServices.Contains(serviceName);
        }

        public bool IsRecommendedForOptimization(
            string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                return false;
            }

            return SafeToDisableIfUnusedServices.Contains(
                serviceName);
        }
    }
}