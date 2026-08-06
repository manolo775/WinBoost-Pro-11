using Microsoft.Win32;
using System;
using System.Collections.Generic;
using WinBoost.App.Models;

namespace WinBoost.App.Services.Privacy
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
            object? rawValue =
                ReadValue(
                    RegistryHive.LocalMachine,
                    @"SOFTWARE\Policies\Microsoft\Windows\DataCollection",
                    "AllowTelemetry");

            rawValue ??=
                ReadValue(
                    RegistryHive.LocalMachine,
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection",
                    "AllowTelemetry");

            int? value =
                ConvertToInt(
                    rawValue);

            string statusResourceKey =
                value switch
                {
                    0 =>
                        "PrivacyStatusDiagnosticMinimal",

                    1 =>
                        "PrivacyStatusDiagnosticRequired",

                    2 =>
                        "PrivacyStatusDiagnosticEnhanced",

                    3 =>
                        "PrivacyStatusDiagnosticOptional",

                    _ =>
                        "PrivacyStatusDefault"
                };

            return CreateItem(
                "diagnostic-data",
                "PrivacyItemDiagnosticTitle",
                "PrivacyItemDiagnosticDescription",
                statusResourceKey);
        }

        private static PrivacyCheckItem CheckAdvertisingId()
        {
            int? disabledByPolicy =
                ConvertToInt(
                    ReadValue(
                        RegistryHive.LocalMachine,
                        @"SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo",
                        "DisabledByGroupPolicy"));

            string statusResourceKey;

            if (disabledByPolicy == 1)
            {
                statusResourceKey =
                    "PrivacyStatusDisabled";
            }
            else
            {
                object? rawValue =
                    ReadValue(
                        RegistryHive.CurrentUser,
                        @"SOFTWARE\Microsoft\Windows\CurrentVersion\AdvertisingInfo",
                        "Enabled");

                rawValue ??=
                    ReadValue(
                        RegistryHive.LocalMachine,
                        @"SOFTWARE\Microsoft\Windows\CurrentVersion\AdvertisingInfo",
                        "Enabled");

                statusResourceKey =
                    ConvertToInt(
                        rawValue) switch
                    {
                        0 =>
                            "PrivacyStatusDisabled",

                        1 =>
                            "PrivacyStatusEnabled",

                        _ =>
                            "PrivacyStatusDefault"
                    };
            }

            return CreateItem(
                "advertising-id",
                "PrivacyItemAdvertisingTitle",
                "PrivacyItemAdvertisingDescription",
                statusResourceKey);
        }

        private static PrivacyCheckItem CheckActivityHistory()
        {
            const string path =
                @"SOFTWARE\Policies\Microsoft\Windows\System";

            int? activityFeed =
                ConvertToInt(
                    ReadValue(
                        RegistryHive.LocalMachine,
                        path,
                        "EnableActivityFeed"));

            int? publishActivities =
                ConvertToInt(
                    ReadValue(
                        RegistryHive.LocalMachine,
                        path,
                        "PublishUserActivities"));

            int? uploadActivities =
                ConvertToInt(
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

            string statusResourceKey =
                !isConfigured
                    ? "PrivacyStatusDefault"
                    : isEnabled
                        ? "PrivacyStatusEnabled"
                        : "PrivacyStatusDisabled";

            return CreateItem(
                "activity-history",
                "PrivacyItemActivityHistoryTitle",
                "PrivacyItemActivityHistoryDescription",
                statusResourceKey);
        }

        private static PrivacyCheckItem CheckLocationServices()
        {
            const string path =
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\" +
                @"CapabilityAccessManager\ConsentStore\location";

            object? rawValue =
                ReadValue(
                    RegistryHive.CurrentUser,
                    path,
                    "Value");

            rawValue ??=
                ReadValue(
                    RegistryHive.LocalMachine,
                    path,
                    "Value");

            string? value =
                rawValue?.ToString();

            string statusResourceKey;

            if (string.Equals(
                    value,
                    "Deny",
                    StringComparison.OrdinalIgnoreCase))
            {
                statusResourceKey =
                    "PrivacyStatusDisabled";
            }
            else if (string.Equals(
                         value,
                         "Allow",
                         StringComparison.OrdinalIgnoreCase))
            {
                statusResourceKey =
                    "PrivacyStatusEnabled";
            }
            else
            {
                statusResourceKey =
                    "PrivacyStatusDefault";
            }

            return CreateItem(
                "location-services",
                "PrivacyItemLocationTitle",
                "PrivacyItemLocationDescription",
                statusResourceKey);
        }

        private static PrivacyCheckItem CreateItem(
            string id,
            string titleResourceKey,
            string descriptionResourceKey,
            string statusResourceKey)
        {
            return new PrivacyCheckItem
            {
                Id =
                    id,

                TitleResourceKey =
                    titleResourceKey,

                DescriptionResourceKey =
                    descriptionResourceKey,

                StatusResourceKey =
                    statusResourceKey
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

                return key?.GetValue(
                    valueName);
            }
            catch
            {
                return null;
            }
        }

        private static int? ConvertToInt(
            object? value)
        {
            try
            {
                return value == null
                    ? null
                    : Convert.ToInt32(
                        value);
            }
            catch
            {
                return null;
            }
        }
    }
}