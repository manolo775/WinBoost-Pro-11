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

namespace WinBoost.App.ViewModels
{
    public class PerformanceViewModel : DashboardViewModel
    {
        private readonly ProcessMonitorService _processMonitorService;
        private readonly TempFileService _tempFileService;
        private readonly DispatcherTimer _processRefreshTimer;

        private bool _isRefreshingProcesses;
        private bool _isAnalyzing;
        private bool _isApplyingOptimizations;

        private string _optimizationStatus =
            LocalizationHelper.Get("PerformanceAnalyzePrompt");

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

        public PerformanceViewModel()
        {
            _processMonitorService =
                new ProcessMonitorService();

            _tempFileService =
                new TempFileService();

            TopProcesses =
                new ObservableCollection<ProcessInfo>();

            Recommendations =
                new ObservableCollection<OptimizationRecommendation>();

            RecommendationItems =
                new ObservableCollection<RecommendationItem>();

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

                    RecommendationItems.Add(
                          new RecommendationItem
                           {
                                 Icon = PackIconKind.CheckCircle,
                                  IconBrush = Brushes.LimeGreen,
                                 Text = LocalizationHelper.Format(
                                 "PerformanceTempCleanupMessage",
                                space)
                           });

                    messages.Add(
                        LocalizationHelper.Format(
                            "PerformanceTempCleanupMessage",
                            space));
                }

                if (CpuUsageValue >= 80)
                {
                    messages.Add(
                        LocalizationHelper.Get(
                            "PerformanceCpuHighMessage"));
                }

   

                if (RamUsageValue >= 85)
                {
                    messages.Add(
                        LocalizationHelper.Get(
                            "PerformanceRamHighMessage"));
                }

                     RecommendationItems.Add(
                         new RecommendationItem
                      {
                            Icon = PackIconKind.Alert,
                         IconBrush = Brushes.Gold,
                           Text = LocalizationHelper.Get(
                            "PerformanceRamHighMessage")
                            });

                 await RefreshTopProcessesAsync();

                ProcessInfo? highestCpuProcess =
                    TopProcesses
                        .OrderByDescending(
                            process => process.CpuUsage)
                        .FirstOrDefault();

                if (highestCpuProcess != null &&
                    highestCpuProcess.CpuUsage >= 20)
                {
                    messages.Add(
                        LocalizationHelper.Format(
                            "PerformanceHighCpuProcessMessage",
                            highestCpuProcess.Name,
                            highestCpuProcess.CpuUsage.ToString("F1")));

                    RecommendationItems.Add(
                        new RecommendationItem
                        {
                            Icon = PackIconKind.AlertCircle,
                            IconBrush = Brushes.Orange,
                            Text = LocalizationHelper.Format(
                                "PerformanceHighCpuProcessMessage",
                                highestCpuProcess.Name,
                                highestCpuProcess.CpuUsage.ToString("F1"))
                        });
                }

                if (highestCpuProcess != null &&
                    highestCpuProcess.CpuUsage >= 20)
                {
                    messages.Add(
                        LocalizationHelper.Format(
                            "PerformanceHighCpuProcessMessage",
                            highestCpuProcess.Name,
                            highestCpuProcess.CpuUsage.ToString("F1")));
                }

                if (DiskUsageValue >= 90)
                {
                    messages.Add(
                        LocalizationHelper.Get(
                            "PerformanceDiskLowMessage"));
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
                CommandManager.InvalidateRequerySuggested();
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

            if (confirmation != MessageBoxResult.Yes)
                return;

            IsApplyingOptimizations = true;
            OptimizationStatus =
                LocalizationHelper.Get(
                    "PerformanceApplyingOptimizations");

            try
            {
                int deletedFiles = 0;
                long freedBytes = 0;

                foreach (OptimizationRecommendation item
         in selectedItems)
                {
                    if (!item.IsActionable)
                    {
                        continue;
                    }

                    switch (item.ActionId)
                    {
                        case "temp-cleanup":
                            {
                                var result =
                                    await _tempFileService
                                        .CleanAsync();

                                deletedFiles +=
                                    result.DeletedFiles;

                                freedBytes +=
                                    result.FreedBytes;

                                break;
                            }
                    }
                }

                Recommendations.Clear();

                CommandManager.InvalidateRequerySuggested();

                OptimizationStatus =
                       LocalizationHelper.Format(
                       "PerformanceCleanupCompleted",
                        deletedFiles,
         FormatBytes(freedBytes));
            }
            catch (Exception ex)
            {
                OptimizationStatus =
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