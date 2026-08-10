using Microsoft.Win32;
using System;
using System.Security;
using System.Threading.Tasks;
using WinBoost.App.Models;

namespace WinBoost.App.Services.Startup
{
    public class StartupAppsManager
    {
        public Task SetEnabledAsync(
            StartupAppInfo application,
            bool enable)
        {
            ArgumentNullException.ThrowIfNull(application);

            return Task.Run(() =>
            {
                if (enable)
                {
                    EnableApplication(application);
                }
                else
                {
                    DisableApplication(application);
                }

                application.IsEnabled = enable;
            });
        }

        private static void DisableApplication(
            StartupAppInfo application)
        {
            string disabledPath =
                GetDisabledRegistryPath(application);

            MoveRegistryValue(
                application.RegistryHive,
                        GetRegistryView(application),
                     application.RegistryPath,
                   disabledPath,
                application.RegistryValueName);
        }

        private static void EnableApplication(
            StartupAppInfo application)
        {
            string disabledPath =
                GetDisabledRegistryPath(application);

            MoveRegistryValue(
     application.RegistryHive,
     GetRegistryView(application),
     disabledPath,
     application.RegistryPath,
     application.RegistryValueName);
        }

        private static void MoveRegistryValue(
    RegistryHive hive,
    RegistryView registryView,
    string sourcePath,
    string destinationPath,
    string valueName)
        {
            ValidateRegistryInformation(
                sourcePath,
                destinationPath,
                valueName);

            try
            {
                using RegistryKey baseKey =
                 RegistryKey.OpenBaseKey(
                                hive,
                         registryView); 

                using RegistryKey? sourceKey =
                    baseKey.OpenSubKey(
                        sourcePath,
                        writable: true);

                if (sourceKey == null)
                {
                    throw new InvalidOperationException(
                        $"Cheia Registry sursă nu a fost găsită: " +
                        $"{sourcePath}");
                }

                object? value =
                    sourceKey.GetValue(
                        valueName,
                        null,
                        RegistryValueOptions
                            .DoNotExpandEnvironmentNames);

                if (value == null)
                {
                    throw new InvalidOperationException(
                        $"Intrarea Registry „{valueName}” " +
                        $"nu a fost găsită.");
                }

                RegistryValueKind valueKind =
                    sourceKey.GetValueKind(valueName);

                using RegistryKey destinationKey =
                    baseKey.CreateSubKey(
                        destinationPath,
                        writable: true)
                    ?? throw new InvalidOperationException(
                        $"Cheia Registry destinație nu a putut " +
                        $"fi creată: {destinationPath}");

                destinationKey.SetValue(
                    valueName,
                    value,
                    valueKind);

                try
                {
                    sourceKey.DeleteValue(
                        valueName,
                        throwOnMissingValue: true);
                }
                catch
                {
                    // Dacă ștergerea din cheia sursă eșuează,
                    // eliminăm copia creată pentru a evita dublarea.
                    destinationKey.DeleteValue(
                        valueName,
                        throwOnMissingValue: false);

                    throw;
                }
            }
            catch (UnauthorizedAccessException exception)
            {
                throw new InvalidOperationException(
                    "Operația necesită drepturi de administrator. " +
                    "Pornește WinBoost Pro 11 ca administrator.",
                    exception);
            }
            catch (SecurityException exception)
            {
                throw new InvalidOperationException(
                    "Windows a refuzat accesul la Registry. " +
                    "Pornește WinBoost Pro 11 ca administrator.",
                    exception);
            }
        }

        private static RegistryView GetRegistryView(
    StartupAppInfo application)
        {
            if (application.RegistryHive ==
                    RegistryHive.LocalMachine &&
                application.RegistryPath.Equals(
                    @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run",
                    StringComparison.OrdinalIgnoreCase))
            {
                return RegistryView.Registry32;
            }

            if (application.RegistryHive ==
                RegistryHive.LocalMachine)
            {
                return RegistryView.Registry64;
            }

            return RegistryView.Default;
        }

        private static string GetDisabledRegistryPath(
            StartupAppInfo application)
        {
            if (application.RegistryHive ==
                    RegistryHive.CurrentUser &&
                application.RegistryPath.Equals(
                    @"Software\Microsoft\Windows\CurrentVersion\Run",
                    StringComparison.OrdinalIgnoreCase))
            {
                return
                    @"Software\WinBoost\" +
                    @"WinBoostDisabledStartup\CurrentUser";
            }

            if (application.RegistryHive ==
                    RegistryHive.LocalMachine &&
                application.RegistryPath.Equals(
                    @"Software\Microsoft\Windows\CurrentVersion\Run",
                    StringComparison.OrdinalIgnoreCase))
            {
                return
                    @"Software\WinBoost\" +
                    @"WinBoostDisabledStartup\LocalMachine";
            }

            if (application.RegistryHive ==
                    RegistryHive.LocalMachine &&
                application.RegistryPath.Equals(
                    @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run",
                    StringComparison.OrdinalIgnoreCase))
            {
                return
                    @"Software\WinBoost\WinBoostDisabledStartup\LocalMachine32";
            }

            throw new NotSupportedException(
                "Locația Registry a aplicației nu este suportată.");
        }

        private static void ValidateRegistryInformation(
            string sourcePath,
            string destinationPath,
            string valueName)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                throw new ArgumentException(
                    "Calea Registry sursă lipsește.",
                    nameof(sourcePath));
            }

            if (string.IsNullOrWhiteSpace(destinationPath))
            {
                throw new ArgumentException(
                    "Calea Registry destinație lipsește.",
                    nameof(destinationPath));
            }

            if (string.IsNullOrWhiteSpace(valueName))
            {
                throw new ArgumentException(
                    "Numele intrării Registry lipsește.",
                    nameof(valueName));
            }
        }
    }
}