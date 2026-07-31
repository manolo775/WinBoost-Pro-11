using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using WinBoost.App.Models;

namespace WinBoost.App.Services.Optimization
{
    public class OptimizationEngine
    {
        private readonly TempFilesCleanerService
            _tempFilesCleanerService;

        public OptimizationEngine()
        {
            _tempFilesCleanerService =
                new TempFilesCleanerService();
        }

        public async Task<OptimizationReport>
            RunOptimizationAsync()
        {
            var stopwatch = Stopwatch.StartNew();

            var report =
                new OptimizationReport();

            try
            {
                OptimizationResult tempFilesResult =
                    await _tempFilesCleanerService
                        .CleanUserTempAsync();

                report.Results.Add(tempFilesResult);

                report.TotalDeletedFiles +=
                    tempFilesResult.DeletedFilesCount;

                report.TotalRecoveredBytes +=
                    tempFilesResult.RecoveredBytes;

                report.IsSuccessful =
                    tempFilesResult.IsSuccessful;

                report.Message =
                    tempFilesResult.IsSuccessful
                        ? "Optimizarea a fost finalizată."
                        : "Optimizarea a fost finalizată cu erori.";
            }
            catch (Exception ex)
            {
                report.IsSuccessful = false;

                report.Message =
                    $"Optimizarea nu a putut fi finalizată: " +
                    $"{ex.Message}";
            }
            finally
            {
                stopwatch.Stop();

                report.Duration =
                    stopwatch.Elapsed;
            }

            return report;
        }
    }

    public class OptimizationReport
    {
        public bool IsSuccessful { get; set; }

        public long TotalDeletedFiles { get; set; }

        public long TotalRecoveredBytes { get; set; }

        public TimeSpan Duration { get; set; }

        public string Message { get; set; } =
            string.Empty;

        public List<OptimizationResult> Results
        {
            get;
        } =
            new List<OptimizationResult>();

        public string RecoveredSpaceText =>
            FormatBytes(TotalRecoveredBytes);

        public string DurationText =>
            $"{Duration.TotalSeconds:F1} sec";

        private static string FormatBytes(long bytes)
        {
            string[] units =
            {
                "B",
                "KB",
                "MB",
                "GB",
                "TB"
            };

            double value = bytes;
            int unitIndex = 0;

            while (value >= 1024 &&
                   unitIndex < units.Length - 1)
            {
                value /= 1024;
                unitIndex++;
            }

            return $"{value:F2} {units[unitIndex]}";
        }
    }
}