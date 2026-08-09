using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using WinBoost.App.Models;

namespace WinBoost.App.Services.Apps
{
    public class InstalledAppsScanner
    {
        private const string UninstallPath =
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

        private const string WowUninstallPath =
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall";

        public Task<List<InstalledAppInfo>> ScanAsync()
        {
            return Task.Run(() =>
            {
                var applications =
                    new List<InstalledAppInfo>();

                ReadApplications(
                    Registry.LocalMachine,
                    UninstallPath,
                    applications);

                ReadApplications(
                    Registry.LocalMachine,
                    WowUninstallPath,
                    applications);

                ReadApplications(
                    Registry.CurrentUser,
                    UninstallPath,
                    applications);

                return applications
                    .Where(application =>
                        !string.IsNullOrWhiteSpace(
                            application.DisplayName))
                    .GroupBy(
                        application =>
                            $"{application.DisplayName}|{application.Version}",
                        StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.First())
                    .OrderBy(
                        application => application.DisplayName,
                        StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
            });
        }

        private static void ReadApplications(
            RegistryKey rootKey,
            string registryPath,
            List<InstalledAppInfo> applications)
        {
            try
            {
                using RegistryKey? uninstallKey =
                    rootKey.OpenSubKey(registryPath);

                if (uninstallKey == null)
                    return;

                foreach (string subKeyName
                         in uninstallKey.GetSubKeyNames())
                {
                    ReadApplication(
                        uninstallKey,
                        subKeyName,
                        applications);
                }
            }
            catch
            {
                // O zonă inaccesibilă din Registry este ignorată.
            }
        }

        private static void ReadApplication(
            RegistryKey uninstallKey,
            string subKeyName,
            List<InstalledAppInfo> applications)
        {
            try
            {
                using RegistryKey? applicationKey =
                    uninstallKey.OpenSubKey(subKeyName);

                if (applicationKey == null)
                    return;

                if (IsHiddenOrUpdate(applicationKey))
                    return;

                string displayName =
                    ReadValue(
                        applicationKey,
                        "DisplayName",
                        string.Empty);

                if (string.IsNullOrWhiteSpace(displayName))
                    return;

                applications.Add(
    new InstalledAppInfo
    {
        DisplayName = displayName,

        Publisher =
            ReadValue(
                applicationKey,
                "Publisher",
                "Necunoscut"),

        Version =
            ReadValue(
                applicationKey,
                "DisplayVersion",
                "—"),

        InstallDate =
            FormatInstallDate(
                ReadValue(
                    applicationKey,
                    "InstallDate",
                    string.Empty)),

        InstallLocation =
            ReadValue(
                applicationKey,
                "InstallLocation",
                string.Empty)
    });
            }
            catch
            {
                // Intrările invalide sunt ignorate.
            }
        }

        private static bool IsHiddenOrUpdate(
            RegistryKey applicationKey)
        {
            object? systemComponent =
                applicationKey.GetValue("SystemComponent");

            if (systemComponent != null &&
                int.TryParse(
                    systemComponent.ToString(),
                    out int hiddenValue) &&
                hiddenValue == 1)
            {
                return true;
            }

            if (applicationKey.GetValue("ParentKeyName") != null)
                return true;

            string releaseType =
                applicationKey
                    .GetValue("ReleaseType")
                    ?.ToString()
                    ?? string.Empty;

            return
                releaseType.Contains(
                    "Update",
                    StringComparison.OrdinalIgnoreCase) ||
                releaseType.Contains(
                    "Hotfix",
                    StringComparison.OrdinalIgnoreCase);
        }

        private static string ReadValue(
            RegistryKey key,
            string valueName,
            string fallbackValue)
        {
            string? value =
                key.GetValue(valueName)
                    ?.ToString()
                    ?.Trim();

            return string.IsNullOrWhiteSpace(value)
                ? fallbackValue
                : value;
        }

        private static string FormatInstallDate(
            string installDate)
        {
            if (DateTime.TryParseExact(
                    installDate,
                    "yyyyMMdd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime parsedDate))
            {
                return parsedDate.ToString("dd.MM.yyyy");
            }

            return "—";
        }
    }
}