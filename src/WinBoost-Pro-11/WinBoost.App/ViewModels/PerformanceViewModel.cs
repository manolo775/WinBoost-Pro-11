using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MaterialDesignThemes.Wpf;
using WinBoost.App.Commands;
using WinBoost.App.Localization;
using WinBoost.App.Models;
using WinBoost.App.Services.Health;
using WinBoost.App.Services.Monitoring;
using WinBoost.App.Services.Optimization;
using WinBoost.App.Services.Startup;
using WinBoost.App.Helpers;

namespace WinBoost.App.ViewModels
{
    public class PerformanceViewModel : DashboardViewModel
    {
        private readonly ProcessMonitorService _processMonitorService;
        private readonly ProcessActionsService _processActionsService;
        private readonly TempFileService _tempFileService;
        private readonly DispatcherTimer _processRefreshTimer;
        private readonly StartupAppsScanner _startupAppsScanner;
        private readonly OptimizationCoordinator _optimizationCoordinator;

        private readonly OptimizationSummaryViewModel
            _optimizationSummaryViewModel;

        private readonly OptimizationHistoryViewModel
            _optimizationHistoryViewModel;

        private readonly OptimizationLogViewModel
            _optimizationLogViewModel;

        private bool _isRefreshingProcesses;
        private ProcessInfo? _selectedProcess;
        private ProcessSortMode _selectedProcessSortMode =
                 ProcessSortMode.Cpu;
        private DateTime? _lastProcessesUpdatedLocal;

        private string _processesLastUpdatedText =
            LocalizationHelper.Get(
                "PerformanceProcessesNotUpdated");

        private bool _isAnalyzing;
        private bool _isApplyingOptimizations;

        private string _optimizationStatus =
            LocalizationHelper.Get(
                "PerformanceAnalyzePrompt");

        private int _performanceAnalyzerScore = 100;

        private bool _hasPerformanceAnalysisResult;

        private int _optimizationProgress;

        private string _currentOptimizationOperation =
            string.Empty;

        private OptimizationProgressEventArgs?
              _lastOptimizationProgress;

        public PerformanceViewModel()
        {
            _processMonitorService =
                new ProcessMonitorService();

            _processActionsService =
                new ProcessActionsService();

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
                new ObservableCollection<
                    OptimizationRecommendation>();

            RecommendationItems =
                new ObservableCollection<
                    RecommendationItem>();

            RefreshProcessesCommand =
                new RelayCommand(
                    async _ =>
                        await RefreshTopProcessesAsync(),
                    _ => !_isRefreshingProcesses);

            OpenProcessLocationCommand =
                new RelayCommand(
                    _ => OpenSelectedProcessLocation(),
                    _ => SelectedProcess?.HasExecutablePath == true);

            OptimizeCommand =
                new RelayCommand(
                    async _ =>
                        await AnalyzeSystemAsync(),
                    _ => !IsAnalyzing &&
                         !IsApplyingOptimizations);

            ApplyOptimizationsCommand =
                new RelayCommand(
                    async _ =>
                        await ApplyOptimizationsAsync(),
                    _ => !IsAnalyzing &&
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
                    Interval =
                        TimeSpan.FromSeconds(5)
                };

            _processRefreshTimer.Tick +=
                ProcessRefreshTimer_Tick;

            LanguageManager.Instance.LanguageChanged +=
                LanguageManager_LanguageChanged;

            
        }

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

                bool scoreChanged =
                    _performanceAnalyzerScore !=
                    normalizedValue;

                _performanceAnalyzerScore =
                    normalizedValue;

                _hasPerformanceAnalysisResult =
                    true;

                WinBoostHealthScoreService
                    .Instance
                    .PerformanceScore =
                    normalizedValue;

                if (scoreChanged)
                {
                    OnPropertyChanged();
                }

                OnPropertyChanged(
                    nameof(
                        PerformanceAnalyzerScoreText));

                OnPropertyChanged(
                  nameof(
                        PerformanceAnalyzerProgressValue));

