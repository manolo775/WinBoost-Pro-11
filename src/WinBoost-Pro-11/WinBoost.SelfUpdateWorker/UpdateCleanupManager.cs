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