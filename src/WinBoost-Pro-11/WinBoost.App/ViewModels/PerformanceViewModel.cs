using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using WinBoost.App.Commands;
using WinBoost.App.Localization;
using WinBoost.App.Models;
using WinBoost.App.Services.Monitoring;
using WinBoost.App.Services.Optimization;
using MaterialDesignThemes.Wpf;
using System.Windows.Media;
using WinBoost.App.Services.Startup;
using WinBoost.App.Services.Health;

namespace WinBoost.App.ViewModels
{
    public class PerformanceViewModel : DashboardViewModel
    {
        private readonly ProcessMonitorService _processMonitorService;
        private readonly TempFileService _tempFileService;
        private readonly DispatcherTimer _processRefreshTimer;
        private readonly StartupAppsScanner
                         _startupAppsScanner;
        private readonly OptimizationCoordinator
                          _optimizationCoordinator;

        private bool _isRefreshingProcesses;
        private bool _isAnalyzing;
        private bool _isApplyingOptimizations;

        private string _optimizationStatus =
            LocalizationHelper.Get("PerformanceAnalyzePrompt");
        private int _performanceAnalyzerScore = 100;

        private int _optimizationProgress;

        private string _currentOptimizationOperation =
            string.Empty;

        private readonly OptimizationSummaryViewModel
                    _optimizationSummaryViewModel;
        private readonly OptimizationHistoryViewModel
                    _optimizationHistoryViewModel;

        private readonly OptimizationLogViewModel
            _optimizationLogViewModel;

        public int PerformanceAnalyzerScore
        {
            get => _performanceAnalyzerScore;

            private set
            {
                int normalizedValue =
                    Math.Clamp(
                        value,
                        0,
                        100);

                if (_performanceAnalyzerScore ==
                    normalizedValue)
                {
                    return;
                }

                _performanceAnalyzerScore =
                    normalizedValue;

                WinBoostHealthScoreService
                          .Instance
                          .PerformanceScore =
                           normalizedValue;

                OnPropertyChanged();

                OnPropertyChanged(
                       nameof(PerformanceAnalyzerStatus));
            }
        }

        public string PerformanceAnalyzerStatus =>
    PerformanceAnalyzerScore switch
    {
        >= 90 =>
            LocalizationHelper.Get(
                "PerformanceScoreExcellent"),

        >= 75 =>
            LocalizationHelper.Get(
                "PerformanceScoreGood"),

        >= 50 =>
            LocalizationHelper.Get(
                "PerformanceScoreAttention"),

        _ =>
            LocalizationHelper.Get(
                "PerformanceScoreCritical")
    };
        public ObservableCollection<ProcessInfo> TopProcesses { get; }

        public ObservableCollection<OptimizationRecommendation>
            Recommendations
        { get; }

        public ObservableCollection<RecommendationItem>
    RecommendationItems
        { get; }

        public ICommand OptimizeCommand { get; }

        public ICommand ApplyOptimizationsCommand { get; }

        public string OptimizationStatus
        {
            get => _optimizationStatus;

            private set
            {
                if (_optimizationStatus == value)
                    return;

                _optimizationStatus = value;
                OnPropertyChanged();
            }
        }

        public bool IsAnalyzing
        {
            get => _isAnalyzing;

            private set
            {
                if (_isAnalyzing == value)
                    return;

                _isAnalyzing = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(OptimizationButtonText));

                CommandManager.InvalidateRequerySuggested();
            }
        }

