using System;
using System.IO;
using System.Threading.Tasks;
using WinBoost.App.Localization;
using WinBoost.App.Models;
using WinBoost.App.Localization;

namespace WinBoost.App.Services.Optimization
{
    public class TempFilesCleanerService
    {
        public Task<OptimizationResult> CleanUserTempAsync()
        {
            return Task.Run(() =>
            {
                string tempPath = Path.GetTempPath();

                long deletedFilesCount = 0;
                long recoveredBytes = 0;

                try
                {
                    if (!Directory.Exists(tempPath))
                    {
                        return new OptimizationResult
                        {

                            OperationId = "temp-files",
                            OperationName =
                               LocalizationHelper.Get(
                                  "OptimizationTemporaryFilesOperation"),
                            RequiresAdministrator = false,

                            IsSuccessful = true,
                            Message =
                                "Folderul temporar al utilizatorului nu există."
                        };
                    }

                    CleanDirectoryContents(
                        tempPath,
                        ref deletedFilesCount,
                        ref recoveredBytes);

                    return new OptimizationResult
                    {

                        OperationId = "temp-files",
                        OperationName =
                                    LocalizationHelper.Get(
                                   "OptimizationTemporaryFilesOperation"),
                        RequiresAdministrator = false,

                        IsSuccessful = true,
                        DeletedFilesCount = deletedFilesCount,
                        RecoveredBytes = recoveredBytes,
                        Message =
                            $"Curățarea a fost finalizată. " +
                            $"{deletedFilesCount} fișiere au fost eliminate."
                    };
                }
                catch (Exception ex)
                {
                    return new OptimizationResult
                    {

                        OperationId = "temp-files",
                        OperationName =
                                 LocalizationHelper.Get(
                                 "OptimizationTemporaryFilesOperation"),
                        RequiresAdministrator = false,

                        IsSuccessful = false,
                        DeletedFilesCount = deletedFilesCount,
                        RecoveredBytes = recoveredBytes,
                        Message =
                            $"Curățarea nu a putut fi finalizată: {ex.Message}"
                    };
                }
            });
        }

        private static void CleanDirectoryContents(
            string directoryPath,
            ref long deletedFilesCount,
            ref long recoveredBytes)
        {
            foreach (string filePath
                     in Directory.EnumerateFiles(directoryPath))
            {
                TryDeleteFile(
                    filePath,
                    ref deletedFilesCount,
                    ref recoveredBytes);
            }

            foreach (string subdirectoryPath
                     in Directory.EnumerateDirectories(directoryPath))
            {
                try
                {
                    CleanDirectoryContents(
                        subdirectoryPath,
                        ref deletedFilesCount,
                        ref recoveredBytes);

                    Directory.Delete(
                        subdirectoryPath,
                        recursive: false);
                }
                catch
                {
                    // Unele foldere sunt folosite de Windows
                    // sau de alte aplicații și nu pot fi șterse.
                }
            }
        }

        private static void TryDeleteFile(
            string filePath,
            ref long deletedFilesCount,
            ref long recoveredBytes)
        {
            try
            {
                var fileInfo =
                    new FileInfo(filePath);

                long fileLength =
                    fileInfo.Exists
                        ? fileInfo.Length
                        : 0;

                File.SetAttributes(
                    filePath,
                    FileAttributes.Normal);

                File.Delete(filePath);

                deletedFilesCount++;
                recoveredBytes += fileLength;
            }
            catch
            {
                // Fișierele aflate în uz sau protejate
                // sunt ignorate în siguranță.
            }
        }
    }
}