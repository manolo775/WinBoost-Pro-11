using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WinBoost.App.Models;

namespace WinBoost.App.Services
{
    public class StartupAppsScanner
    {
        private static readonly (RegistryHive Hive, string Path, string Source)[]
            RegistryLocations =
            {
                (
                    RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\Run",
                    "Utilizator curent"
                ),
                (
                    RegistryHive.LocalMachine,
                    @"Software\Microsoft\Windows\CurrentVersion\Run",
                    "Toți utilizatorii"
                ),
                (
                    RegistryHive.LocalMachine,
                    @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run",
                    "Toți utilizatorii (32-bit)"
                )
            };

        public Task<List<StartupAppInfo>> ScanAsync()
        {
            return Task.Run(() =>
            {
                var applications =
                    new List<StartupAppInfo>();

                foreach (var location in RegistryLocations)
                {
                    ScanRegistryLocation(
                        location.Hive,
                        location.Path,
                        location.Source,
                        applications);
                }

                return applications
                    .GroupBy(
                        application =>
                            $"{application.Name}|{application.Command}",
                        StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.First())
                    .OrderBy(application => application.Name)
                    .ToList();
            });
        }

        private static void ScanRegistryLocation(
            RegistryHive hive,
            string registryPath,
            string source,
            List<StartupAppInfo> applications)
        {
            try
            {
                using RegistryKey baseKey =
                    RegistryKey.OpenBaseKey(
                        hive,
                        RegistryView.Default);

                using RegistryKey? registryKey =
                    baseKey.OpenSubKey(registryPath);

                if (registryKey == null)
                    return;

                foreach (string valueName
                         in registryKey.GetValueNames())
                {
                    string command =
                        Convert.ToString(
                            registryKey.GetValue(valueName))
                        ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(command))
                        continue;

                    applications.Add(
                        new StartupAppInfo
                        {
                            Name = string.IsNullOrWhiteSpace(valueName)
                                ? "Aplicație necunoscută"
                                : valueName,

                            Command = command,
                            Source = source,
                            IsEnabled = true
                        });
                }
            }
            catch
            {
                // Unele locații pot necesita drepturi suplimentare.
                // Scanarea continuă cu celelalte locații.
            }
        }
    }
}