        public bool IsApplyingOptimizations
        {
            get => _isApplyingOptimizations;

            private set
            {
                if (_isApplyingOptimizations == value)
                    return;

                _isApplyingOptimizations = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(ApplyButtonText));

                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string OptimizationButtonText =>
            IsAnalyzing
                ? LocalizationHelper.Get("PerformanceAnalyzing")
                : LocalizationHelper.Get("PerformanceAnalyze");

        public string ApplyButtonText =>
            IsApplyingOptimizations
                ? LocalizationHelper.Get("PerformanceApplying")
                : LocalizationHelper.Get("PerformanceApplySelected");

        public int OptimizationProgress
        {
            get => _optimizationProgress;

            private set
            {
                int normalizedValue =
                    Math.Clamp(
                        value,
                        0,
                        100);

                if (_optimizationProgress ==
                    normalizedValue)
                {
                    return;
                }

                _optimizationProgress =
                    normalizedValue;

                OnPropertyChanged();
            }
        }

        public string CurrentOptimizationOperation
        {
            get => _currentOptimizationOperation;

            private set
            {
                if (_currentOptimizationOperation ==
                    value)
                {
                    return;
                }

                _currentOptimizationOperation =
                    value;

                OnPropertyChanged();
            }
        }

        public PerformanceViewModel()
        {
            _processMonitorService =
                new ProcessMonitorService();

            _tempFileService =
                new TempFileService();

            _startupAppsScanner =
                 new StartupAppsScanner();

            _optimizationCoordinator =
                 new OptimizationCoordinator();

            _optimizationSummaryViewModel =
                 new OptimizationSummaryViewModel();

            _optimizationHistoryViewModel =
                 new OptimizationHistoryViewModel();

            _optimizationLogViewModel =
                       new OptimizationLogViewModel();

            _optimizationCoordinator
                    .Engine
                    .ProgressChanged +=
                     OptimizationEngine_ProgressChanged;

            TopProcesses =
                new ObservableCollection<ProcessInfo>();

            Recommendations =
                new ObservableCollection<OptimizationRecommendation>();

            RecommendationItems =
                new ObservableCollection<RecommendationItem>();



            OptimizeCommand =
                new RelayCommand(
                    async _ => await AnalyzeSystemAsync(),
                    _ => !IsAnalyzing &&
                         !IsApplyingOptimizations);


            ApplyOptimizationsCommand =
            new RelayCommand(
                async _ =>
                    await ApplyOptimizationsAsync(),
                _ =>
                    !IsAnalyzing &&
                    !IsApplyingOptimizations &&
                    Recommendations.Any(
                        item =>
                            item.IsActionable &&
                            item.IsSelected &&
                            !string.IsNullOrWhiteSpace(
                                item.ActionId)));

            _processRefreshTimer =
                new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(5)
                };

            _processRefreshTimer.Tick +=
                ProcessRefreshTimer_Tick;

            LanguageManager.Instance.LanguageChanged +=
                   LanguageManager_LanguageChanged;

            StartPerformanceMonitoring();
        }

        public OptimizationSummaryViewModel
              OptimizationSummaryViewModel =>
              _optimizationSummaryViewModel;

        public OptimizationHistoryViewModel
              OptimizationHistoryViewModel =>
              _optimizationHistoryViewModel;

        public OptimizationLogViewModel
                  OptimizationLogViewModel =>
                  _optimizationLogViewModel;

        public void StartPerformanceMonitoring()
        {
            StartMonitoring();

            if (!_processRefreshTimer.IsEnabled)
            {
                _processRefreshTimer.Start();
            }

            if (TopProcesses.Count == 0)
            {
                _ = RefreshTopProcessesAsync();
            }
        }

        public void StopPerformanceMonitoring()
        {
            StopMonitoring();

            if (_processRefreshTimer.IsEnabled)
            {
                _processRefreshTimer.Stop();
            }
        }

        private async void ProcessRefreshTimer_Tick(
            object? sender,
            EventArgs e)
        {
            await RefreshTopProcessesAsync();
        }

        private async Task RefreshTopProcessesAsync()
        {
            if (_isRefreshingProcesses)
                return;

            try
            {
                _isRefreshingProcesses = true;

                var processes =
                    await _processMonitorService
                        .GetTopProcessesAsync(5);

                TopProcesses.Clear();

                foreach (ProcessInfo process in processes)
                {
                    TopProcesses.Add(process);
                }
            }
            catch
            {
                // Procesele inaccesibile sunt ignorate.
            }
            finally
            {
                _isRefreshingProcesses = false;
            }
        }

