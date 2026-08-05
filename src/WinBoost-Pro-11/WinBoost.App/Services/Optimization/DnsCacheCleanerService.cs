using System;
using System.Diagnostics;
using System.Threading.Tasks;
using WinBoost.App.Models;

namespace WinBoost.App.Services.Optimization
{
    public sealed class DnsCacheCleanerService
    {
        public Task<OptimizationResult> CleanAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    using Process process =
                        new Process
                        {
                            StartInfo =
                                new ProcessStartInfo
                                {
                                    FileName = "ipconfig.exe",
                                    Arguments = "/flushdns",
                                    UseShellExecute = false,
                                    CreateNoWindow = true,
                                    RedirectStandardOutput = true,
                                    RedirectStandardError = true
                                }
                        };

                    process.Start();

                    string output =
                        process.StandardOutput.ReadToEnd();

                    string error =
                        process.StandardError.ReadToEnd();

                    process.WaitForExit();

                    bool isSuccessful =
                        process.ExitCode == 0;

                    return new OptimizationResult
                    {
                        OperationId = "dns-cache",
                        OperationName = "DNS Cache",
                        RequiresAdministrator = false,
                        IsSuccessful = isSuccessful,
                        DeletedFilesCount = 0,
                        RecoveredBytes = 0,
                        Message =
                            isSuccessful
                                ? "Cache-ul DNS a fost curățat."
                                : "Cache-ul DNS nu a putut fi curățat: " +
                                  (
                                      string.IsNullOrWhiteSpace(error)
                                          ? output
                                          : error
                                  )
                    };
                }
                catch (Exception ex)
                {
                    return new OptimizationResult
                    {
                        OperationId = "dns-cache",
                        OperationName = "DNS Cache",
                        RequiresAdministrator = false,
                        IsSuccessful = false,
                        Message =
                            "Cache-ul DNS nu a putut fi curățat: " +
                            ex.Message
                    };
                }
            });
        }
    }
}