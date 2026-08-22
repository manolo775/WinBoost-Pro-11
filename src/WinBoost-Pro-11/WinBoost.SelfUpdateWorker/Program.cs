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

            UpdateProgressWindowHost? progressWindow =
                null;

            string? recoveryTargetDirectory =
                null;

            int? recoveryParentProcessId =
                null;

            try
            {
                // ======================================
                // CLEANUP OLD TEMPORARY DATA
                // ======================================

                UpdateCleanupManager
                    .CleanupOldTemporaryData();

                UpdateLogger.Write(
                    "Old updater temporary data cleanup completed.");

                Dictionary<string, string>
                    arguments =
                        ParseArguments(args);

                // ======================================
                // PREPARE RECOVERY INFORMATION
                // ======================================

                if (arguments.TryGetValue(
                        "target-dir",
                        out string? recoveryTargetText) &&
                    !string.IsNullOrWhiteSpace(
                        recoveryTargetText))
                {
                    try
                    {
                        recoveryTargetDirectory =
                            Path.GetFullPath(
                                recoveryTargetText);
                    }
                    catch
                    {
                        recoveryTargetDirectory =
                            null;
                    }
                }

                if (arguments.TryGetValue(
                        "parent-pid",
                        out string? recoveryParentPidText) &&
                    int.TryParse(
                        recoveryParentPidText,
                        out int recoveryParentPid) &&
                    recoveryParentPid > 0)
                {
                    recoveryParentProcessId =
                        recoveryParentPid;
                }

                // ======================================
                // PACKAGE
                // ======================================

                if (!arguments.TryGetValue(
                        "package",
                        out string? packagePath) ||
                    string.IsNullOrWhiteSpace(
                        packagePath))
                {
                    return HandleFailure(
                        "Missing --package argument.",
                        10,
                        progressWindow,
                        null,
                        recoveryTargetDirectory,
                        recoveryParentProcessId,
                        tryRestart: true);
                }

                // ======================================
                // TARGET DIRECTORY
                // ======================================

                if (string.IsNullOrWhiteSpace(
                        recoveryTargetDirectory))
                {
                    return HandleFailure(
                        "Missing or invalid --target-dir argument.",
                        11,
                        progressWindow,
                        null,
                        null,
                        recoveryParentProcessId,
                        tryRestart: false);
                }

                string targetDirectory =
                    recoveryTargetDirectory;

                // ======================================
                // PARENT PROCESS ID
                // ======================================

                if (!recoveryParentProcessId.HasValue)
                {
                    return HandleFailure(
                        "Invalid --parent-pid argument.",
                        12,
                        progressWindow,
                        null,
                        targetDirectory,
                        null,
                        tryRestart: false);
                }

                int parentProcessId =
                    recoveryParentProcessId.Value;

                // ======================================
                // EXPECTED SHA-256
                // ======================================

                if (!arguments.TryGetValue(
                        "sha256",
                        out string? expectedSha256) ||
                    string.IsNullOrWhiteSpace(
                        expectedSha256))
                {
                    return HandleFailure(
                        "Missing --sha256 argument.",
                        16,
                        progressWindow,
                        null,
                        targetDirectory,
                        parentProcessId,
                        tryRestart: true);
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
                    return HandleFailure(
                        "Invalid --sha256 argument.",
                        17,
                        progressWindow,
                        null,
                        targetDirectory,
                        parentProcessId,
                        tryRestart: true);
                }

                // ======================================
                // PACKAGE SIGNATURE
                // ======================================

                arguments.TryGetValue(
                    "package-signature",
                    out string? expectedPackageSignature);

                expectedPackageSignature =
                    expectedPackageSignature?
                        .Trim();

                // ======================================
                // NORMALIZE PACKAGE PATH
                // ======================================

                packagePath =
                    Path.GetFullPath(
                        packagePath);

                // ======================================
                // CLEANUP OLD DOWNLOADED PACKAGES
                // ======================================

                UpdateCleanupManager
                    .CleanupOldDownloadedPackages(
                        packagePath);

                UpdateLogger.Write(
                    "Old downloaded update packages cleanup completed.");

                // ======================================
                // VALIDATE PACKAGE
                // ======================================

                if (!File.Exists(
                        packagePath))
                {
                    return HandleFailure(
                        $"Update package does not exist: {packagePath}",
                        13,
                        progressWindow,
                        null,
                        targetDirectory,
                        parentProcessId,
                        tryRestart: true);
                }

                // ======================================
                // VALIDATE TARGET DIRECTORY
                // ======================================

                if (!Directory.Exists(
                        targetDirectory))
                {
                    return HandleFailure(
                        $"Target directory does not exist: {targetDirectory}",
                        14,
                        progressWindow,
                        null,
                        targetDirectory,
                        parentProcessId,
                        tryRestart: false);
                }

                // ======================================
                // START PROGRESS WINDOW
                // ======================================

                progressWindow =
                    new UpdateProgressWindowHost();

                if (!progressWindow.Start())
                {
                    UpdateLogger.Write(
                        "Self update will continue without the progress window.");
                }

                progressWindow.UpdateProgress(
                    "Se verifică pachetul de actualizare...",
                    10);

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
                    return HandleFailure(
                        "Update package SHA-256 verification failed.",
                        18,
                        progressWindow,
                        "Verificarea pachetului de actualizare a eșuat.",
                        targetDirectory,
                        parentProcessId,
                        tryRestart: true);
                }

                Console.WriteLine(
                    "Update package SHA-256 verified.");

                UpdateLogger.Write(
                    "Update package SHA-256 verified successfully.");

                // ======================================
                // VERIFY PACKAGE SIGNATURE
                // ======================================

                if (!string.IsNullOrWhiteSpace(
                        expectedPackageSignature))
                {
                    progressWindow.UpdateProgress(
                        "Se verifică semnătura digitală a actualizării...",
                        15);

                    bool signatureValid =
                        UpdatePackageSignatureVerifier.Verify(
                            packagePath,
                            expectedPackageSignature,
                            UpdateSigningPublicKey.Pem);

                    if (!signatureValid)
                    {
                        return HandleFailure(
                            "Update package signature verification failed.",
                            19,
                            progressWindow,
                            "Semnătura digitală a pachetului de actualizare nu este validă.",
                            targetDirectory,
                            parentProcessId,
                            tryRestart: true);
                    }

                    Console.WriteLine(
                        "Update package signature verified.");

                    UpdateLogger.Write(
                        "Update package signature verified successfully.");
                }
                else
                {
                    UpdateLogger.Write(
                        "Update package signature is not present. Legacy SHA-256-only verification is being used.");
                }

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

                progressWindow.UpdateProgress(
                    "Se așteaptă închiderea WinBoost...",
                    25);

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
                    return HandleFailure(
                        "WinBoost did not close within the safety timeout.",
                        15,
                        progressWindow,
                        "WinBoost nu s-a închis la timp. Actualizarea a fost anulată.",
                        targetDirectory,
                        parentProcessId,
                        tryRestart: false);
                }

                Console.WriteLine(
                    "WinBoost process is closed.");

                UpdateLogger.Write(
                    "WinBoost process closed successfully.");

                // ======================================
                // EXTRACT UPDATE PACKAGE TO STAGING
                // ======================================

                progressWindow.UpdateProgress(
                    "Se extrage pachetul de actualizare...",
                    40);

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

                progressWindow.UpdateProgress(
                    "Se creează copia de siguranță...",
                    55);

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

                progressWindow.UpdateProgress(
                    "Se instalează actualizarea...",
                    75);

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
                // SAVE SUCCESS RESULT
                // ======================================

                progressWindow.UpdateProgress(
                    "Se pregătește repornirea WinBoost...",
                    95);

                UpdateResultStore
                    .SaveSuccess();

                UpdateLogger.Write(
                    "Update result saved as successful.");

                // ======================================
                // RESTART WINBOOST
                // ======================================

                progressWindow.UpdateProgress(
                    "Actualizarea a fost finalizată. Se repornește WinBoost...",
                    100);

                System.Threading.Thread.Sleep(
                    700);

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
                bool rolledBack =
                    WasRollbackSuccessful(
                        ex);

                UpdateResultStore
                    .SaveFailure(
                        ex.Message,
                        rolledBack);

                if (progressWindow != null)
                {
                    if (rolledBack)
                    {
                        progressWindow.UpdateStatus(
                            "Instalarea a eșuat. Versiunea anterioară WinBoost a fost restaurată.");
                    }
                    else
                    {
                        progressWindow.UpdateStatus(
                            "Actualizarea WinBoost nu a putut fi instalată.");
                    }

                    System.Threading.Thread.Sleep(
                        5000);
                }

                // ======================================
                // RESTART AVAILABLE WINBOOST
                // ======================================

                TryRestartWinBoostAfterFailure(
                    recoveryTargetDirectory,
                    recoveryParentProcessId);

                UpdateLogger.WriteException(
                    "Self update failed",
                    ex);

                Console.Error.WriteLine(
                    "WinBoost Self Update Worker failed.");

                Console.Error.WriteLine(
                    ex.Message);

                return 100;
            }
            finally
            {
                progressWindow?.Dispose();
            }
        }

        // ======================================
        // HANDLE EXPECTED FAILURE
        // ======================================

        private static int HandleFailure(
            string message,
            int exitCode,
            UpdateProgressWindowHost? progressWindow,
            string? progressStatus,
            string? targetDirectory,
            int? parentProcessId,
            bool tryRestart)
        {
            UpdateLogger.Write(
                message);

            UpdateResultStore
                .SaveFailure(
                    message,
                    rolledBack: false);

            if (progressWindow != null &&
                !string.IsNullOrWhiteSpace(
                    progressStatus))
            {
                progressWindow.UpdateStatus(
                    progressStatus);

                System.Threading.Thread.Sleep(
                    2000);
            }

            Console.Error.WriteLine(
                message);

            if (tryRestart)
            {
                TryRestartWinBoostAfterFailure(
                    targetDirectory,
                    parentProcessId);
            }

            return exitCode;
        }

        // ======================================
        // RESTART AFTER FAILURE
        // ======================================

        private static void TryRestartWinBoostAfterFailure(
            string? targetDirectory,
            int? parentProcessId)
        {
            if (string.IsNullOrWhiteSpace(
                    targetDirectory))
            {
                UpdateLogger.Write(
                    "WinBoost restart after failure was skipped because the target directory is unavailable.");

                return;
            }

            if (!Directory.Exists(
                    targetDirectory))
            {
                UpdateLogger.Write(
                    $"WinBoost restart after failure was skipped because the target directory does not exist: {targetDirectory}");

                return;
            }

            if (!parentProcessId.HasValue ||
                parentProcessId.Value <= 0)
            {
                UpdateLogger.Write(
                    "WinBoost restart after failure was skipped because the original process ID is unavailable.");

                return;
            }

            try
            {
                UpdateLogger.Write(
                    "Waiting for the original WinBoost process before failure recovery restart.");

                bool parentExited =
                    WaitForParentProcessExit(
                        parentProcessId.Value,
                        TimeSpan.FromSeconds(10));

                if (!parentExited)
                {
                    UpdateLogger.Write(
                        "WinBoost restart after failure was skipped because the original WinBoost process is still running.");

                    return;
                }

                UpdateLogger.Write(
                    "Restarting WinBoost after update failure.");

                UpdateRestartManager
                    .Restart(
                        targetDirectory);

                UpdateLogger.Write(
                    "WinBoost restarted successfully after update failure.");
            }
            catch (Exception restartException)
            {
                UpdateLogger.WriteException(
                    "WinBoost could not be restarted after update failure",
                    restartException);
            }
        }

        // ======================================
        // ROLLBACK RESULT
        // ======================================

        private static bool WasRollbackSuccessful(
            Exception exception)
        {
            return exception
                       is UpdateInstallationException
                       updateInstallationException &&
                   updateInstallationException.RolledBack;
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