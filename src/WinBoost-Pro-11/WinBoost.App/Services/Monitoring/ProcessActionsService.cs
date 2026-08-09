using System;
using System.Diagnostics;
using System.IO;

namespace WinBoost.App.Services.Monitoring
{
    public sealed class ProcessActionsService
    {
        public bool OpenExecutableLocation(
            string executablePath)
        {
            if (string.IsNullOrWhiteSpace(
                    executablePath) ||
                !File.Exists(executablePath))
            {
                return false;
            }

            try
            {
                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments =
                            $"/select,\"{executablePath}\"",
                        UseShellExecute = true
                    });

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}