        private async Task AnalyzeSystemAsync()
        {
            if (IsAnalyzing ||
                IsApplyingOptimizations)
            {
                return;
            }

            IsAnalyzing = true;

            OptimizationStatus =
                LocalizationHelper.Get(
                    "PerformanceStatusAnalyzing");

            Recommendations.Clear();
            RecommendationItems.Clear();

            try
            {
                var messages =
                    new List<string>();

                // FIȘIERE TEMPORARE

                var tempResult =
                    await _tempFileService
                        .AnalyzeAsync();

                if (tempResult.FileCount > 0)
                {
                    string space =
                        FormatBytes(
                            tempResult.TotalBytes);

                    Recommendations.Add(
                        new OptimizationRecommendation
                        {
                            Id = "temp-cleanup",
                            ActionId = "temp-cleanup",

                            Title =
                                LocalizationHelper.Get(
                                    "PerformanceTempCleanupTitle"),

                            Description =
                                LocalizationHelper.Format(
                                    "PerformanceTempCleanupDescription",
                                    tempResult.FileCount,
                                    space),

                            RequiresAdministrator = false,
                            IsActionable = true,
                            Impact = "Low",
                            IsSelected = true
                        });

                    string cleanupMessage =
                        LocalizationHelper.Format(
                            "PerformanceTempCleanupMessage",
                            space);

                    RecommendationItems.Add(
                        new RecommendationItem
                        {
                            Icon =
                                PackIconKind.CheckCircle,

                            IconBrush =
                                Brushes.LimeGreen,

                            Text =
                                cleanupMessage
                        });

                    messages.Add(
                        cleanupMessage);
                }

                // APLICAȚII STARTUP

                var startupApplications =
                    await _startupAppsScanner
                        .ScanAsync();

                int enabledStartupApps =
                    startupApplications.Count(
                        application =>
                            application.IsEnabled);

                if (enabledStartupApps >= 5)
                {
                    string startupMessage =
                        LocalizationHelper.Format(
                            "PerformanceStartupAppsMessage",
                            enabledStartupApps);

                    RecommendationItems.Add(
                        new RecommendationItem
                        {
                            Icon =
                                PackIconKind.RocketLaunch,

                            IconBrush =
                                Brushes.Gold,

                            Text =
                                startupMessage
                        });

                    messages.Add(
                        startupMessage);
                }

                // UTILIZARE GENERALĂ CPU

                if (CpuUsageValue >= 80)
                {
                    string cpuMessage =
                        LocalizationHelper.Get(
                            "PerformanceCpuHighMessage");

                    messages.Add(
                        cpuMessage);

                    RecommendationItems.Add(
                        new RecommendationItem
                        {
                            Icon =
                                PackIconKind.AlertCircle,

                            IconBrush =
                                Brushes.Orange,

                            Text =
                                cpuMessage
                        });
                }

                // UTILIZARE RAM

                if (RamUsageValue >= 85)
                {
                    string ramMessage =
                        LocalizationHelper.Get(
                            "PerformanceRamHighMessage");

                    messages.Add(
                        ramMessage);

                    RecommendationItems.Add(
                        new RecommendationItem
                        {
                            Icon =
                                PackIconKind.Alert,

                            IconBrush =
                                Brushes.Gold,

                            Text =
                                ramMessage
                        });
                }

                // PROCES CU UTILIZARE CPU RIDICATĂ

                await RefreshTopProcessesAsync();

                ProcessInfo? highestCpuProcess =
                    TopProcesses
                        .OrderByDescending(
                            process =>
                                process.CpuUsage)
                        .FirstOrDefault();

                if (highestCpuProcess != null &&
                    highestCpuProcess.CpuUsage >= 20)
                {
                    string processMessage =
                        LocalizationHelper.Format(
                            "PerformanceHighCpuProcessMessage",
                            highestCpuProcess.Name,
                            highestCpuProcess.CpuUsage
                                .ToString("F1"));

                    messages.Add(
                        processMessage);

                    RecommendationItems.Add(
                        new RecommendationItem
                        {
                            Icon =
                                PackIconKind.AlertCircle,

                            IconBrush =
                                Brushes.Orange,

                            Text =
                                processMessage
                        });
                }

                // UTILIZARE DISC

                if (DiskUsageValue >= 90)
                {
                    string diskMessage =
                        LocalizationHelper.Get(
                            "PerformanceDiskLowMessage");

                    messages.Add(
                        diskMessage);

                    RecommendationItems.Add(
                        new RecommendationItem
                        {
                            Icon =
                                PackIconKind.Harddisk,

                            IconBrush =
                                Brushes.OrangeRed,

                            Text =
                                diskMessage
                        });
                }

                OptimizationStatus =
                    messages.Count == 0
                        ? LocalizationHelper.Get(
                            "PerformanceAnalysisNormal")
                        : LocalizationHelper.Format(
                            "PerformanceRecommendationsPrefix",
                            string.Join(
                                " • ",
                                messages));

                int score = 100;

                if (CpuUsageValue >= 80)
                    score -= 10;

                if (RamUsageValue >= 85)
                    score -= 15;

                if (enabledStartupApps >= 5)
                    score -= 10;

                if (tempResult.FileCount > 0)
                    score -= 5;

                PerformanceAnalyzerScore = score;

                CommandManager
                    .InvalidateRequerySuggested();
            }
            catch (Exception ex)
            {
                OptimizationStatus =
                    LocalizationHelper.Format(
                        "PerformanceAnalysisFailed",
                        ex.Message);
            }
            finally
            {
                IsAnalyzing = false;
            }
        }

