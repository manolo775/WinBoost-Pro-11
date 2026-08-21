using System;
using System.Collections.Generic;
using System.IO;

namespace WinBoost.SelfUpdateWorker
{
    internal static class UpdateInstallationManager
    {
        public static void Install(
            string stagingDirectory,
            string targetDirectory,
            string backupDirectory)
        {
            string staging =
                NormalizeDirectory(
                    stagingDirectory);

            string target =
                NormalizeDirectory(
                    targetDirectory);

            string backup =
                NormalizeDirectory(
                    backupDirectory);

            if (!Directory.Exists(
                    staging))
            {
                throw new DirectoryNotFoundException(
                    "The update staging directory was not found.");
            }

            if (!Directory.Exists(
                    target))
            {
                throw new DirectoryNotFoundException(
                    "The WinBoost target directory was not found.");
            }

            if (!Directory.Exists(
                    backup))
            {
                throw new DirectoryNotFoundException(
                    "The WinBoost backup directory was not found.");
            }

            UpdateLogger.Write(
                $"Installation started. Staging: {staging}");

            UpdateLogger.Write(
                $"Installation target: {target}");

            try
            {
                CopyDirectory(
                    staging,
                    target);

                UpdateLogger.Write(
                    "Update files copied successfully.");
            }
            catch (Exception installationException)
            {
                UpdateLogger.WriteException(
                    "Update file installation failed",
                    installationException);

                UpdateLogger.Write(
                    "Rollback started.");

                try
                {
                    RestoreBackup(
                        backup,
                        target);

                    UpdateLogger.Write(
                        "Rollback completed successfully.");
                }
                catch (Exception rollbackException)
                {
                    UpdateLogger.WriteException(
                        "Rollback failed",
                        rollbackException);

                    throw new InvalidOperationException(
                        "The update installation failed and rollback also failed.",
                        new AggregateException(
                            installationException,
                            rollbackException));
                }

                throw new InvalidOperationException(
                    "The update installation failed. The previous WinBoost version was restored.",
                    installationException);
            }
        }

        private static void RestoreBackup(
            string backupDirectory,
            string targetDirectory)
        {
            UpdateLogger.Write(
                "Restoring previous WinBoost installation.");

            HashSet<string> backupFiles =
                GetRelativeFilePaths(
                    backupDirectory);

            foreach (string targetFile
                in EnumerateFilesSafely(
                    targetDirectory))
            {
                string relativePath =
                    Path.GetRelativePath(
                        targetDirectory,
                        targetFile);

                if (!backupFiles.Contains(
                        relativePath))
                {
                    File.Delete(
                        targetFile);

                    UpdateLogger.Write(
                        $"Removed update-only file during rollback: {relativePath}");
                }
            }

            CopyDirectory(
                backupDirectory,
                targetDirectory);

            DeleteEmptyDirectories(
                targetDirectory);

            UpdateLogger.Write(
                "Previous WinBoost installation restored.");
        }

        private static HashSet<string>
            GetRelativeFilePaths(
                string rootDirectory)
        {
            var result =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (string file
                in EnumerateFilesSafely(
                    rootDirectory))
            {
                result.Add(
                    Path.GetRelativePath(
                        rootDirectory,
                        file));
            }

            return result;
        }

        private static void CopyDirectory(
            string sourceDirectory,
            string destinationDirectory)
        {
            Directory.CreateDirectory(
                destinationDirectory);

            string destinationRoot =
                NormalizeDirectory(
                    destinationDirectory);

            CopyDirectoryRecursive(
                sourceDirectory,
                sourceDirectory,
                destinationRoot);
        }

        private static void CopyDirectoryRecursive(
            string rootSourceDirectory,
            string currentSourceDirectory,
            string destinationRoot)
        {
            foreach (string sourceFile
                in Directory.GetFiles(
                    currentSourceDirectory))
            {
                FileAttributes attributes =
                    File.GetAttributes(
                        sourceFile);

                if ((attributes &
                     FileAttributes.ReparsePoint) != 0)
                {
                    continue;
                }

                string relativePath =
                    Path.GetRelativePath(
                        rootSourceDirectory,
                        sourceFile);

                string destinationFile =
                    GetSafeDestinationPath(
                        destinationRoot,
                        relativePath);

                string? destinationDirectory =
                    Path.GetDirectoryName(
                        destinationFile);

                if (!string.IsNullOrWhiteSpace(
                        destinationDirectory))
                {
                    Directory.CreateDirectory(
                        destinationDirectory);
                }

                File.Copy(
                    sourceFile,
                    destinationFile,
                    overwrite: true);
            }

            foreach (string sourceSubdirectory
                in Directory.GetDirectories(
                    currentSourceDirectory))
            {
                FileAttributes attributes =
                    File.GetAttributes(
                        sourceSubdirectory);

                if ((attributes &
                     FileAttributes.ReparsePoint) != 0)
                {
                    continue;
                }

                CopyDirectoryRecursive(
                    rootSourceDirectory,
                    sourceSubdirectory,
                    destinationRoot);
            }
        }

        private static IEnumerable<string>
            EnumerateFilesSafely(
                string rootDirectory)
        {
            foreach (string file
                in Directory.GetFiles(
                    rootDirectory))
            {
                FileAttributes attributes =
                    File.GetAttributes(
                        file);

                if ((attributes &
                     FileAttributes.ReparsePoint) == 0)
                {
                    yield return file;
                }
            }

            foreach (string directory
                in Directory.GetDirectories(
                    rootDirectory))
            {
                FileAttributes attributes =
                    File.GetAttributes(
                        directory);

                if ((attributes &
                     FileAttributes.ReparsePoint) != 0)
                {
                    continue;
                }

                foreach (string file
                    in EnumerateFilesSafely(
                        directory))
                {
                    yield return file;
                }
            }
        }

        private static string GetSafeDestinationPath(
            string destinationRoot,
            string relativePath)
        {
            string destinationPath =
                Path.GetFullPath(
                    Path.Combine(
                        destinationRoot,
                        relativePath));

            if (!destinationPath.StartsWith(
                    destinationRoot,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    "The update contains an unsafe destination path.");
            }

            return destinationPath;
        }

        private static string NormalizeDirectory(
            string directoryPath)
        {
            return Path.GetFullPath(
                       directoryPath)
                   .TrimEnd(
                       Path.DirectorySeparatorChar,
                       Path.AltDirectorySeparatorChar)
                   + Path.DirectorySeparatorChar;
        }

        private static void DeleteEmptyDirectories(
            string rootDirectory)
        {
            foreach (string directory
                in Directory.GetDirectories(
                    rootDirectory,
                    "*",
                    SearchOption.TopDirectoryOnly))
            {
                FileAttributes attributes =
                    File.GetAttributes(
                        directory);

                if ((attributes &
                     FileAttributes.ReparsePoint) != 0)
                {
                    continue;
                }

                DeleteEmptyDirectories(
                    directory);

                if (Directory.GetFiles(
                        directory).Length == 0 &&
                    Directory.GetDirectories(
                        directory).Length == 0)
                {
                    Directory.Delete(
                        directory);
                }
            }
        }
    }
}