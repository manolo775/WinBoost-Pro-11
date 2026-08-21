using System;
using System.IO;

namespace WinBoost.SelfUpdateWorker
{
    internal static class UpdateBackupManager
    {
        public static string CreateBackup(
            string targetDirectory)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(
                targetDirectory);

            string sourceDirectory =
                Path.GetFullPath(
                    targetDirectory);

            if (!Directory.Exists(
                    sourceDirectory))
            {
                throw new DirectoryNotFoundException(
                    "The WinBoost installation directory was not found.");
            }

            string backupRoot =
                Path.Combine(
                    Path.GetTempPath(),
                    "WinBoost",
                    "SelfUpdateBackup");

            Directory.CreateDirectory(
                backupRoot);

            string backupDirectory =
                Path.Combine(
                    backupRoot,
                    Guid.NewGuid()
                        .ToString("N"));

            Directory.CreateDirectory(
                backupDirectory);

            try
            {
                CopyDirectory(
                    sourceDirectory,
                    backupDirectory);

                return backupDirectory;
            }
            catch
            {
                TryDeleteDirectory(
                    backupDirectory);

                throw;
            }
        }

        private static void CopyDirectory(
            string sourceDirectory,
            string destinationDirectory)
        {
            Directory.CreateDirectory(
                destinationDirectory);

            foreach (string sourceFile
                in Directory.GetFiles(
                    sourceDirectory))
            {
                string fileName =
                    Path.GetFileName(
                        sourceFile);

                string destinationFile =
                    Path.Combine(
                        destinationDirectory,
                        fileName);

                File.Copy(
                    sourceFile,
                    destinationFile,
                    overwrite: true);
            }

            foreach (string sourceSubdirectory
                in Directory.GetDirectories(
                    sourceDirectory))
            {
                FileAttributes attributes =
                    File.GetAttributes(
                        sourceSubdirectory);

                if ((attributes &
                     FileAttributes.ReparsePoint) != 0)
                {
                    continue;
                }

                string directoryName =
                    Path.GetFileName(
                        sourceSubdirectory);

                string destinationSubdirectory =
                    Path.Combine(
                        destinationDirectory,
                        directoryName);

                CopyDirectory(
                    sourceSubdirectory,
                    destinationSubdirectory);
            }
        }

        public static void TryDeleteDirectory(
            string directoryPath)
        {
            try
            {
                if (Directory.Exists(
                        directoryPath))
                {
                    Directory.Delete(
                        directoryPath,
                        recursive: true);
                }
            }
            catch
            {
                // Cleanup failure must not hide
                // the original update result.
            }
        }
    }
}