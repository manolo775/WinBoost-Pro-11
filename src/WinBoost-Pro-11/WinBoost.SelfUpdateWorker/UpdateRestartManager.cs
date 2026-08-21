using System;
using System.Diagnostics;
using System.IO;

namespace WinBoost.SelfUpdateWorker
{
    internal static class UpdateRestartManager
    {
        private const string ApplicationFileName =
            "WinBoost.App.exe";

        public static void Restart(
            string targetDirectory)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(
                targetDirectory);

            string normalizedTargetDirectory =
                Path.GetFullPath(
                    targetDirectory);

            string applicationPath =
                Path.Combine(
                    normalizedTargetDirectory,
                    ApplicationFileName);

            if (!File.Exists(
                    applicationPath))
            {
                throw new FileNotFoundException(
                    "WinBoost.App.exe was not found after the update.",
                    applicationPath);
            }

            var startInfo =
                new ProcessStartInfo
                {
                    FileName =
                        applicationPath,

                    WorkingDirectory =
                        normalizedTargetDirectory,

                    UseShellExecute =
                        true
                };

            Process? process =
                Process.Start(
                    startInfo);

            if (process == null)
            {
                throw new InvalidOperationException(
                    "WinBoost could not be restarted.");
            }

            process.Dispose();
        }
    }
}