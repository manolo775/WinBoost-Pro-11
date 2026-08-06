using System;
using System.Collections.Generic;
using WinBoost.App.Models;

namespace WinBoost.App.Services.ServicesManager
{
    public sealed class WindowsServiceRecommendationEngine
    {
        public const string RecommendationCritical =
            "Critical service";

        public const string RecommendationKeepEnabled =
            "Keep enabled";

        public const string RecommendationOptional =
            "Optional";

        public const string RecommendationSafeToDisable =
            "Safe to disable if unused";

        public const string RecommendationReview =
            "Review";

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
            if (string.IsNullOrWhiteSpace(
                    serviceName))
            {
                return RecommendationReview;
            }

            if (IsCritical(
                    serviceName))
            {
                return RecommendationCritical;
            }

            if (KeepEnabledServices.Contains(
                    serviceName))
            {
                return RecommendationKeepEnabled;
            }

            if (OptionalServices.Contains(
                    serviceName))
            {
                return RecommendationOptional;
            }

            if (SafeToDisableIfUnusedServices.Contains(
                    serviceName))
            {
                return RecommendationSafeToDisable;
            }

            return RecommendationReview;
        }

        public bool IsCritical(
            string serviceName)
        {
            if (string.IsNullOrWhiteSpace(
                    serviceName))
            {
                return false;
            }

            return CriticalServices.Contains(
                serviceName);
        }

        public string GetRiskLevel(
            string serviceName)
        {
            if (string.IsNullOrWhiteSpace(
                    serviceName))
            {
                return WindowsServiceInfo.RiskUnknown;
            }

            if (CriticalServices.Contains(
                    serviceName))
            {
                return WindowsServiceInfo.RiskCritical;
            }

            if (KeepEnabledServices.Contains(
                    serviceName))
            {
                return WindowsServiceInfo.RiskHigh;
            }

            if (OptionalServices.Contains(
                    serviceName))
            {
                return WindowsServiceInfo.RiskMedium;
            }

            if (SafeToDisableIfUnusedServices.Contains(
                    serviceName))
            {
                return WindowsServiceInfo.RiskLow;
            }

            return WindowsServiceInfo.RiskUnknown;
        }

        public bool CanBeStoppedSafely(
            string serviceName)
        {
            if (string.IsNullOrWhiteSpace(
                    serviceName))
            {
                return false;
            }

            /*
             * Serviciile cunoscute ca fiind critice
             * nu pot fi oprite din WinBoost.
             *
             * Pentru celelalte servicii, utilizatorul
             * primește confirmare înainte de oprire.
             */
            return !CriticalServices.Contains(
                serviceName);
        }

        public bool IsRecommendedForOptimization(
            string serviceName)
        {
            if (string.IsNullOrWhiteSpace(
                    serviceName))
            {
                return false;
            }

            return SafeToDisableIfUnusedServices.Contains(
                serviceName);
        }
    }
}