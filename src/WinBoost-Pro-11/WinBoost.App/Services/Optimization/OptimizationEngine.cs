using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using WinBoost.App.Models;

namespace WinBoost.App.Services.Optimization
{
    public sealed class OptimizationEngine
    {
        private readonly TempFilesCleanerService
            _tempFilesCleanerService;

        private readonly RecycleBinCleanerService
            _recycleBinCleanerService;

        private readonly DnsCacheCleanerService
            _dnsCacheCleanerService;

        private readonly ThumbnailCacheCleanerService
            _thumbnailCacheCleanerService;

        private readonly PrefetchCleanerService
            _prefetchCleanerService;

        private readonly WindowsErrorReportsCleanerService
            _windowsErrorReportsCleanerService;

        private readonly WindowsLogsCleanerService
            _windowsLogsCleanerService;

        public OptimizationEngine()
        {
            _tempFilesCleanerService =
                new TempFilesCleanerService();

            _recycleBinCleanerService =
                new RecycleBinCleanerService();

            _dnsCacheCleanerService =
                new DnsCacheCleanerService();

            _thumbnailCacheCleanerService =
                new ThumbnailCacheCleanerService();

            _prefetchCleanerService =
                new PrefetchCleanerService();

            _windowsErrorReportsCleanerService =
                new WindowsErrorReportsCleanerService();

            _windowsLogsCleanerService =
                new WindowsLogsCleanerService();
        }

        public event EventHandler<OptimizationProgressEventArgs>?
            ProgressChanged;

        /*
         * Metodă păstrată pentru compatibilitate.
         * Curățarea fișierelor temporare este executată implicit.
         */
        public Task<OptimizationReport> RunOptimizationAsync(
            bool emptyRecycleBin = false,
            bool cleanDnsCache = false,
            bool cleanThumbnailCache = false)
        {
            var options =
                new OptimizationOptions
                {
                    CleanTemporaryFiles = true,
                    EmptyRecycleBin = emptyRecycleBin,
                    CleanDnsCache = cleanDnsCache,
                    CleanThumbnailCache =
                        cleanThumbnailCache
                };

            return RunOptimizationAsync(
                options);
        }

        /*
         * Metoda principală a motorului.
         * Execută numai operațiile selectate în options.
         */
        public async Task<OptimizationReport>
            RunOptimizationAsync(
                OptimizationOptions options)
        {
            ArgumentNullException.ThrowIfNull(
                options);

            var stopwatch =
                Stopwatch.StartNew();

            var report =
                new OptimizationReport();

            List<OptimizationOperation>
                operations =
                    CreateOperations(
                        options);

            if (operations.Count == 0)
            {
                stopwatch.Stop();

                report.IsSuccessful = true;
                report.Duration =
                    stopwatch.Elapsed;

                report.Message =
                    "Nu a fost selectată nicio operație.";

                OnProgressChanged(
                    "Nicio operație selectată",
                    100);

                return report;
            }

            try
            {
                for (int index = 0;
                     index < operations.Count;
                     index++)
                {
                    OptimizationOperation operation =
                        operations[index];

                    int startProgress =
                        CalculateProgress(
                            index,
                            operations.Count);

                    OnProgressChanged(
                        operation.StartMessage,
                        startProgress);

                    OptimizationResult result =
                        await operation.ExecuteAsync();

                    AddResult(
                        report,
                        result);

                    int completedProgress =
                        CalculateProgress(
                            index + 1,
                            operations.Count);

                    OnProgressChanged(
                        GetCompletionMessage(
                            result),
                        completedProgress);
                }

                report.IsSuccessful =
                    report.Results.All(
                        result =>
                            result.IsSuccessful ||
                            result.WasSkipped);

                report.Message =
                    report.IsSuccessful
                        ? "Optimizarea a fost finalizată."
                        : "Optimizarea a fost finalizată cu erori.";
            }
            catch (Exception ex)
            {
                report.IsSuccessful = false;

                report.Message =
                    "Optimizarea nu a putut fi finalizată: " +
                    ex.Message;

                OnProgressChanged(
                    "Optimizarea a eșuat",
                    100);
            }
            finally
            {
                stopwatch.Stop();

                report.Duration =
                    stopwatch.Elapsed;
            }

            return report;
        }

        private List<OptimizationOperation>
            CreateOperations(
                OptimizationOptions options)
        {
            var operations =
                new List<OptimizationOperation>();

            if (options.CleanTemporaryFiles)
            {
                operations.Add(
                    new OptimizationOperation(
                        "temp-files",
                        "Curățare fișiere temporare",
                        () =>
                            _tempFilesCleanerService
                                .CleanUserTempAsync()));
            }

            if (options.EmptyRecycleBin)
            {
                operations.Add(
                    new OptimizationOperation(
                        "recycle-bin",
                        "Golire Coș de reciclare",
                        () =>
                            _recycleBinCleanerService
                                .EmptyRecycleBinAsync()));
            }

            if (options.CleanDnsCache)
            {
                operations.Add(
                    new OptimizationOperation(
                        "dns-cache",
                        "Curățare cache DNS",
                        () =>
                            _dnsCacheCleanerService
                                .CleanAsync()));
            }

            if (options.CleanThumbnailCache)
            {
                operations.Add(
                    new OptimizationOperation(
                        "thumbnail-cache",
                        "Curățare cache miniaturi",
                        () =>
                            _thumbnailCacheCleanerService
                                .CleanAsync()));
            }

            if (options.CleanPrefetch)
            {
                operations.Add(
                    new OptimizationOperation(
                        "prefetch",
                        "Curățare Windows Prefetch",
                        () =>
                            _prefetchCleanerService
                                .CleanAsync()));
            }

            if (options.CleanWindowsErrorReports)
            {
                operations.Add(
                    new OptimizationOperation(
                        "windows-error-reports",
                        "Curățare rapoarte de eroare Windows",
                        () =>
                            _windowsErrorReportsCleanerService
                                .CleanAsync()));
            }

            if (options.CleanWindowsLogs)
            {
                operations.Add(
                    new OptimizationOperation(
                        "windows-logs",
                        "Curățare jurnale Windows",
                        () =>
                            _windowsLogsCleanerService
                                .CleanAsync()));
            }

            return operations;
        }

