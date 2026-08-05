using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using WinBoost.App.Models;

namespace WinBoost.App.Services.Optimization
{
    public sealed class WindowsErrorReportsCleanerService
    {
        public Task<OptimizationResult> CleanAsync()
        {
            return Task.Run(() =>
            {
                long deletedFiles = 0;
                long recoveredBytes = 0;

                try
                {
                    foreach (string directoryPath
                             in GetErrorReportDirectories())
                    {
                        if (!Directory.Exists(
                                directoryPath))
                        {
                            continue;
                        }

                        CleanDirectoryContents(
                            directoryPath,
                            ref deletedFiles,
                            ref recoveredBytes);
                    }

                    return new OptimizationResult
                    {
                        OperationId =
                            "windows-error-reports",

                        OperationName =
                            "Windows Error Reports",

                        RequiresAdministrator =
                            false,

                        IsSuccessful =
                            true,

                        DeletedFilesCount =
                            deletedFiles,

                        RecoveredBytes =
                            recoveredBytes,

                        Message =
                            deletedFiles > 0
                                ? $"Rapoartele de eroare Windows au fost curățate. " +
                                  $"{deletedFiles} fișiere au fost eliminate."
                                : "Nu au fost găsite rapoarte de eroare care să poată fi șterse."
                    };
                }
                catch (Exception ex)
                {
                    return new OptimizationResult
                    {
                        OperationId =
                            "windows-error-reports",

                        OperationName =
                            "Windows Error Reports",

                        RequiresAdministrator =
                            false,

                        IsSuccessful =
                            false,

                        DeletedFilesCount =
                            deletedFiles,

                        RecoveredBytes =
                            recoveredBytes,

                        Message =
                            "Rapoartele de eroare Windows nu au putut fi curățate: " +
                            ex.Message
                    };
                }
            });
        }

        private static IEnumerable<string>
            GetErrorReportDirectories()
        {
            string localAppData =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData);

            string programData =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.CommonApplicationData);

            yield return Path.Combine(
                localAppData,
                "Microsoft",
                "Windows",
                "WER");

            yield return Path.Combine(
                programData,
                "Microsoft",
                "Windows",
                "WER");
        }

        private static void CleanDirectoryContents(
            string directoryPath,
            ref long deletedFiles,
            ref long recoveredBytes)
        {
            var options =
                new EnumerationOptions
                {
                    RecurseSubdirectories =
                        true,

                    IgnoreInaccessible =
                        true,

                    AttributesToSkip =
                        FileAttributes.ReparsePoint
                };

            foreach (string filePath
                     in Directory.EnumerateFiles(
                         directoryPath,
                         "*",
                         options))
            {
                TryDeleteFile(
                    filePath,
                    ref deletedFiles,
                    ref recoveredBytes);
            }

            DeleteEmptyDirectories(
                directoryPath);
        }

        private static void TryDeleteFile(
            string filePath,
            ref long deletedFiles,
            ref long recoveredBytes)
        {
            try
            {
                var fileInfo =
                    new FileInfo(
                        filePath);

                long fileSize =
                    fileInfo.Exists
                        ? fileInfo.Length
                        : 0;

                File.SetAttributes(
                    filePath,
                    FileAttributes.Normal);

                File.Delete(
                    filePath);

                deletedFiles++;
                recoveredBytes +=
                    fileSize;
            }
            catch
            {
                // Fișierele folosite sau protejate
                // sunt ignorate în siguranță.
            }
        }

        private static void DeleteEmptyDirectories(
            string rootDirectory)
        {
            try
            {
                string[] directories =
                    Directory.GetDirectories(
                        rootDirectory,
                        "*",
                        SearchOption.AllDirectories);

                Array.Sort(
                    directories,
                    (left, right) =>
                        right.Length.CompareTo(
                            left.Length));

                foreach (string directoryPath
                         in directories)
                {
                    try
                    {
                        if (!Directory.EnumerateFileSystemEntries(
                                directoryPath)
                            .GetEnumerator()
                            .MoveNext())
                        {
                            Directory.Delete(
                                directoryPath,
                                recursive: false);
                        }
                    }
                    catch
                    {
                        // Folderele protejate sau aflate în uz
                        // sunt păstrate.
                    }
                }
            }
            catch
            {
                // Curățarea fișierelor rămâne validă
                // chiar dacă unele foldere nu pot fi eliminate.
            }
        }
    }
}