        private async Task ApplyOptimizationsAsync()
        {
            if (IsAnalyzing ||
                IsApplyingOptimizations)
            {
                return;
            }

            var selectedItems =
                Recommendations
                    .Where(
                        item =>
                            item.IsActionable &&
                            item.IsSelected &&
                            !string.IsNullOrWhiteSpace(
                                item.ActionId))
                    .ToList();

            if (selectedItems.Count == 0)
            {
                OptimizationStatus =
                    LocalizationHelper.Get(
                        "PerformanceSelectRecommendation");

                return;
            }

            MessageBoxResult confirmation =
                MessageBox.Show(
                    LocalizationHelper.Get(
                        "PerformanceOptimizationConfirmMessage"),
                    LocalizationHelper.Get(
                        "PerformanceOptimizationConfirmTitle"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

            if (confirmation !=
                MessageBoxResult.Yes)
            {
                return;
            }

            IsApplyingOptimizations = true;

            _optimizationSummaryViewModel
              .Clear();

            OptimizationStatus =
                LocalizationHelper.Get(
                    "PerformanceApplyingOptimizations");

            try
            {
                var options =
                    new OptimizationOptions
                    {
                        CleanTemporaryFiles =
                            selectedItems.Any(
                                item =>
                                    item.ActionId ==
                                    "temp-cleanup"),

                        EmptyRecycleBin =
                            selectedItems.Any(
                                item =>
                                    item.ActionId ==
                                    "recycle-bin"),

                        CleanDnsCache =
                            selectedItems.Any(
                                item =>
                                    item.ActionId ==
                                    "dns-cache"),

                        CleanThumbnailCache =
                            selectedItems.Any(
                                item =>
                                    item.ActionId ==
                                    "thumbnail-cache"),

                        CleanPrefetch =
                            selectedItems.Any(
                                item =>
                                    item.ActionId ==
                                    "prefetch"),

                        CleanWindowsErrorReports =
                            selectedItems.Any(
                                item =>
                                    item.ActionId ==
                                    "windows-error-reports"),

                        CleanWindowsLogs =
                            selectedItems.Any(
                                item =>
                                    item.ActionId ==
                                    "windows-logs")
                    };

                OptimizationReport report =
                    await _optimizationCoordinator
                        .OptimizeAsync(
                            options);

                OptimizationHistoryService
                    .Instance
                    .Add(
                     report);

                _optimizationSummaryViewModel
                    .Update(
                        report);

                Recommendations.Clear();
                RecommendationItems.Clear();

                CommandManager
                    .InvalidateRequerySuggested();

                OptimizationStatus =
                    report.IsSuccessful
                        ? LocalizationHelper.Format(
                            "PerformanceCleanupCompleted",
                            report.TotalDeletedFiles,
                            report.RecoveredSpaceText)
                        : report.Message;
            }
            catch (Exception ex)
            {
                OptimizationStatus =
                    LocalizationHelper.Format(
                        "PerformanceOptimizationFailed",
                        ex.Message);
            }
            finally
            {
                IsApplyingOptimizations = false;
            }
        }

        private async void LanguageManager_LanguageChanged(
          object? sender,
          EventArgs e)
        {
            OnPropertyChanged(
                nameof(OptimizationButtonText));

            OnPropertyChanged(
                nameof(ApplyButtonText));

            if (IsAnalyzing ||
                IsApplyingOptimizations)
            {
                return;
            }

            if (Recommendations.Count > 0)
            {
                await AnalyzeSystemAsync();
                return;
            }

            OptimizationStatus =
                LocalizationHelper.Get(
                    "PerformanceAnalyzePrompt");
        }

        private void OptimizationEngine_ProgressChanged(
            object? sender,
            OptimizationProgressEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                OptimizationProgress =
                    e.ProgressPercentage;

                CurrentOptimizationOperation =
                    e.OperationName;

                OptimizationStatus =
                    $"{e.ProgressPercentage}% - {e.OperationName}";
            });
        }
        private static string FormatBytes(long bytes)
        {
            const double megabyte =
                1024d * 1024d;

            const double gigabyte =
                1024d * 1024d * 1024d;

            if (bytes >= gigabyte)
            {
                return $"{bytes / gigabyte:F2} GB";
            }

            return $"{bytes / megabyte:F1} MB";
        }
    }
}