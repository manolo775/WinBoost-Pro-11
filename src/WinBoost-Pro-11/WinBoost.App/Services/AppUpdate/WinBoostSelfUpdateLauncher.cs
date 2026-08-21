using System;
using System.Diagnostics;
using System.IO;

namespace WinBoost.App.Services.AppUpdate
{
    public sealed class WinBoostSelfUpdateLauncher
    {
        private const string WorkerFilePrefix =
            "WinBoost.SelfUpdateWorker.";

        public bool TryStart(
            string packagePath,
            string expectedSha256,
            out string errorMessage)
        {
            errorMessage =
                string.Empty;

            try
            {
                if (string.IsNullOrWhiteSpace(
                        packagePath) ||
                    !File.Exists(
                        packagePath))
                {
                    errorMessage =
                        "The update package does not exist.";

                    return false;
                }

                if (string.IsNullOrWhiteSpace(
                        expectedSha256))
                {
                    errorMessage =
                        "The update SHA-256 is unavailable.";

                    return false;
                }

                string applicationDirectory =
                    AppContext.BaseDirectory;

                string sourceWorkerPath =
                    Path.Combine(
                        applicationDirectory,
                        "WinBoost.SelfUpdateWorker.exe");

                if (!File.Exists(
                        sourceWorkerPath))
                {
                    errorMessage =
                        "WinBoost Self Update Worker was not found.";

                    return false;
                }

                string temporaryWorkerDirectory =
                    CreateTemporaryWorkerDirectory(
                        applicationDirectory);

                string temporaryWorkerPath =
                    Path.Combine(
                        temporaryWorkerDirectory,
                        "WinBoost.SelfUpdateWorker.exe");

                if (!File.Exists(
                        temporaryWorkerPath))
                {
                    errorMessage =
                        "The temporary self update worker was not created.";

                    return false;
                }

                var startInfo =
                    new ProcessStartInfo
                    {
                        FileName =
                            temporaryWorkerPath,

                        WorkingDirectory =
                            temporaryWorkerDirectory,

                        UseShellExecute =
                            false,

                        CreateNoWindow =
                            true
                    };

                startInfo.ArgumentList.Add(
                    $"--package={Path.GetFullPath(packagePath)}");

                startInfo.ArgumentList.Add(
                    $"--target-dir={Path.GetFullPath(applicationDirectory)}");

                startInfo.ArgumentList.Add(
                    $"--parent-pid={Environment.ProcessId}");

                startInfo.ArgumentList.Add(
                    $"--sha256={expectedSha256}");

                Process? process =
                    Process.Start(
                        startInfo);

                if (process == null)
                {
                    errorMessage =
                        "The self update worker could not be started.";

                    return false;
                }

                process.Dispose();

                return true;
            }
            catch (Exception ex)
            {
                errorMessage =
                    ex.Message;

                return false;
            }
        }

        private static string
            CreateTemporaryWorkerDirectory(
                string applicationDirectory)
        {
            string temporaryRoot =
                Path.Combine(
                    Path.GetTempPath(),
                    "WinBoost",
                    "SelfUpdateWorker");

            Directory.CreateDirectory(
                temporaryRoot);

            string temporaryDirectory =
                Path.Combine(
                    temporaryRoot,
                    Guid.NewGuid()
                        .ToString("N"));

            Directory.CreateDirectory(
                temporaryDirectory);

            string[] workerFiles =
                Directory.GetFiles(
                    applicationDirectory,
                    $"{WorkerFilePrefix}*",
                    SearchOption.TopDirectoryOnly);

            if (workerFiles.Length == 0)
            {
                throw new FileNotFoundException(
                    "WinBoost Self Update Worker files were not found.");
            }

            foreach (string sourceFile
                in workerFiles)
            {
                string fileName =
                    Path.GetFileName(
                        sourceFile);

                string destinationFile =
                    Path.Combine(
                        temporaryDirectory,
                        fileName);

                File.Copy(
                    sourceFile,
                    destinationFile,
                    overwrite: true);
            }

            return temporaryDirectory;
        }
    }
}