using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;

namespace WinBoost.SelfUpdateWorker
{
    internal static class Program
    {
        private static int Main(
            string[] args)
        {
            UpdateLogger.Write(
                "Self Update Worker started.");

            UpdateCleanupManager
                .CleanupOldTemporaryData();

            UpdateLogger.Write(
                "Old updater temporary data cleanup completed.");

            try
            {
                Dictionary<string, string>
                    arguments =
                        ParseArguments(args);

                // ======================================
                // PACKAGE
                // ======================================

                if (!arguments.TryGetValue(
                        "package",
                        out string? packagePath) ||
                    string.IsNullOrWhiteSpace(
                        packagePath))
                {
                    UpdateLogger.Write(
                        "Missing --package argument.");

                    Console.Error.WriteLine(
                        "Missing --package argument.");

                    return 10;
                }

                // ======================================
                // TARGET DIRECTORY
                // ======================================

                if (!arguments.TryGetValue(
                        "target-dir",
                        out string? targetDirectory) ||
                    string.IsNullOrWhiteSpace(
                        targetDirectory))
                {
                    UpdateLogger.Write(
                        "Missing --target-dir argument.");

                    Console.Error.WriteLine(
                        "Missing --target-dir argument.");

                    return 11;
                }

                // ======================================
                // PARENT PROCESS ID
                // ======================================

                if (!arguments.TryGetValue(
                        "parent-pid",
                        out string? parentPidText) ||
                    !int.TryParse(
                        parentPidText,
                        out int parentProcessId) ||
                    parentProcessId <= 0)
                {
                    UpdateLogger.Write(
                        "Invalid --parent-pid argument.");

                    Console.Error.WriteLine(
                        "Invalid --parent-pid argument.");

                    return 12;
                }

                // ======================================
                // EXPECTED SHA-256
                // ======================================

                if (!arguments.TryGetValue(
                        "sha256",
                        out string? expectedSha256) ||
                    string.IsNullOrWhiteSpace(
                        expectedSha256))
                {
                    UpdateLogger.Write(
                        "Missing --sha256 argument.");

                    Console.Error.WriteLine(
                        "Missing --sha256 argument.");

                    return 16;
                }

                expectedSha256 =
                    expectedSha256
                        .Trim()
                        .Replace(
                            " ",
                            string.Empty)
                        .ToLowerInvariant();

                if (expectedSha256.Length != 64)
                {
                    UpdateLogger.Write(
                        "Invalid --sha256 argument.");

                    Console.Error.WriteLine(
                        "Invalid --sha256 argument.");

                    return 17;
                }

                // ======================================
                // NORMALIZE PATHS
                // ======================================

                packagePath =
                    Path.GetFullPath(
                        packagePath);

                targetDirectory =
                    Path.GetFullPath(
                        targetDirectory);

                // ======================================
                // VALIDATE PACKAGE
                // ======================================

                if (!File.Exists(
                        packagePath))
                {
                    UpdateLogger.Write(
                        $"Update package does not exist: {packagePath}");

                    Console.Error.WriteLine(
                        "Update package does not exist.");

                    return 13;
                }

                // ======================================
                // VALIDATE TARGET DIRECTORY
                // ======================================

                if (!Directory.Exists(
                        targetDirectory))
                {
                    UpdateLogger.Write(
                        $"Target directory does not exist: {targetDirectory}");

                    Console.Error.WriteLine(
                        "Target directory does not exist.");

                    return 14;
                }

                // ======================================
                // VERIFY SHA-256
                // ======================================

                string actualSha256 =
                    CalculateSha256(
                        packagePath);

                if (!string.Equals(
                        expectedSha256,
                        actualSha256,
                        StringComparison.OrdinalIgnoreCase))
                {
                    UpdateLogger.Write(
                        "Update package SHA-256 verification failed.");

                    Console.Error.WriteLine(
                        "Update package SHA-256 verification failed.");

                    return 18;
                }

                Console.WriteLine(
                    "Update package SHA-256 verified.");

                UpdateLogger.Write(
                    "Update package SHA-256 verified successfully.");

                // ======================================
                // INFORMATION
                // ======================================

                Console.WriteLine(
                    "WinBoost Self Update Worker");

                Console.WriteLine(
                    $"Package: {packagePath}");

                Console.WriteLine(
                    $"Target directory: {targetDirectory}");

                Console.WriteLine(
                    $"Parent PID: {parentProcessId}");

                // ======================================
                // WAIT FOR WINBOOST TO CLOSE
                // ======================================

                Console.WriteLine(
                    "Waiting for WinBoost to close...");

                UpdateLogger.Write(
                    $"Waiting for WinBoost process {parentProcessId} to close.");

                bool parentExited =
                    WaitForParentProcessExit(
                        parentProcessId,
                        TimeSpan.FromSeconds(30));

                if (!parentExited)
                {
                    UpdateLogger.Write(
                        "WinBoost did not close within the safety timeout.");

                    Console.Error.WriteLine(
                        "WinBoost did not close within the safety timeout.");

                    return 15;
                }

                Console.WriteLine(
                    "WinBoost process is closed.");

                UpdateLogger.Write(
                    "WinBoost process closed successfully.");

                // ======================================
                // EXTRACT UPDATE PACKAGE TO STAGING
                // ======================================

                Console.WriteLine(
                    "Extracting update package...");

                string stagingDirectory =
                    UpdatePackageExtractor
                        .ExtractToStaging(
                            packagePath);

                Console.WriteLine(
                    "Update package extracted successfully.");

                Console.WriteLine(
                    $"Staging directory: {stagingDirectory}");

                UpdateLogger.Write(
                    $"Update package extracted to staging: {stagingDirectory}");

                // ======================================
                // CREATE INSTALLATION BACKUP
                // ======================================

                Console.WriteLine(
                    "Creating WinBoost backup...");

                string backupDirectory =
                    UpdateBackupManager
                        .CreateBackup(
                            targetDirectory);

                Console.WriteLine(
                    "WinBoost backup created successfully.");

                Console.WriteLine(
                    $"Backup directory: {backupDirectory}");

                UpdateLogger.Write(
                    $"WinBoost backup created: {backupDirectory}");

                // ======================================
                // INSTALL UPDATE
                // ======================================

                Console.WriteLine(
                    "Installing WinBoost update...");

                UpdateLogger.Write(
                    "Installing WinBoost update.");

                UpdateInstallationManager
                    .Install(
                        stagingDirectory,
                        targetDirectory,
                        backupDirectory);

                Console.WriteLine(
                    "WinBoost update installed successfully.");

                UpdateLogger.Write(
                    "WinBoost update installed successfully.");

                Console.WriteLine(
                    $"Backup directory: {backupDirectory}");

                // ======================================
                // RESTART WINBOOST
                // ======================================

                Console.WriteLine(
                    "Restarting WinBoost...");

                UpdateLogger.Write(
                    "Restarting WinBoost.");

                UpdateRestartManager
                    .Restart(
                        targetDirectory);

                Console.WriteLine(
                    "WinBoost restarted successfully.");

                UpdateLogger.Write(
                    "WinBoost restarted successfully.");

                return 0;
            }
            catch (Exception ex)
            {
                UpdateLogger.WriteException(
                    "Self update failed",
                    ex);

                Console.Error.WriteLine(
                    "WinBoost Self Update Worker failed.");

                Console.Error.WriteLine(
                    ex.Message);

                return 100;
            }
        }

