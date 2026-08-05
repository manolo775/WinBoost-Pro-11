using System;
using System.IO;
using System.Threading.Tasks;
using WinBoost.App.Models;

namespace WinBoost.App.Services.Optimization
{
    public sealed class PrefetchCleanerService
    {
        public Task<OptimizationResult> CleanAsync()
        {
            return Task.Run(() =>
            {
                long deletedFiles = 0;
                long recoveredBytes = 0;

                try
                {
                    string windowsPath =
                        Environment.GetFolderPath(
                            Environment.SpecialFolder.Windows);

                    string prefetchPath =
                        Path.Combine(
                            windowsPath,
                            "Prefetch");

                    if (!Directory.Exists(prefetchPath))
                    {
                        return new OptimizationResult
                        {
                            OperationId = "prefetch",
                            OperationName = "Windows Prefetch",
                            RequiresAdministrator = true,
                            IsSuccessful = true,
                            WasSkipped = true,
                            Message =
                                "Folderul Prefetch nu există."
                        };
                    }

                    foreach (string filePath in
                             Directory.EnumerateFiles(
                                 prefetchPath,
                                 "*",
                                 SearchOption.TopDirectoryOnly))
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
                            // Fișierele aflate în uz sau protejate
                            // sunt ignorate în siguranță.
                        }
                    }

                    return new OptimizationResult
                    {
                        OperationId = "prefetch",
                        OperationName = "Windows Prefetch",
                        RequiresAdministrator = true,
                        IsSuccessful = true,
                        DeletedFilesCount = deletedFiles,
                        RecoveredBytes = recoveredBytes,
                        Message =
                            deletedFiles > 0
                                ? $"Prefetch a fost curățat. " +
                                  $"{deletedFiles} fișiere au fost eliminate."
                                : "Nu au fost găsite fișiere Prefetch care să poată fi șterse."
                    };
                }
                catch (UnauthorizedAccessException)
                {
                    return new OptimizationResult
                    {
                        OperationId = "prefetch",
                        OperationName = "Windows Prefetch",
                        RequiresAdministrator = true,
                        IsSuccessful = false,
                        WasSkipped = true,
                        Message =
                            "Curățarea Prefetch necesită drepturi de administrator."
                    };
                }
                catch (Exception ex)
                {
                    return new OptimizationResult
                    {
                        OperationId = "prefetch",
                        OperationName = "Windows Prefetch",
                        RequiresAdministrator = true,
                        IsSuccessful = false,
                        DeletedFilesCount = deletedFiles,
                        RecoveredBytes = recoveredBytes,
                        Message =
                            "Prefetch nu a putut fi curățat: " +
                            ex.Message
                    };
                }
            });
        }
    }
}