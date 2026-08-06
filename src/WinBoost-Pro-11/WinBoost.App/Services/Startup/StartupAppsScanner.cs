using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WinBoost.App.Localization;
using WinBoost.App.Models;

namespace WinBoost.App.Services.Startup
{
    public sealed class StartupAppsScanner
    {
        private static readonly StartupRegistryLocation[]
            RegistryLocations =
            {
                new(
                    RegistryHive.CurrentUser,
                    @"Software\Microsoft\Windows\CurrentVersion\Run",
                    @"Software\WinBoost\WinBoostDisabledStartup\CurrentUser",
                    "StartupSourceCurrentUser",
                    RegistryView.Default),

                new(
                    RegistryHive.LocalMachine,
                    @"Software\Microsoft\Windows\CurrentVersion\Run",
                    @"Software\WinBoost\WinBoostDisabledStartup\LocalMachine",
                    "StartupSourceAllUsers",
                    RegistryView.Registry64),

                new(
                    RegistryHive.LocalMachine,
                    @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run",
                    @"Software\WinBoost\WinBoostDisabledStartup\LocalMachine32",
                    "StartupSourceAllUsers32",
                    RegistryView.Registry32)
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
                    ScanRegistryLocation(
                        location,
                        location.ActivePath,
                        isEnabled: true,
                        applications);

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
                    .Select(
                        group =>
                            group
                                .OrderByDescending(
                                    application =>
                                        application.IsEnabled)
                                .First())
                    .OrderBy(
                        application =>
                            application.Name,
                        StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
            });
        }

