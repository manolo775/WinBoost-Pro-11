using System;
using System.IO;
using System.IO.Compression;

namespace WinBoost.SelfUpdateWorker
{
    internal static class UpdatePackageExtractor
    {
        public static string ExtractToStaging(
            string packagePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(
                packagePath);

            if (!File.Exists(
                    packagePath))
            {
                throw new FileNotFoundException(
                    "Update package was not found.",
                    packagePath);
            }

            string stagingRoot =
                Path.Combine(
                    Path.GetTempPath(),
                    "WinBoost",
                    "SelfUpdate");

            Directory.CreateDirectory(
                stagingRoot);

            string stagingDirectory =
                Path.Combine(
                    stagingRoot,
                    Guid.NewGuid()
                        .ToString("N"));

            Directory.CreateDirectory(
                stagingDirectory);

            string stagingDirectoryWithSeparator =
                Path.GetFullPath(
                    stagingDirectory) +
                Path.DirectorySeparatorChar;

            try
            {
                using ZipArchive archive =
                    ZipFile.OpenRead(
                        packagePath);

                if (archive.Entries.Count == 0)
                {
                    throw new InvalidDataException(
                        "The update package is empty.");
                }

                foreach (ZipArchiveEntry entry
                    in archive.Entries)
                {
                    string destinationPath =
                        Path.GetFullPath(
                            Path.Combine(
                                stagingDirectory,
                                entry.FullName));

                    if (!destinationPath.StartsWith(
                            stagingDirectoryWithSeparator,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidDataException(
                            "The update package contains an unsafe path.");
                    }

                    bool isDirectory =
                        string.IsNullOrEmpty(
                            entry.Name);

                    if (isDirectory)
                    {
                        Directory.CreateDirectory(
                            destinationPath);

                        continue;
                    }

                    string? destinationDirectory =
                        Path.GetDirectoryName(
                            destinationPath);

                    if (!string.IsNullOrWhiteSpace(
                            destinationDirectory))
                    {
                        Directory.CreateDirectory(
                            destinationDirectory);
                    }

                    entry.ExtractToFile(
                        destinationPath,
                        overwrite: true);
                }

                return stagingDirectory;
            }
            catch
            {
                TryDeleteDirectory(
                    stagingDirectory);

                throw;
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
                // the original update operation result.
            }
        }
    }
}