        private static int CalculateProgress(
            int completedOperations,
            int totalOperations)
        {
            if (totalOperations <= 0)
            {
                return 100;
            }

            double progress =
                completedOperations /
                (double)totalOperations *
                100;

            return Math.Clamp(
                (int)Math.Round(progress),
                0,
                100);
        }

        private static string GetCompletionMessage(
            OptimizationResult result)
        {
            if (result.WasSkipped)
            {
                return string.IsNullOrWhiteSpace(
                    result.OperationName)
                        ? "Operație omisă"
                        : $"{result.OperationName}: omis";
            }

            if (!result.IsSuccessful)
            {
                return string.IsNullOrWhiteSpace(
                    result.OperationName)
                        ? "Operație finalizată cu eroare"
                        : $"{result.OperationName}: eroare";
            }

            return string.IsNullOrWhiteSpace(
                result.OperationName)
                    ? "Operație finalizată"
                    : $"{result.OperationName}: finalizat";
        }

        private void OnProgressChanged(
            string operationName,
            int progressPercentage)
        {
            ProgressChanged?.Invoke(
                this,
                new OptimizationProgressEventArgs(
                    operationName,
                    progressPercentage));
        }

        private static void AddResult(
            OptimizationReport report,
            OptimizationResult result)
        {
            report.Results.Add(
                result);

            report.TotalDeletedFiles +=
                result.DeletedFilesCount;

            report.TotalRecoveredBytes +=
                result.RecoveredBytes;
        }

        private sealed class OptimizationOperation
        {
            public OptimizationOperation(
                string operationId,
                string startMessage,
                Func<Task<OptimizationResult>>
                    executeAsync)
            {
                OperationId =
                    operationId;

                StartMessage =
                    startMessage;

                ExecuteAsync =
                    executeAsync;
            }

            public string OperationId
            {
                get;
            }

            public string StartMessage
            {
                get;
            }

            public Func<Task<OptimizationResult>>
                ExecuteAsync
            {
                get;
            }
        }
    }

    public sealed class OptimizationOptions
    {
        public bool CleanTemporaryFiles
        {
            get;
            set;
        } = true;

        public bool EmptyRecycleBin
        {
            get;
            set;
        }

        public bool CleanDnsCache
        {
            get;
            set;
        }

        public bool CleanThumbnailCache
        {
            get;
            set;
        }

        public bool CleanPrefetch
        {
            get;
            set;
        }

        public bool CleanWindowsErrorReports
        {
            get;
            set;
        }

        public bool CleanWindowsLogs
        {
            get;
            set;
        }

        public bool HasSelectedOperations =>
            CleanTemporaryFiles ||
            EmptyRecycleBin ||
            CleanDnsCache ||
            CleanThumbnailCache ||
            CleanPrefetch ||
            CleanWindowsErrorReports ||
            CleanWindowsLogs;
    }

    public sealed class OptimizationReport
    {
        public bool IsSuccessful
        {
            get;
            set;
        }

        public long TotalDeletedFiles
        {
            get;
            set;
        }

        public long TotalRecoveredBytes
        {
            get;
            set;
        }

        public TimeSpan Duration
        {
            get;
            set;
        }

        public string Message
        {
            get;
            set;
        } =
            string.Empty;

        public List<OptimizationResult> Results
        {
            get;
        } =
            new();

        public int SuccessfulOperations =>
            Results.Count(
                result =>
                    result.IsSuccessful &&
                    !result.WasSkipped);

        public int FailedOperations =>
            Results.Count(
                result =>
                    !result.IsSuccessful &&
                    !result.WasSkipped);

        public int SkippedOperations =>
            Results.Count(
                result =>
                    result.WasSkipped);

        public string RecoveredSpaceText =>
            FormatBytes(
                TotalRecoveredBytes);

        public string DurationText =>
            $"{Duration.TotalSeconds:F1} sec";

        private static string FormatBytes(
            long bytes)
        {
            string[] units =
            {
                "B",
                "KB",
                "MB",
                "GB",
                "TB"
            };

            double value =
                Math.Max(
                    0,
                    bytes);

            int unitIndex =
                0;

            while (value >= 1024 &&
                   unitIndex < units.Length - 1)
            {
                value /=
                    1024;

                unitIndex++;
            }

            return
                $"{value:F2} {units[unitIndex]}";
        }
    }

    public sealed class OptimizationProgressEventArgs :
        EventArgs
    {
        public OptimizationProgressEventArgs(
            string operationName,
            int progressPercentage)
        {
            OperationName =
                operationName;

            ProgressPercentage =
                Math.Clamp(
                    progressPercentage,
                    0,
                    100);
        }

        public string OperationName
        {
            get;
        }

        public int ProgressPercentage
        {
            get;
        }
    }
}