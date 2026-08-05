using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using WinBoost.App.Models;

namespace WinBoost.App.Services.Optimization
{
    public sealed class WindowsLogsCleanerService
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
                             in GetLogDirectories())
                    {
                        if (!Directory.Exists(directoryPath))
                        {
                            continue;
                        }

                        CleanDirectory(
                            directoryPath,
                            ref deletedFiles,
                            ref recoveredBytes);
                    }

                    return new OptimizationResult
                    {
                        OperationId =
                            "windows-logs",

                        OperationName =
                            "Windows Logs",

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
                                ? $"Jurnalele Windows au fost curățate. {deletedFiles} fișiere eliminate."
                                : "Nu au fost găsite fișiere jurnal care să poată fi șterse."
                    };
                }
                catch (Exception ex)
                {
                    return new OptimizationResult
                    {
                        OperationId =
                            "windows-logs",

                        OperationName =
                            "Windows Logs",

                        RequiresAdministrator =
                            false,

                        IsSuccessful =
                            false,

                        DeletedFilesCount =
                            deletedFiles,

                        RecoveredBytes =
                            recoveredBytes,

                        Message =
                            "Jurnalele Windows nu au putut fi curățate: " +
                            ex.Message
                    };
                }
            });
        }

        private static IEnumerable<string>
            GetLogDirectories()
        {
            string windowsPath =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.Windows);

            yield return Path.Combine(
                windowsPath,
                "Logs");

            yield return Path.Combine(
                windowsPath,
                "Panther");

            yield return Path.Combine(
                windowsPath,
                "SoftwareDistribution",
                "ReportingEvents");

            yield return Path.Combine(
                windowsPath,
                "INF");
        }

        private static void CleanDirectory(
            string directoryPath,
            ref long deletedFiles,
            ref long recoveredBytes)
        {
            var options =
                new EnumerationOptions
                {
                    RecurseSubdirectories = true,
                    IgnoreInaccessible = true,
                    AttributesToSkip =
                        FileAttributes.ReparsePoint
                };

            foreach (string filePath in
                     Directory.EnumerateFiles(
                         directoryPath,
                         "*",
                         options))
            {
                try
                {
                    var fileInfo =
                        new FileInfo(filePath);

                    long fileSize =
                        fileInfo.Exists
                            ? fileInfo.Length
                            : 0;

                    File.SetAttributes(
                        filePath,
                        FileAttributes.Normal);

                    File.Delete(filePath);

                    deletedFiles++;
                    recoveredBytes +=
                        fileSize;
                }
                catch
                {
                    // Fișierele aflate în uz
                    // sunt ignorate.
                }
            }
        }
    }
}