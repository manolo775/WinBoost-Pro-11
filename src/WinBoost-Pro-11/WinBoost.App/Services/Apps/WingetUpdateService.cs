using System;
using System.Diagnostics;
using System.Threading.Tasks;
using WinBoost.App.Models;

namespace WinBoost.App.Services.Apps
{
    public sealed class WingetUpdateService
    {
        public async Task<WingetAvailabilityResult>
            CheckAvailabilityAsync()
        {
            WingetCommandResult result =
                await RunWingetAsync(
                    "--version");

            return new WingetAvailabilityResult(
                result.ExitCode == 0 &&
                !string.IsNullOrWhiteSpace(
                    result.Output),
                result.Output.Trim());
        }

        public async Task<AppUpdateCheckResult>
            CheckForUpdateAsync(
                string displayName)
        {
            if (string.IsNullOrWhiteSpace(
                    displayName))
            {
                return new AppUpdateCheckResult
                {
                    Status =
                        AppUpdateStatus.Unavailable,

                    Details =
                        "Application name is unavailable."
                };
            }

            WingetCommandResult installedResult =
                await RunWingetAsync(
                    "list",
                    "--name",
                    displayName,
                    "--exact",
                    "--disable-interactivity",
                    "--accept-source-agreements");

            if (installedResult.ExitCode != 0 ||
                !ContainsApplication(
                    installedResult.Output,
                    displayName))
            {
                return new AppUpdateCheckResult
                {
                    Status =
                        AppUpdateStatus.Unavailable,

                    Details =
                        "The application could not be associated with winget."
                };
            }

            WingetCommandResult upgradesResult =
                await RunWingetAsync(
                    "list",
                    "--upgrade-available",
                    "--disable-interactivity",
                    "--accept-source-agreements");

            if (upgradesResult.ExitCode != 0)
            {
                return new AppUpdateCheckResult
                {
                    Status =
                        AppUpdateStatus.Failed,

                    Details =
                        GetErrorText(
                            upgradesResult)
                };
            }

            bool updateAvailable =
                ContainsApplication(
                    upgradesResult.Output,
                    displayName);

            return new AppUpdateCheckResult
            {
                Status =
                    updateAvailable
                        ? AppUpdateStatus
                            .UpdateAvailable
                        : AppUpdateStatus
                            .UpToDate,

                Details =
                    updateAvailable
                        ? "An update is available through winget."
                        : "No update was found through winget."
            };
        }

        public async Task<AppUpdateCheckResult>
            UpdateAsync(
                string displayName)
        {
            if (string.IsNullOrWhiteSpace(
                    displayName))
            {
                return new AppUpdateCheckResult
                {
                    Status =
                        AppUpdateStatus.Unavailable,

                    Details =
                        "Application name is unavailable."
                };
            }

            WingetCommandResult result =
                await RunWingetAsync(
                    "upgrade",
                    "--name",
                    displayName,
                    "--exact",
                    "--accept-package-agreements",
                    "--accept-source-agreements",
                    "--disable-interactivity");

            if (result.ExitCode != 0)
            {
                return new AppUpdateCheckResult
                {
                    Status =
                        AppUpdateStatus.Failed,

                    Details =
                        GetErrorText(result)
                };
            }

            return new AppUpdateCheckResult
            {
                Status =
                    AppUpdateStatus.Updated,

                Details =
                    "The update was installed successfully."
            };
        }

        private static bool ContainsApplication(
            string output,
            string displayName)
        {
            return !string.IsNullOrWhiteSpace(
                       output) &&
                   output.Contains(
                       displayName,
                       StringComparison
                           .OrdinalIgnoreCase);
        }

        private static string GetErrorText(
            WingetCommandResult result)
        {
            if (!string.IsNullOrWhiteSpace(
                    result.Error))
            {
                return result.Error.Trim();
            }

            return "winget could not complete the operation.";
        }

        private static async Task<
            WingetCommandResult>
            RunWingetAsync(
                params string[] arguments)
        {
            try
            {
                using var process =
                    new Process();

                process.StartInfo =
                    new ProcessStartInfo
                    {
                        FileName = "winget",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                foreach (string argument
                         in arguments)
                {
                    process.StartInfo.ArgumentList.Add(
                        argument);
                }

                if (!process.Start())
                {
                    return new WingetCommandResult(
                        -1,
                        string.Empty,
                        "winget could not be started.");
                }

                Task<string> outputTask =
                    process.StandardOutput
                        .ReadToEndAsync();

                Task<string> errorTask =
                    process.StandardError
                        .ReadToEndAsync();

                Task exitTask =
                    process.WaitForExitAsync();

                Task completedTask =
                    await Task.WhenAny(
                        exitTask,
                        Task.Delay(
                            TimeSpan.FromMinutes(3)));

                if (completedTask != exitTask)
                {
                    try
                    {
                        process.Kill(true);
                    }
                    catch
                    {
                    }

                    return new WingetCommandResult(
                        -1,
                        string.Empty,
                        "winget timed out.");
                }

                string output =
                    await outputTask;

                string error =
                    await errorTask;

                return new WingetCommandResult(
                    process.ExitCode,
                    output,
                    error);
            }
            catch (Exception exception)
            {
                return new WingetCommandResult(
                    -1,
                    string.Empty,
                    exception.Message);
            }
        }

        private sealed class WingetCommandResult
        {
            public WingetCommandResult(
                int exitCode,
                string output,
                string error)
            {
                ExitCode = exitCode;
                Output = output;
                Error = error;
            }

            public int ExitCode { get; }

            public string Output { get; }

            public string Error { get; }
        }
    }

    public sealed class WingetAvailabilityResult
    {
        public WingetAvailabilityResult(
            bool isAvailable,
            string version)
        {
            IsAvailable = isAvailable;
            Version = version;
        }

        public bool IsAvailable { get; }

        public string Version { get; }
    }
}