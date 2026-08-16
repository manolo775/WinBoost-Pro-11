using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows;
using System.ComponentModel;
using System.IO;
using WinBoost.App.Services.Navigation;

namespace WinBoost.App.Helpers
{
    public static class ApplicationElevationHelper
    {
        public static bool IsRunningAsAdministrator()
        {
            using WindowsIdentity identity =
                WindowsIdentity.GetCurrent();

            WindowsPrincipal principal =
                new WindowsPrincipal(identity);

            return principal.IsInRole(
                WindowsBuiltInRole.Administrator);
        }

        public static bool RestartAsAdministrator()
        {
            try
            {
                string? executablePath =
                    Environment.ProcessPath;

                if (string.IsNullOrWhiteSpace(
                        executablePath))
                {
                    return false;
                }

                string workingDirectory =
                    Path.GetDirectoryName(
                        executablePath) ??
                    AppContext.BaseDirectory;

                string? returnPage =
                    AppNavigationService
                        .ReturnPageAfterPrivilegeRestart;

                string arguments =
                    string.IsNullOrWhiteSpace(
                        returnPage)
                        ? string.Empty
                        : $"--return-page \"{returnPage}\"";

                var startInfo =
                    new ProcessStartInfo
                    {
                        FileName =
                            executablePath,

                        WorkingDirectory =
                            workingDirectory,

                        Arguments =
                            arguments,

                        UseShellExecute =
                            true,

                        Verb =
                            "runas"
                    };

                Process.Start(
                    startInfo);

                Application.Current.Shutdown();

                return true;
            }
            catch (Win32Exception exception)
                when (exception.NativeErrorCode == 1223)
            {
                return false;
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    exception.Message,
                    "WinBoost Pro 11",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return false;
            }
        }

        public static bool RestartNormally()
        {
            try
            {
                string? executablePath =
                    Environment.ProcessPath;

                if (string.IsNullOrWhiteSpace(
                        executablePath))
                {
                    return false;
                }

                var startInfo =
                    new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments =
                            $"\"{executablePath}\"",
                        UseShellExecute = true
                    };

                Process.Start(
                    startInfo);

                Application.Current.Shutdown();

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}