        // ======================================
        // SHA-256
        // ======================================

        private static string CalculateSha256(
            string filePath)
        {
            using FileStream stream =
                File.OpenRead(
                    filePath);

            using SHA256 sha256 =
                SHA256.Create();

            byte[] hash =
                sha256.ComputeHash(
                    stream);

            return Convert
                .ToHexString(hash)
                .ToLowerInvariant();
        }

        // ======================================
        // WAIT FOR PARENT PROCESS
        // ======================================

        private static bool WaitForParentProcessExit(
            int processId,
            TimeSpan timeout)
        {
            try
            {
                using Process process =
                    Process.GetProcessById(
                        processId);

                if (process.HasExited)
                {
                    return true;
                }

                int timeoutMilliseconds =
                    checked(
                        (int)timeout.TotalMilliseconds);

                return process.WaitForExit(
                    timeoutMilliseconds);
            }
            catch (ArgumentException)
            {
                // Procesul nu mai există.
                // Pentru updater înseamnă că
                // WinBoost este deja închis.
                return true;
            }
        }

        // ======================================
        // ARGUMENT PARSER
        // ======================================

        private static Dictionary<string, string>
            ParseArguments(
                string[] args)
        {
            var result =
                new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (string argument in args)
            {
                if (string.IsNullOrWhiteSpace(
                        argument) ||
                    !argument.StartsWith(
                        "--",
                        StringComparison.Ordinal))
                {
                    continue;
                }

                int separatorIndex =
                    argument.IndexOf('=');

                if (separatorIndex <= 2)
                {
                    continue;
                }

                string key =
                    argument.Substring(
                            2,
                            separatorIndex - 2)
                        .Trim();

                string value =
                    argument.Substring(
                            separatorIndex + 1)
                        .Trim()
                        .Trim('"');

                if (string.IsNullOrWhiteSpace(
                        key))
                {
                    continue;
                }

                result[key] =
                    value;
            }

            return result;
        }
    }
}