        private static void ScanRegistryLocation(
            StartupRegistryLocation location,
            string pathToScan,
            bool isEnabled,
            ICollection<StartupAppInfo> applications)
        {
            try
            {
                using RegistryKey baseKey =
                    RegistryKey.OpenBaseKey(
                        location.Hive,
                        location.RegistryView);

                using RegistryKey? registryKey =
                    baseKey.OpenSubKey(
                        pathToScan,
                        writable: false);

                if (registryKey == null)
                {
                    return;
                }

                foreach (string valueName
                         in registryKey.GetValueNames())
                {
                    string command =
                        ReadCommand(
                            registryKey,
                            valueName);

                    if (string.IsNullOrWhiteSpace(
                            command))
                    {
                        continue;
                    }

                    StartupCommandInfo commandInfo =
                        ParseCommand(
                            command);

                    FileMetadata metadata =
                        ReadFileMetadata(
                            commandInfo.ExecutablePath);

                    string applicationName =
                        GetApplicationName(
                            valueName,
                            commandInfo.ExecutablePath,
                            metadata.Description);

                    applications.Add(
                        new StartupAppInfo
                        {
                            Name =
                                applicationName,

                            Command =
                                command,

                            SourceResourceKey =
                                        location.SourceResourceKey,

                            RegistryHive =
                                location.Hive,

                            RegistryPath =
                                location.ActivePath,

                            RegistryValueName =
                                valueName,

                            IsEnabled =
                                isEnabled,

                            ExecutablePath =
                                commandInfo.ExecutablePath,

                            Arguments =
                                commandInfo.Arguments,

                            Publisher =
                                metadata.Publisher,

                            Description =
                                metadata.Description,

                            FileVersion =
                                metadata.FileVersion,

                            StartupImpactResourceKey =
                                     "StartupImpactUnknown",

                            StartupTypeResourceKey =
                                     "StartupTypeRegistry",

                            RequiresAdministrator =
                                location.Hive ==
                                RegistryHive.LocalMachine
                        });
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Unele chei HKLM necesită drepturi
                // de administrator pentru acces.
            }
            catch (System.Security.SecurityException)
            {
                // Scanarea continuă cu următoarea locație.
            }
            catch
            {
                // O locație invalidă nu trebuie să oprească
                // întreaga scanare Startup.
            }
        }

        private static string ReadCommand(
            RegistryKey registryKey,
            string valueName)
        {
            object? rawValue =
                registryKey.GetValue(
                    valueName,
                    null,
                    RegistryValueOptions
                        .DoNotExpandEnvironmentNames);

            string command =
                Convert.ToString(
                    rawValue)
                ?? string.Empty;

            return Environment
                .ExpandEnvironmentVariables(
                    command)
                .Trim();
        }

        private static StartupCommandInfo ParseCommand(
            string command)
        {
            if (string.IsNullOrWhiteSpace(
                    command))
            {
                return new StartupCommandInfo();
            }

            string normalizedCommand =
                command.Trim();

            string executablePath;
            string arguments;

            if (normalizedCommand.StartsWith(
                    "\"",
                    StringComparison.Ordinal))
            {
                int closingQuoteIndex =
                    normalizedCommand.IndexOf(
                        '"',
                        1);

                if (closingQuoteIndex > 1)
                {
                    executablePath =
                        normalizedCommand.Substring(
                            1,
                            closingQuoteIndex - 1);

                    arguments =
                        normalizedCommand
                            .Substring(
                                closingQuoteIndex + 1)
                            .Trim();
                }
                else
                {
                    executablePath =
                        normalizedCommand.Trim(
                            '"');

                    arguments =
                        string.Empty;
                }
            }
            else
            {
                int executableEndIndex =
                    FindExecutableEndIndex(
                        normalizedCommand);

                if (executableEndIndex >= 0)
                {
                    executablePath =
                        normalizedCommand.Substring(
                            0,
                            executableEndIndex);

                    arguments =
                        normalizedCommand
                            .Substring(
                                executableEndIndex)
                            .Trim();
                }
                else
                {
                    int firstSpaceIndex =
                        normalizedCommand.IndexOf(
                            ' ');

                    if (firstSpaceIndex > 0)
                    {
                        executablePath =
                            normalizedCommand.Substring(
                                0,
                                firstSpaceIndex);

                        arguments =
                            normalizedCommand
                                .Substring(
                                    firstSpaceIndex + 1)
                                .Trim();
                    }
                    else
                    {
                        executablePath =
                            normalizedCommand;

                        arguments =
                            string.Empty;
                    }
                }
            }

            executablePath =
                NormalizeExecutablePath(
                    executablePath);

            return new StartupCommandInfo
            {
                ExecutablePath =
                    executablePath,

                Arguments =
                    arguments
            };
        }

        private static int FindExecutableEndIndex(
            string command)
        {
            string[] executableExtensions =
            {
                ".exe",
                ".com",
                ".bat",
                ".cmd"
            };

            int selectedIndex =
                -1;

            foreach (string extension
                     in executableExtensions)
            {
                int extensionIndex =
                    command.IndexOf(
                        extension,
                        StringComparison.OrdinalIgnoreCase);

                if (extensionIndex < 0)
                {
                    continue;
                }

                int candidateIndex =
                    extensionIndex +
                    extension.Length;

                if (selectedIndex < 0 ||
                    candidateIndex < selectedIndex)
                {
                    selectedIndex =
                        candidateIndex;
                }
            }

            return selectedIndex;
        }

        private static string NormalizeExecutablePath(
            string executablePath)
        {
            if (string.IsNullOrWhiteSpace(
                    executablePath))
            {
                return string.Empty;
            }

            string normalizedPath =
                Environment
                    .ExpandEnvironmentVariables(
                        executablePath)
                    .Trim()
                    .Trim('"');

            try
            {
                if (Path.IsPathFullyQualified(
                        normalizedPath))
                {
                    return Path.GetFullPath(
                        normalizedPath);
                }

                string? resolvedPath =
                    ResolveExecutableFromPath(
                        normalizedPath);

                return resolvedPath ??
                       normalizedPath;
            }
            catch
            {
                return normalizedPath;
            }
        }

        private static string? ResolveExecutableFromPath(
            string executableName)
        {
            if (string.IsNullOrWhiteSpace(
                    executableName))
            {
                return null;
            }

            string? pathEnvironmentVariable =
                Environment.GetEnvironmentVariable(
                    "PATH");

            if (string.IsNullOrWhiteSpace(
                    pathEnvironmentVariable))
            {
                return null;
            }

            foreach (string directory
                     in pathEnvironmentVariable.Split(
                         Path.PathSeparator,
                         StringSplitOptions
                             .RemoveEmptyEntries |
                         StringSplitOptions
                             .TrimEntries))
            {
                try
                {
                    string candidatePath =
                        Path.Combine(
                            directory,
                            executableName);

                    if (File.Exists(
                            candidatePath))
                    {
                        return Path.GetFullPath(
                            candidatePath);
                    }
                }
                catch
                {
                    // Folderele PATH invalide sunt ignorate.
                }
            }

            return null;
        }

        private static FileMetadata ReadFileMetadata(
            string executablePath)
        {
            if (string.IsNullOrWhiteSpace(
                    executablePath) ||
                !File.Exists(
                    executablePath))
            {
                return new FileMetadata();
            }

            try
            {
                FileVersionInfo versionInfo =
                    FileVersionInfo.GetVersionInfo(
                        executablePath);

                return new FileMetadata
                {
                    Publisher =
                        versionInfo.CompanyName?.Trim()
                        ?? string.Empty,

                    Description =
                        versionInfo.FileDescription?.Trim()
                        ?? string.Empty,

                    FileVersion =
                        versionInfo.FileVersion?.Trim()
                        ?? string.Empty
                };
            }
            catch
            {
                return new FileMetadata();
            }
        }

        private static string GetApplicationName(
            string registryValueName,
            string executablePath,
            string description)
        {
            if (!string.IsNullOrWhiteSpace(
                    registryValueName))
            {
                return registryValueName;
            }

            if (!string.IsNullOrWhiteSpace(
                    description))
            {
                return description;
            }

            if (!string.IsNullOrWhiteSpace(
                    executablePath))
            {
                try
                {
                    string fileName =
                        Path.GetFileNameWithoutExtension(
                            executablePath);

                    if (!string.IsNullOrWhiteSpace(
                            fileName))
                    {
                        return fileName;
                    }
                }
                catch
                {
                    // Continuăm cu numele implicit.
                }
            }

            return LocalizationHelper.Get(
                "StartupUnknownApplication");
        }

        private sealed class StartupRegistryLocation
        {
            public StartupRegistryLocation(
                RegistryHive hive,
                string activePath,
                string disabledPath,
                string sourceResourceKey,
                RegistryView registryView)
            {
                Hive =
                    hive;

                ActivePath =
                    activePath;

                DisabledPath =
                    disabledPath;

                SourceResourceKey =
                    sourceResourceKey;

                RegistryView =
                    registryView;
            }

            public RegistryHive Hive
            {
                get;
            }

            public string ActivePath
            {
                get;
            }

            public string DisabledPath
            {
                get;
            }

            public string SourceResourceKey
            {
                get;
            }

            public RegistryView RegistryView
            {
                get;
            }
        }

        private sealed class StartupCommandInfo
        {
            public string ExecutablePath
            {
                get;
                init;
            } =
                string.Empty;

            public string Arguments
            {
                get;
                init;
            } =
                string.Empty;
        }

        private sealed class FileMetadata
        {
            public string Publisher
            {
                get;
                init;
            } =
                string.Empty;

            public string Description
            {
                get;
                init;
            } =
                string.Empty;

            public string FileVersion
            {
                get;
                init;
            } =
                string.Empty;
        }
    }
}