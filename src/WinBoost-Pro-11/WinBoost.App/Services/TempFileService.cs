using System;
using System.IO;
using System.Threading.Tasks;

namespace WinBoost.App.Services
{
    public class TempFileService
    {
        public Task<(int FileCount, long TotalBytes)> AnalyzeAsync()
        {
            return Task.Run(() =>
            {
                int fileCount = 0;
                long totalBytes = 0;

                string tempPath =
                    Path.GetFullPath(Path.GetTempPath());

                DateTime limit =
                    DateTime.UtcNow.AddDays(-1);

                foreach (string filePath in
                         EnumerateTempFiles(tempPath))
                {
                    try
                    {
                        var file = new FileInfo(filePath);

                        if (file.LastWriteTimeUtc >= limit)
                            continue;

                        fileCount++;
                        totalBytes += file.Length;
                    }
                    catch
                    {
                        // Fișierele inaccesibile sunt ignorate.
                    }
                }

                return (fileCount, totalBytes);
            });
        }

        public Task<(int DeletedFiles, long FreedBytes)> CleanAsync()
        {
            return Task.Run(() =>
            {
                int deletedFiles = 0;
                long freedBytes = 0;

                string tempPath =
                    Path.GetFullPath(Path.GetTempPath());

                string safeRoot =
                    Path.TrimEndingDirectorySeparator(tempPath) +
                    Path.DirectorySeparatorChar;

                DateTime limit =
                    DateTime.UtcNow.AddDays(-1);

                foreach (string filePath in
                         EnumerateTempFiles(tempPath))
                {
                    try
                    {
                        string fullPath =
                            Path.GetFullPath(filePath);

                        if (!fullPath.StartsWith(
                                safeRoot,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        var file = new FileInfo(fullPath);

                        if (file.LastWriteTimeUtc >= limit)
                            continue;

                        long fileSize = file.Length;

                        file.Delete();

                        deletedFiles++;
                        freedBytes += fileSize;
                    }
                    catch
                    {
                        // Fișierele folosite de Windows sau de alte
                        // aplicații sunt păstrate și ignorate.
                    }
                }

                return (deletedFiles, freedBytes);
            });
        }

        private static System.Collections.Generic.IEnumerable<string>
            EnumerateTempFiles(string tempPath)
        {
            var options = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                AttributesToSkip =
                    FileAttributes.ReparsePoint |
                    FileAttributes.System
            };

            return Directory.EnumerateFiles(
                tempPath,
                "*",
                options);
        }
    }
}