using System;
using System.IO;

namespace WinBoost.SelfUpdateWorker
{
    internal static class UpdateCleanupManager
    {
        private static readonly TimeSpan RetentionPeriod =
            TimeSpan.FromDays(7);

        public static void CleanupOldTemporaryData()
        {
            string winBoostTempRoot =
                Path.Combine(
                    Path.GetTempPath(),
                    "WinBoost");

            CleanupDirectory(
                Path.Combine(
                    winBoostTempRoot,
                    "SelfUpdate"));

            CleanupDirectory(
                Path.Combine(
                    winBoostTempRoot,
                    "SelfUpdateBackup"));

            CleanupDirectory(
                Path.Combine(
                    winBoostTempRoot,
                    "SelfUpdateWorker"));
        }

        public static void CleanupOldDownloadedPackages(
            string? protectedPackagePath)
        {
            string updatesDirectory =
                Path.Combine(
                    Path.GetTempPath(),
                    "WinBoost",
                    "Updates");

            if (!Directory.Exists(
                    updatesDirectory))
            {
                return;
            }

            string? normalizedProtectedPackagePath =
                null;

            if (!string.IsNullOrWhiteSpace(
                    protectedPackagePath))
            {
                try
                {
                    normalizedProtectedPackagePath =
                        Path.GetFullPath(
                            protectedPackagePath);
                }
                catch
                {
                    normalizedProtectedPackagePath =
                        null;
                }
            }

            DateTime thresholdUtc =
                DateTime.UtcNow
                    .Subtract(
                        RetentionPeriod);

            foreach (string file
                in Directory.GetFiles(
                    updatesDirectory))
            {
                try
                {
                    string normalizedFilePath =
                        Path.GetFullPath(
                            file);

                    if (!string.IsNullOrWhiteSpace(
                            normalizedProtectedPackagePath) &&
                        string.Equals(
                            normalizedFilePath,
                            normalizedProtectedPackagePath,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    FileInfo fileInfo =
                        new FileInfo(
                            normalizedFilePath);

                    if (fileInfo.LastWriteTimeUtc >=
                        thresholdUtc)
                    {
                        continue;
                    }

                    File.Delete(
                        normalizedFilePath);

                    UpdateLogger.Write(
                        $"Removed old downloaded update package: {normalizedFilePath}");
                }
                catch (Exception ex)
                {
                    UpdateLogger.WriteException(
                        $"Could not remove old downloaded update package: {file}",
                        ex);
                }
            }
        }

        private static void CleanupDirectory(
            string rootDirectory)
        {
            if (!Directory.Exists(
                    rootDirectory))
            {
                return;
            }

            DateTime thresholdUtc =
                DateTime.UtcNow
                    .Subtract(
                        RetentionPeriod);

            foreach (string directory
                in Directory.GetDirectories(
                    rootDirectory))
            {
                try
                {
                    DirectoryInfo directoryInfo =
                        new DirectoryInfo(
                            directory);

                    if (directoryInfo.LastWriteTimeUtc >=
                        thresholdUtc)
                    {
                        continue;
                    }

                    Directory.Delete(
                        directory,
                        recursive: true);

                    UpdateLogger.Write(
                        $"Removed old updater temporary directory: {directory}");
                }
                catch (Exception ex)
                {
                    UpdateLogger.WriteException(
                        $"Could not remove old updater temporary directory: {directory}",
                        ex);
                }
            }
        }
    }
}