                OnPropertyChanged(
                    nameof(
                        PerformanceAnalyzerStatus));
            }
        }

        public string PerformanceAnalyzerStatus =>
    !_hasPerformanceAnalysisResult
        ? LocalizationHelper.Get(
            "PerformanceScoreNotAnalyzed")
        : PerformanceAnalyzerScore switch
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

        public string PerformanceAnalyzerScoreText =>
    _hasPerformanceAnalysisResult
        ? PerformanceAnalyzerScore.ToString()
        : "--";

        public int PerformanceAnalyzerProgressValue =>
    _hasPerformanceAnalysisResult
        ? PerformanceAnalyzerScore
        : 0;

        public ObservableCollection<ProcessInfo>
            TopProcesses
        {
            get;
        }

        public ObservableCollection<
            OptimizationRecommendation>
            Recommendations
        {
            get;
        }

        public ObservableCollection<
            RecommendationItem>
            RecommendationItems
        {
            get;
        }

        public ICommand RefreshProcessesCommand
        {
            get;
        }

        public ICommand OpenProcessLocationCommand
        {
            get;
        }

        public ProcessInfo? SelectedProcess
        {
            get => _selectedProcess;

            set
            {
                if (_selectedProcess == value)
                {
                    return;
                }

                _selectedProcess = value;

                OnPropertyChanged();

                CommandManager
                    .InvalidateRequerySuggested();
            }
        }

        public ProcessSortMode SelectedProcessSortMode
        {
            get => _selectedProcessSortMode;

            set
            {
                if (_selectedProcessSortMode == value)
                {
                    return;
                }

                _selectedProcessSortMode = value;

                OnPropertyChanged();

                OnPropertyChanged(
                    nameof(PrimaryProcessMetricHeader));

                OnPropertyChanged(
                    nameof(SecondaryProcessMetricHeader));

                SortDisplayedProcesses();

                _ = RefreshTopProcessesAsync();
            }
        }

        public ICommand OptimizeCommand
        {
            get;
        }

        public ICommand ApplyOptimizationsCommand
        {
            get;
        }

        public string ProcessesLastUpdatedText
        {
            get => _processesLastUpdatedText;

            private set
            {
                if (_processesLastUpdatedText ==
                    value)
                {
                    return;
                }

                _processesLastUpdatedText =
                    value;

                OnPropertyChanged();
            }
        }

        public string OptimizationStatus
        {
            get => _optimizationStatus;

            private set
            {
                if (_optimizationStatus == value)
                {
                    return;
                }

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
                {
                    return;
                }

                _isAnalyzing = value;

                OnPropertyChanged();

                OnPropertyChanged(
                    nameof(OptimizationButtonText));

                CommandManager
                    .InvalidateRequerySuggested();
            }
        }

        public bool IsApplyingOptimizations
        {
            get => _isApplyingOptimizations;

            private set
            {
                if (_isApplyingOptimizations ==
                    value)
                {
                    return;
                }

                _isApplyingOptimizations =
                    value;

                OnPropertyChanged();

                OnPropertyChanged(
                    nameof(ApplyButtonText));

                CommandManager
                    .InvalidateRequerySuggested();
            }
        }

        public string OptimizationButtonText =>
            IsAnalyzing
                ? LocalizationHelper.Get(
                    "PerformanceAnalyzing")
                : LocalizationHelper.Get(
                    "PerformanceAnalyze");

        public string ApplyButtonText =>
            IsApplyingOptimizations
                ? LocalizationHelper.Get(
                    "PerformanceApplying")
                : LocalizationHelper.Get(
                    "PerformanceApplySelected");

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
            StartMonitoring(
              loadHistory: false);

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

        private void SortDisplayedProcesses()
        {
            IEnumerable<ProcessInfo> sortedProcesses =
                SelectedProcessSortMode ==
                ProcessSortMode.Memory
                    ? TopProcesses
                        .OrderByDescending(
                            process =>
                                process.MemoryUsageMb)
                        .ThenByDescending(
                            process =>
                                process.CpuUsage)
                        .ToList()
                    : TopProcesses
                        .OrderByDescending(
                            process =>
                                process.CpuUsage)
                        .ThenByDescending(
                            process =>
                                process.MemoryUsageMb)
                        .ToList();

            TopProcesses.Clear();

            foreach (ProcessInfo process
                     in sortedProcesses)
            {
                TopProcesses.Add(process);
            }
        }

        public string PrimaryProcessMetricHeader =>
                SelectedProcessSortMode ==
                ProcessSortMode.Memory
        ? LocalizationHelper.Get(
            "PerformanceColumnRam")
        : LocalizationHelper.Get(
            "PerformanceColumnCpu");

        public string SecondaryProcessMetricHeader =>
            SelectedProcessSortMode ==
            ProcessSortMode.Memory
                ? LocalizationHelper.Get(
                    "PerformanceColumnCpu")
                : LocalizationHelper.Get(
                    "PerformanceColumnRam");

        private async Task RefreshTopProcessesAsync()
        {
            if (_isRefreshingProcesses)
            {
                return;
            }

            try
            {
                _isRefreshingProcesses = true;

                CommandManager
                     .InvalidateRequerySuggested();

                List<ProcessInfo> processes =
                    await _processMonitorService
                        .GetTopProcessesAsync(
                            5,
                          SelectedProcessSortMode);

                int? selectedProcessId =
                    SelectedProcess?.ProcessId;

                TopProcesses.Clear();

                foreach (ProcessInfo process
                         in processes)
                {
                    TopProcesses.Add(process);
                }

                SelectedProcess =
                    selectedProcessId.HasValue
                        ? TopProcesses.FirstOrDefault(
                            process =>
                                process.ProcessId ==
                                selectedProcessId.Value)
                        : null;

                _lastProcessesUpdatedLocal =
                    DateTime.Now;

                UpdateProcessesLastUpdatedText();
            }
            catch
            {
                // Procesele inaccesibile sunt ignorate.
            }
            finally
            {
                _isRefreshingProcesses = false;

                CommandManager
                 .InvalidateRequerySuggested();
            }
        }

        private void OpenSelectedProcessLocation()
        {
            if (SelectedProcess == null)
            {
                return;
            }

            _processActionsService
                .OpenExecutableLocation(
                    SelectedProcess.ExecutablePath);
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

                    messages.Add(cleanupMessage);
                }

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

                    messages.Add(startupMessage);
                }

                if (CpuUsageValue >= 80)
                {
                    string cpuMessage =
                        LocalizationHelper.Get(
                            "PerformanceCpuHighMessage");

                    messages.Add(cpuMessage);

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

                if (RamUsageValue >= 85)
                {
                    string ramMessage =
                        LocalizationHelper.Get(
                            "PerformanceRamHighMessage");

                    messages.Add(ramMessage);

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

                    messages.Add(processMessage);

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

                if (DiskUsageValue >= 90)
                {
                    string diskMessage =
                        LocalizationHelper.Get(
                            "PerformanceDiskLowMessage");

                    messages.Add(diskMessage);

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
                {
                    score -= 10;
                }

                if (RamUsageValue >= 85)
                {
                    score -= 15;
                }

                if (enabledStartupApps >= 5)
                {
                    score -= 10;
                }

                if (tempResult.FileCount > 0)
                {
                    score -= 5;
                }

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

            List<OptimizationRecommendation>
                selectedItems =
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

            bool confirmed =
    NativeConfirmationDialog.Ask(
        Application.Current.MainWindow,
        LocalizationHelper.Get(
            "PerformanceOptimizationConfirmTitle"),
        LocalizationHelper.Get(
            "PerformanceOptimizationConfirmMessage"),
        LocalizationHelper.Get(
            "CommonYes"),
        LocalizationHelper.Get(
            "CommonNo"));

            if (!confirmed)
            {
                return;
            }

            IsApplyingOptimizations = true;

            _optimizationSummaryViewModel.Clear();

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
                        .OptimizeAsync(options);

                OptimizationHistoryService
                    .Instance
                    .Add(report);

                _optimizationSummaryViewModel
                    .Update(report);

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

            OnPropertyChanged(
               nameof(PerformanceAnalyzerStatus));

            if (_lastOptimizationProgress != null)
            {
                CurrentOptimizationOperation =
                    _lastOptimizationProgress
                        .GetLocalizedOperationName();
            }

            UpdateProcessesLastUpdatedText();

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
            _lastOptimizationProgress =
                e;

            Application.Current.Dispatcher.Invoke(
                () =>
                {
                    string localizedOperationName =
                        e.GetLocalizedOperationName();

                    OptimizationProgress =
                        e.ProgressPercentage;

                    CurrentOptimizationOperation =
                        localizedOperationName;

                    OptimizationStatus =
                        $"{e.ProgressPercentage}% - " +
                        localizedOperationName;
                });
        }

        private void UpdateProcessesLastUpdatedText()
        {
            if (!_lastProcessesUpdatedLocal.HasValue)
            {
                ProcessesLastUpdatedText =
                    LocalizationHelper.Get(
                        "PerformanceProcessesNotUpdated");

                return;
            }

            ProcessesLastUpdatedText =
                LocalizationHelper.Format(
                    "PerformanceProcessesLastUpdated",
                    _lastProcessesUpdatedLocal.Value
                        .ToString("HH:mm:ss"));
        }

        private static string FormatBytes(
            long bytes)
        {
            const double megabyte =
                1024d * 1024d;

            const double gigabyte =
                1024d * 1024d * 1024d;

            if (bytes >= gigabyte)
            {
                return
                    $"{bytes / gigabyte:F2} GB";
            }

            return
                $"{bytes / megabyte:F1} MB";
        }
    }
}