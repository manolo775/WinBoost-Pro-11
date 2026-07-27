using Microsoft.Win32;
using System;
using System.Collections.Generic;
using WinBoost.App.Models;

namespace WinBoost.App.Services
{
    public sealed class PrivacyScanService
    {
        public IReadOnlyList<PrivacyCheckItem> Scan()
        {
            return new List<PrivacyCheckItem>
            {
                CheckDiagnosticData(),
                CheckAdvertisingId(),
                CheckActivityHistory(),
                CheckLocationServices()
            };
        }

        private static PrivacyCheckItem CheckDiagnosticData()
        {
            object? rawValue = ReadValue(
                RegistryHive.LocalMachine,
                @"SOFTWARE\Policies\Microsoft\Windows\DataCollection",
                "AllowTelemetry");

            rawValue ??= ReadValue(
                RegistryHive.LocalMachine,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection",
                "AllowTelemetry");

            int? value = ConvertToInt(rawValue);

            string status = value switch
            {
                0 => "Date minime",
                1 => "Date necesare",
                2 => "Date îmbunătățite",
                3 => "Date opționale",
                _ => "Setare implicită"
            };

            return CreateItem(
                "diagnostic-data",
                "Diagnostic și telemetrie",
                "Verifică nivelul datelor de diagnostic trimise către Microsoft.",
                status);
        }

        private static PrivacyCheckItem CheckAdvertisingId()
        {
            int? disabledByPolicy = ConvertToInt(
                ReadValue(
                    RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo",
                    "DisabledByGroupPolicy"));

            string status;

            if (disabledByPolicy == 1)
            {
                status = "Dezactivat";
            }
            else
            {
                object? rawValue = ReadValue(
                    RegistryHive.CurrentUser,
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\AdvertisingInfo",
                    "Enabled");

                rawValue ??= ReadValue(
                    RegistryHive.LocalMachine,
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\AdvertisingInfo",
                    "Enabled");

                status = ConvertToInt(rawValue) switch
                {
                    0 => "Dezactivat",
                    1 => "Activat",
                    _ => "Setare implicită"
                };
            }

            return CreateItem(
                "advertising-id",
                "Advertising ID",
                "Verifică utilizarea identificatorului pentru reclame personalizate.",
                status);
        }

        private static PrivacyCheckItem CheckActivityHistory()
        {
            const string path =
                @"SOFTWARE\Policies\Microsoft\Windows\System";

            int? activityFeed = ConvertToInt(
                ReadValue(
                    RegistryHive.LocalMachine,
                    path,
                    "EnableActivityFeed"));

            int? publishActivities = ConvertToInt(
                ReadValue(
                    RegistryHive.LocalMachine,
                    path,
                    "PublishUserActivities"));

            int? uploadActivities = ConvertToInt(
                ReadValue(
                    RegistryHive.LocalMachine,
                    path,
                    "UploadUserActivities"));

            bool isConfigured =
                activityFeed.HasValue ||
                publishActivities.HasValue ||
                uploadActivities.HasValue;

            bool isEnabled =
                activityFeed == 1 ||
                publishActivities == 1 ||
                uploadActivities == 1;

            string status = !isConfigured
                ? "Setare implicită"
                : isEnabled
                    ? "Activat"
                    : "Dezactivat";

            return CreateItem(
                "activity-history",
                "Activity History",
                "Verifică dacă Windows salvează istoricul activităților.",
                status);
        }

        private static PrivacyCheckItem CheckLocationServices()
        {
            const string path =
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\" +
                @"CapabilityAccessManager\ConsentStore\location";

            object? rawValue = ReadValue(
                RegistryHive.CurrentUser,
                path,
                "Value");

            rawValue ??= ReadValue(
                RegistryHive.LocalMachine,
                path,
                "Value");

            string? value = rawValue?.ToString();

            string status;

            if (string.Equals(
                value,
                "Deny",
                StringComparison.OrdinalIgnoreCase))
            {
                status = "Dezactivat";
            }
            else if (string.Equals(
                value,
                "Allow",
                StringComparison.OrdinalIgnoreCase))
            {
                status = "Activat";
            }
            else
            {
                status = "Setare implicită";
            }

            return CreateItem(
                "location-services",
                "Location Services",
                "Verifică accesul aplicațiilor la locația dispozitivului.",
                status);
        }

        private static PrivacyCheckItem CreateItem(
            string id,
            string title,
            string description,
            string status)
        {
            return new PrivacyCheckItem
            {
                Id = id,
                Title = title,
                Description = description,
                Status = status
            };
        }

        private static object? ReadValue(
            RegistryHive hive,
            string path,
            string valueName)
        {
            try
            {
                using RegistryKey baseKey =
                    RegistryKey.OpenBaseKey(
                        hive,
                        RegistryView.Registry64);

                using RegistryKey? key =
                    baseKey.OpenSubKey(
                        path,
                        writable: false);

                return key?.GetValue(valueName);
            }
            catch
            {
                return null;
            }
        }

        private static int? ConvertToInt(object? value)
        {
            try
            {
                return value == null
                    ? null
                    : Convert.ToInt32(value);
            }
            catch
            {
                return null;
            }
        }
    }
}