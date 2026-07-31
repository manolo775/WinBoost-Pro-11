using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WinBoost.App.Models;

namespace WinBoost.App.Services.Startup
{
    public class StartupAppsScanner
    {
        private static readonly StartupRegistryLocation[]
            RegistryLocations =
            {
                new(
                    RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\Run",
                    @"Software\WinBoost\WinBoostDisabledStartup\CurrentUser",
                    "Utilizator curent"
                ),

                new(
                    RegistryHive.LocalMachine,
                    @"Software\Microsoft\Windows\CurrentVersion\Run",
                    @"Software\WinBoost\WinBoostDisabledStartup\LocalMachine",
                    "Toți utilizatorii"
                ),

                new(
                    RegistryHive.LocalMachine,
                    @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run",
                    @"Software\WinBoost\WinBoostDisabledStartup\LocalMachine32",
                    "Toți utilizatorii (32-bit)"
                )
            };

        public Task<List<StartupAppInfo>> ScanAsync()
        {
            return Task.Run(() =>
            {
                var applications =
                    new List<StartupAppInfo>();

                foreach (StartupRegistryLocation location
                         in RegistryLocations)
                {
                    // Citește aplicațiile active.
                    ScanRegistryLocation(
                        location,
                        location.ActivePath,
                        isEnabled: true,
                        applications);

                    // Citește aplicațiile dezactivate de WinBoost.
                    ScanRegistryLocation(
                        location,
                        location.DisabledPath,
                        isEnabled: false,
                        applications);
                }

                return applications
                    .GroupBy(
                        application =>
                            $"{application.RegistryHive}|" +
                            $"{application.RegistryPath}|" +
                            $"{application.RegistryValueName}",
                        StringComparer.OrdinalIgnoreCase)
                    .Select(group =>
                        group
                            .OrderByDescending(
                                application =>
                                    application.IsEnabled)
                            .First())
                    .OrderBy(application =>
                        application.Name)
                    .ToList();
            });
        }

        private static void ScanRegistryLocation(
            StartupRegistryLocation location,
            string pathToScan,
            bool isEnabled,
            List<StartupAppInfo> applications)
        {
            try
            {
                using RegistryKey baseKey =
                    RegistryKey.OpenBaseKey(
                        location.Hive,
                        RegistryView.Default);

                using RegistryKey? registryKey =
                    baseKey.OpenSubKey(pathToScan);

                if (registryKey == null)
                    return;

                foreach (string valueName
                         in registryKey.GetValueNames())
                {
                    string command =
                        Convert.ToString(
                            registryKey.GetValue(
                                valueName,
                                null,
                                RegistryValueOptions
                                    .DoNotExpandEnvironmentNames))
                        ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(command))
                        continue;

                    applications.Add(
                        new StartupAppInfo
                        {
                            Name =
                                string.IsNullOrWhiteSpace(valueName)
                                    ? "Aplicație necunoscută"
                                    : valueName,

                            Command = command,

                            Source = location.Source,

                            RegistryHive = location.Hive,

                            // Păstrăm locația originală,
                            // inclusiv pentru aplicațiile dezactivate.
                            RegistryPath = location.ActivePath,

                            RegistryValueName = valueName,

                            IsEnabled = isEnabled
                        });
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Unele chei din HKEY_LOCAL_MACHINE
                // pot necesita drepturi de administrator.
            }
            catch (System.Security.SecurityException)
            {
                // Scanarea continuă cu celelalte locații.
            }
            catch
            {
                // O eroare într-o locație nu trebuie
                // să oprească întreaga scanare.
            }
        }

        private sealed class StartupRegistryLocation
        {
            public StartupRegistryLocation(
                RegistryHive hive,
                string activePath,
                string disabledPath,
                string source)
            {
                Hive = hive;
                ActivePath = activePath;
                DisabledPath = disabledPath;
                Source = source;
            }

            public RegistryHive Hive { get; }

            public string ActivePath { get; }

            public string DisabledPath { get; }

            public string Source { get; }
        }
    }
}