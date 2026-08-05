using System;
using System.IO;
using System.Threading.Tasks;
using WinBoost.App.Models;

namespace WinBoost.App.Services.Optimization
{
    public sealed class ThumbnailCacheCleanerService
    {
        public Task<OptimizationResult> CleanAsync()
        {
            return Task.Run(() =>
            {
                long deletedFiles = 0;
                long recoveredBytes = 0;

                try
                {
                    string localAppData =
                        Environment.GetFolderPath(
                            Environment.SpecialFolder.LocalApplicationData);

                    string explorerCachePath =
                        Path.Combine(
                            localAppData,
                            "Microsoft",
                            "Windows",
                            "Explorer");

                    if (!Directory.Exists(
                            explorerCachePath))
                    {
                        return new OptimizationResult
                        {
                            OperationId = "thumbnail-cache",
                            OperationName = "Thumbnail Cache",
                            RequiresAdministrator = false,
                            IsSuccessful = true,
                            Message =
                                "Cache-ul de miniaturi nu există."
                        };
                    }

                    foreach (string filePath in
                             Directory.EnumerateFiles(
                                 explorerCachePath,
                                 "thumbcache_*.db",
                                 SearchOption.TopDirectoryOnly))
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
                            // Fișierele folosite de Windows Explorer
                            // sunt ignorate în siguranță.
                        }
                    }

                    return new OptimizationResult
                    {
                        OperationId = "thumbnail-cache",
                        OperationName = "Thumbnail Cache",
                        RequiresAdministrator = false,
                        IsSuccessful = true,
                        DeletedFilesCount = deletedFiles,
                        RecoveredBytes = recoveredBytes,
                        Message =
                            deletedFiles > 0
                                ? $"Cache-ul de miniaturi a fost curățat. " +
                                  $"{deletedFiles} fișiere au fost eliminate."
                                : "Nu au fost găsite fișiere de miniaturi care să poată fi șterse."
                    };
                }
                catch (Exception ex)
                {
                    return new OptimizationResult
                    {
                        OperationId = "thumbnail-cache",
                        OperationName = "Thumbnail Cache",
                        RequiresAdministrator = false,
                        IsSuccessful = false,
                        DeletedFilesCount = deletedFiles,
                        RecoveredBytes = recoveredBytes,
                        Message =
                            "Cache-ul de miniaturi nu a putut fi curățat: " +
                            ex.Message
                    };
                }
            });
        }
    }
}