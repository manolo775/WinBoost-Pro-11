using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WinBoost.App.Localization;
using WinBoost.App.Models;
using WinBoost.App.Services.Alerts;
using WinBoost.App.Services.Health;
using WinBoost.App.Services.History;
using WinBoost.App.Services.Monitoring;
using System.Windows;
using WinBoost.App.Commands;
using WinBoost.App.Helpers;

namespace WinBoost.App.ViewModels
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        private readonly SystemMonitorService
            _systemMonitorService;

        private readonly SystemMetricsHistoryService
            _metricsHistoryService;

        private readonly PerformanceHistoryRecorder
            _performanceHistoryRecorder;

        private readonly PerformanceHistoryAnalysisService
            _performanceHistoryAnalysisService;

        private readonly
            PerformanceAnalysisRecommendationService
            _performanceAnalysisRecommendationService;

        private readonly PerformanceAlertService
            _performanceAlertService;

        private readonly SystemHealthCalculator
            _systemHealthCalculator;

        private readonly SystemHealthStateService
            _healthStateService;

        private readonly DispatcherTimer
            _refreshTimer;

        private bool _isRefreshingSystemInfo;
        private bool _isLoadingHistory;

        private int _selectedHistoryRangeIndex;

        private DateTime
            _lastPersistentHistoryRefreshUtc =
            DateTime.MinValue;

        private string _cpuUsage = "0 %";
        private string _ramUsage = "0 %";
        private string _ramDetails = "-- GB / -- GB";
        private string _diskUsage = "-- %";
        private string _uptime = "--";
        private string _cpuStatus = "Normal";
        private string _cpuTemperature = "--";

        private bool _isCpuTemperatureAvailable;

        private double _cpuUsageValue;
        private double _ramUsageValue;
        private double _diskUsageValue;

        private IReadOnlyList<SystemMetricsHistoryPoint>
            _displayedMetricsHistory =
                Array.Empty<SystemMetricsHistoryPoint>();

        private PerformanceHistoryAnalysis
            _performanceAnalysis =
                new PerformanceHistoryAnalysis
                {
                    CpuTrend = PerformanceTrend.Unknown,
                    RamTrend = PerformanceTrend.Unknown,
                    DiskTrend = PerformanceTrend.Unknown,
                    OverallTrend = PerformanceTrend.Unknown
                };

        private PerformanceAnalysisRecommendation
            _performanceRecommendation =
                new PerformanceAnalysisRecommendation
                {
                    Type =
                        PerformanceAnalysisRecommendationType
                            .InsufficientData,

                    Severity =
                        PerformanceAnalysisSeverity
                            .Information
                };

        private PerformanceAlert?
            _latestPerformanceAlert;

        public DashboardViewModel()
        {
            _systemMonitorService =
                new SystemMonitorService();

            _metricsHistoryService =
                new SystemMetricsHistoryService();

            _performanceHistoryRecorder =
                new PerformanceHistoryRecorder();

            _performanceHistoryAnalysisService =
                new PerformanceHistoryAnalysisService();

            _performanceAnalysisRecommendationService =
                new PerformanceAnalysisRecommendationService();

            _performanceAlertService =
                new PerformanceAlertService();

            PerformanceAlerts =
                new ObservableCollection<
                    PerformanceAlert>();

            DismissPerformanceAlertCommand =
                new RelayCommand(
                    DismissPerformanceAlert,
                    _ => HasPerformanceAlert);

            ClearPerformanceHistoryCommand =
                new AsyncRelayCommand(
                    ClearPerformanceHistoryAsync);

            _systemHealthCalculator =
                new SystemHealthCalculator();

            _healthStateService =
                SystemHealthStateService.Instance;

            HealthSummary =
                _healthStateService.Summary;

            _healthStateService.HealthChanged +=
                HealthStateService_HealthChanged;

            LanguageManager.Instance.LanguageChanged +=
                LanguageManager_LanguageChanged;

            _refreshTimer =
                new DispatcherTimer
                {
                    Interval =
                        TimeSpan.FromSeconds(10)
                };

            _refreshTimer.Tick +=
                RefreshTimer_Tick;

            
        }

        private static string T(
            string key,
            params object[] arguments)
        {
            return LocalizationHelper.Format(
                key,
                arguments);
        }

        public SystemHealthSummary HealthSummary
        {
            get;
        }

        public AsyncRelayCommand
            ClearPerformanceHistoryCommand
        {
            get;
        }

        public ObservableCollection<PerformanceAlert>
            PerformanceAlerts
        {
            get;
        }

        public RelayCommand
            DismissPerformanceAlertCommand
        {
            get;
        }

        public PerformanceAlert?
            LatestPerformanceAlert
        {
            get => _latestPerformanceAlert;

            private set
            {
                if (ReferenceEquals(
                    _latestPerformanceAlert,
                    value))
                {
                    return;
                }

                _latestPerformanceAlert = value;

                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(HasPerformanceAlert));

                OnPropertyChanged(
                    nameof(PerformanceAlertTitle));

                OnPropertyChanged(
                    nameof(PerformanceAlertSeverityText));

                OnPropertyChanged(
                    nameof(PerformanceAlertMessage));

                OnPropertyChanged(
                    nameof(PerformanceAlertDurationText));

                OnPropertyChanged(
                    nameof(PerformanceAlertBrush));

                OnPropertyChanged(
                    nameof(PerformanceAlertBackground));

                OnPropertyChanged(
                    nameof(PerformanceAlertIcon));

                CommandManager
                    .InvalidateRequerySuggested();
            }
        }

        public bool HasPerformanceAlert =>
            LatestPerformanceAlert != null;

        public bool IsCriticalAlertSoundEnabled
        {
            get =>
                _performanceAlertService
                    .Settings
                    .EnableSoundForCriticalAlerts;

            set
            {
                PerformanceAlertSettings settings =
                    _performanceAlertService.Settings;

                if (settings
                    .EnableSoundForCriticalAlerts == value)
                {
                    return;
                }

                settings.EnableSoundForCriticalAlerts =
                    value;

                _performanceAlertService
                    .UpdateSettings(
                        settings);

                OnPropertyChanged();
            }
        }

        public int SelectedCriticalAlertRepeatIndex
        {
            get =>
                _performanceAlertService
                    .Settings
                    .CriticalAlertRepeatIntervalMinutes
                switch
                {
                    0 => 0,
                    5 => 1,
                    15 => 2,
                    30 => 3,
                    _ => 2
                };

            set
            {
                int repeatIntervalMinutes =
                    value switch
                    {
                        0 => 0,
                        1 => 5,
                        2 => 15,
                        3 => 30,
                        _ => 15
                    };

                PerformanceAlertSettings settings =
                    _performanceAlertService.Settings;

                if (settings
                    .CriticalAlertRepeatIntervalMinutes ==
                    repeatIntervalMinutes)
                {
                    return;
                }

                settings
                    .CriticalAlertRepeatIntervalMinutes =
                        repeatIntervalMinutes;

                _performanceAlertService
                    .UpdateSettings(
                        settings);

                OnPropertyChanged();
            }
        }

        public string PerformanceAlertTitle =>
            T("PerformanceAlertTitle");

        public string PerformanceAlertSeverityText =>
            LatestPerformanceAlert?.Severity ==
            PerformanceAlertSeverity.Critical
                ? T("PerformanceAlertCritical")
                : T("PerformanceAlertWarning");

        public string PerformanceAlertMessage =>
            GetPerformanceAlertMessage();

        public string PerformanceAlertDurationText =>
            LatestPerformanceAlert == null
                ? string.Empty
                : T(
                    "PerformanceAlertSustainedDuration",
                    Math.Max(
                        1,
                        (int)Math.Round(
                            LatestPerformanceAlert
                                .SustainedDuration
                                .TotalSeconds)));

        public Brush PerformanceAlertBrush =>
            LatestPerformanceAlert?.Severity ==
            PerformanceAlertSeverity.Critical
                ? Brushes.OrangeRed
                : Brushes.Gold;

        public Brush PerformanceAlertBackground =>
            LatestPerformanceAlert?.Severity ==
            PerformanceAlertSeverity.Critical
                ? new SolidColorBrush(
                    Color.FromRgb(
                        74,
                        37,
                        37))
                : new SolidColorBrush(
                    Color.FromRgb(
                        74,
                        61,
                        34));

        public PackIconKind PerformanceAlertIcon =>
            LatestPerformanceAlert?.Severity ==
            PerformanceAlertSeverity.Critical
                ? PackIconKind.AlertCircle
                : PackIconKind.Alert;

        public IReadOnlyList<SystemMetricsHistoryPoint>
            MetricsHistory =>
            _metricsHistoryService.GetSnapshot();

        public IReadOnlyList<SystemMetricsHistoryPoint>
            DisplayedMetricsHistory
        {
            get => _displayedMetricsHistory;

            private set
            {
                _displayedMetricsHistory = value;

                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(HasDisplayedHistory));
            }
        }

        public bool HasDisplayedHistory =>
            DisplayedMetricsHistory.Count > 0;

        public PerformanceHistoryAnalysis
            PerformanceAnalysis
        {
            get => _performanceAnalysis;

            private set
            {
                _performanceAnalysis = value;

                PerformanceRecommendation =
                    _performanceAnalysisRecommendationService
                        .CreateRecommendation(
                            value);

                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(HasPerformanceAnalysis));

                NotifyPerformanceAnalysisProperties();
            }
        }

        public bool HasPerformanceAnalysis =>
            PerformanceAnalysis.HasEnoughData;

        public bool HasCpuTemperatureAnalysis =>
            PerformanceAnalysis
                .AverageCpuTemperature
                .HasValue &&
            PerformanceAnalysis
                .MinimumCpuTemperature
                .HasValue &&
            PerformanceAnalysis
                .MaximumCpuTemperature
                .HasValue;

        public string CpuTemperatureAnalysisText =>
            HasCpuTemperatureAnalysis
                ? T(
                    "DashboardAnalysisTemperatureStatistics",
                    PerformanceAnalysis
                        .AverageCpuTemperature!
                        .Value
                        .ToString("F1"),
                    PerformanceAnalysis
                        .MinimumCpuTemperature!
                        .Value
                        .ToString("F1"),
                    PerformanceAnalysis
                        .MaximumCpuTemperature!
                        .Value
                        .ToString("F1"))
                : string.Empty;

        public PerformanceAnalysisRecommendation
            PerformanceRecommendation
        {
            get => _performanceRecommendation;

            private set
            {
                _performanceRecommendation = value;

                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(PerformanceRecommendationText));

                OnPropertyChanged(
                    nameof(PerformanceRecommendationBrush));

                OnPropertyChanged(
                    nameof(PerformanceRecommendationBackground));
            }
        }

        public string PerformanceRecommendationText =>
            GetPerformanceRecommendationText();

        public Brush PerformanceRecommendationBrush =>
            PerformanceRecommendation.Severity switch
            {
                PerformanceAnalysisSeverity.Good =>
                    Brushes.LimeGreen,

                PerformanceAnalysisSeverity.Warning =>
                    Brushes.Gold,

                PerformanceAnalysisSeverity.Critical =>
                    Brushes.OrangeRed,

                _ =>
                    Brushes.LightGray
            };

        public Brush
            PerformanceRecommendationBackground =>
            PerformanceRecommendation.Severity switch
            {
                PerformanceAnalysisSeverity.Good =>
                    new SolidColorBrush(
                        Color.FromRgb(
                            31,
                            58,
                            36)),

                PerformanceAnalysisSeverity.Warning =>
                    new SolidColorBrush(
                        Color.FromRgb(
                            74,
                            61,
                            34)),

                PerformanceAnalysisSeverity.Critical =>
                    new SolidColorBrush(
                        Color.FromRgb(
                            74,
                            37,
                            37)),

                _ =>
                    new SolidColorBrush(
                        Color.FromRgb(
                            53,
                            40,
                            68))
            };

        public string AnalysisSampleCountText =>
            T(
                "DashboardAnalysisSamples",
                PerformanceAnalysis.SampleCount);

        public string CpuAnalysisTrendText =>
            GetPerformanceTrendText(
                PerformanceAnalysis.CpuTrend);

        public string RamAnalysisTrendText =>
            GetPerformanceTrendText(
                PerformanceAnalysis.RamTrend);

        public string DiskAnalysisTrendText =>
            GetPerformanceTrendText(
                PerformanceAnalysis.DiskTrend);

        public string OverallAnalysisTrendText =>
            GetPerformanceTrendText(
                PerformanceAnalysis.OverallTrend);

        public string CpuAnalysisChangeText =>
            GetPerformanceChangeText(
                PerformanceAnalysis.CpuTrend,
                PerformanceAnalysis.CpuChange);

        public string RamAnalysisChangeText =>
            GetPerformanceChangeText(
                PerformanceAnalysis.RamTrend,
                PerformanceAnalysis.RamChange);

        public string DiskAnalysisChangeText =>
            GetPerformanceChangeText(
                PerformanceAnalysis.DiskTrend,
                PerformanceAnalysis.DiskChange);

        public Brush CpuAnalysisTrendBrush =>
            GetPerformanceTrendBrush(
                PerformanceAnalysis.CpuTrend);

        public Brush RamAnalysisTrendBrush =>
            GetPerformanceTrendBrush(
                PerformanceAnalysis.RamTrend);

        public Brush DiskAnalysisTrendBrush =>
            GetPerformanceTrendBrush(
                PerformanceAnalysis.DiskTrend);

        public Brush OverallAnalysisTrendBrush =>
            GetPerformanceTrendBrush(
                PerformanceAnalysis.OverallTrend);

        public bool IsLoadingHistory
        {
            get => _isLoadingHistory;

            private set
            {
                if (_isLoadingHistory == value)
                {
                    return;
                }

                _isLoadingHistory = value;
                OnPropertyChanged();
            }
        }

        public int SelectedHistoryRangeIndex
        {
            get => _selectedHistoryRangeIndex;

            set
            {
                if (value < 0 ||
                    value > 3 ||
                    _selectedHistoryRangeIndex == value)
                {
                    return;
                }

                _selectedHistoryRangeIndex = value;

                OnPropertyChanged();
                OnPropertyChanged(
                    nameof(SelectedHistoryRange));

                _ = LoadSelectedHistoryAsync();
            }
        }

        public PerformanceHistoryRange
            SelectedHistoryRange =>
            (PerformanceHistoryRange)
            SelectedHistoryRangeIndex;

        public double CpuUsageValue
        {
            get => _cpuUsageValue;

            set
            {
                if (Math.Abs(
                        _cpuUsageValue - value) < 0.01)
                {
                    return;
                }

                _cpuUsageValue = value;
                OnPropertyChanged();
            }
        }

        public double RamUsageValue
        {
            get => _ramUsageValue;

            set
            {
                if (Math.Abs(
                        _ramUsageValue - value) < 0.01)
                {
                    return;
                }

                _ramUsageValue = value;
                OnPropertyChanged();
            }
        }

        public double DiskUsageValue
        {
            get => _diskUsageValue;

            set
            {
                if (Math.Abs(
                        _diskUsageValue - value) < 0.01)
                {
                    return;
                }

                _diskUsageValue = value;
                OnPropertyChanged();
            }
        }

        public string CpuStatus
        {
            get => _cpuStatus;

            set
            {
                if (_cpuStatus == value)
                {
                    return;
                }

                _cpuStatus = value;
                OnPropertyChanged();
            }
        }

        public string CpuUsage
        {
            get => _cpuUsage;

            set
            {
                if (_cpuUsage == value)
                {
                    return;
                }

                _cpuUsage = value;
                OnPropertyChanged();
            }
        }

        public string RamUsage
        {
            get => _ramUsage;

            set
            {
                if (_ramUsage == value)
                {
                    return;
                }

                _ramUsage = value;
                OnPropertyChanged();
            }
        }

        public string RamDetails
        {
            get => _ramDetails;

            set
            {
                if (_ramDetails == value)
                {
                    return;
                }

                _ramDetails = value;
                OnPropertyChanged();
            }
        }

        public string DiskUsage
        {
            get => _diskUsage;

            set
            {
                if (_diskUsage == value)
                {
                    return;
                }

                _diskUsage = value;
                OnPropertyChanged();
            }
        }

        public string Uptime
        {
            get => _uptime;

            set
            {
                if (_uptime == value)
                {
                    return;
                }

                _uptime = value;
                OnPropertyChanged();
            }
        }

        public string CpuTemperature
        {
            get => _cpuTemperature;

            set
            {
                if (_cpuTemperature == value)
                {
                    return;
                }

                _cpuTemperature = value;
                OnPropertyChanged();
            }
        }

        public bool IsCpuTemperatureAvailable
        {
            get => _isCpuTemperatureAvailable;

            set
            {
                if (_isCpuTemperatureAvailable == value)
                {
                    return;
                }

                _isCpuTemperatureAvailable = value;
                OnPropertyChanged();
            }
        }

        public string CpuSummary =>
            CpuUsageValue >= 85
                ? T(
                    "DashboardCpuCritical",
                    CpuUsageValue.ToString("F0"))
                : CpuUsageValue >= 60
                    ? T(
                        "DashboardCpuHigh",
                        CpuUsageValue.ToString("F0"))
                    : T(
                        "DashboardCpuNormal",
                        CpuUsageValue.ToString("F0"));

        public PackIconKind CpuSummaryIcon =>
            CpuUsageValue >= 85
                ? PackIconKind.AlertCircle
                : CpuUsageValue >= 60
                    ? PackIconKind.Alert
                    : PackIconKind.CheckCircle;

        public Brush CpuSummaryBrush =>
            CpuUsageValue >= 85
                ? Brushes.OrangeRed
                : CpuUsageValue >= 60
                    ? Brushes.Gold
                    : Brushes.LimeGreen;

        public string RamSummary =>
            RamUsageValue >= 90
                ? T(
                    "DashboardRamCritical",
                    RamUsageValue.ToString("F0"))
                : RamUsageValue >= 75
                    ? T(
                        "DashboardRamHigh",
                        RamUsageValue.ToString("F0"))
                    : T(
                        "DashboardRamNormal",
                        RamUsageValue.ToString("F0"));

        public PackIconKind RamSummaryIcon =>
            RamUsageValue >= 90
                ? PackIconKind.AlertCircle
                : RamUsageValue >= 75
                    ? PackIconKind.Alert
                    : PackIconKind.CheckCircle;

        public Brush RamSummaryBrush =>
            RamUsageValue >= 90
                ? Brushes.OrangeRed
                : RamUsageValue >= 75
                    ? Brushes.Gold
                    : Brushes.LimeGreen;

        public string DiskSummary =>
            DiskUsageValue >= 90
                ? T(
                    "DashboardDiskCritical",
                    DiskUsageValue.ToString("F0"))
                : DiskUsageValue >= 80
                    ? T(
                        "DashboardDiskLow",
                        DiskUsageValue.ToString("F0"))
                    : T(
                        "DashboardDiskNormal",
                        DiskUsageValue.ToString("F0"));

        public PackIconKind DiskSummaryIcon =>
            DiskUsageValue >= 90
                ? PackIconKind.AlertCircle
                : DiskUsageValue >= 80
                    ? PackIconKind.Alert
                    : PackIconKind.CheckCircle;

        public Brush DiskSummaryBrush =>
            DiskUsageValue >= 90
                ? Brushes.OrangeRed
                : DiskUsageValue >= 80
                    ? Brushes.Gold
                    : Brushes.LimeGreen;

        public string HealthSummaryText =>
            HealthScore >= 85
                ? T(
                    "DashboardHealthExcellent",
                    HealthScore)
                : HealthScore >= 65
                    ? T(
                        "DashboardHealthAttention",
                        HealthScore)
                    : T(
                        "DashboardHealthCritical",
                        HealthScore);

        public PackIconKind HealthSummaryIcon =>
            HealthScore >= 85
                ? PackIconKind.CheckCircle
                : HealthScore >= 65
                    ? PackIconKind.Alert
                    : PackIconKind.AlertCircle;

        public Brush HealthSummaryBrush =>
            HealthScore >= 85
                ? Brushes.LimeGreen
                : HealthScore >= 65
                    ? Brushes.Gold
                    : Brushes.OrangeRed;

        public string OverallRecommendation
        {
            get
            {
                if (RamUsageValue >= 90)
                {
                    return T(
                        "DashboardRecommendationRam");
                }

                if (CpuUsageValue >= 85)
                {
                    return T(
                        "DashboardRecommendationCpu");
                }

                if (DiskUsageValue >= 90)
                {
                    return T(
                        "DashboardRecommendationDisk");
                }

                if (HealthScore < 65)
                {
                    return T(
                        "DashboardRecommendationHealth");
                }

                return T(
                    "DashboardRecommendationGood");
            }
        }

        public Brush RecommendationBackground =>
            HealthScore >= 85
                ? new SolidColorBrush(
                    Color.FromRgb(31, 58, 36))
                : HealthScore >= 65
                    ? new SolidColorBrush(
                        Color.FromRgb(74, 61, 34))
                    : new SolidColorBrush(
                        Color.FromRgb(74, 37, 37));

        public int HealthScore =>
            HealthSummary.OverallHealthScore;

        public string HealthStatus =>
            HealthSummary.OverallHealthStatus;

        public void StartMonitoring(
    bool loadHistory = true)
        {
            if (_refreshTimer.IsEnabled)
            {
                return;
            }

            _refreshTimer.Start();

            _ = UpdateSystemInfoAsync();

            if (loadHistory)
            {
                _ = LoadSelectedHistoryAsync();
            }
        }

        public void StopMonitoring()
        {
            _refreshTimer.Stop();
        }

        private async void RefreshTimer_Tick(
            object? sender,
            EventArgs e)
        {
            await UpdateSystemInfoAsync();
        }

        private async Task UpdateSystemInfoAsync()
        {
            if (_isRefreshingSystemInfo)
            {
                return;
            }

            try
            {
                _isRefreshingSystemInfo = true;

                var metrics =
                    await _systemMonitorService
                        .GetSystemMetricsAsync();
              
                ProcessPerformanceAlerts(
                    metrics);

                try
                {
                    await _performanceHistoryRecorder
                        .RecordIfDueAsync(
                            metrics.CpuUsage,
                            metrics.RamUsage,
                            metrics.DiskUsage,
                            metrics.CpuTemperature);
                }
                catch
                {
                    // Istoricul persistent nu trebuie să
                    // blocheze actualizarea Dashboard-ului.
                }

                CpuUsageValue =
                    metrics.CpuUsage;

                RamUsageValue =
                    metrics.RamUsage;

                DiskUsageValue =
                    metrics.DiskUsage;

                _metricsHistoryService.Add(
                    metrics.CpuUsage,
                    metrics.RamUsage,
                    metrics.DiskUsage);

                OnPropertyChanged(
                    nameof(MetricsHistory));

                if (SelectedHistoryRange ==
                    PerformanceHistoryRange
                        .LiveFiveMinutes)
                {
                    DisplayedMetricsHistory =
                        _metricsHistoryService
                            .GetSnapshot();

                    // UpdateLivePerformanceAnalysis();
                }
                else if (
                    DateTime.UtcNow -
                    _lastPersistentHistoryRefreshUtc >=
                    TimeSpan.FromMinutes(1))
                {
                    _lastPersistentHistoryRefreshUtc =
                        DateTime.UtcNow;

                    _ = LoadSelectedHistoryAsync();
                }

                CpuStatus =
                    GetUsageStatus(
                        metrics.CpuUsage);

                CpuUsage =
                    $"{metrics.CpuUsage:F1} %";

                RamUsage =
                    $"{metrics.RamUsage:F0} %";

                RamDetails =
                    $"{metrics.UsedRamGB:F1} GB / " +
                    $"{metrics.TotalRamGB:F1} GB";

                DiskUsage =
                    $"{metrics.DiskUsage:F0} %";

                Uptime =
                    metrics.Uptime;

                IsCpuTemperatureAvailable =
                    metrics.CpuTemperature.IsAvailable;

                CpuTemperature =
                    metrics.CpuTemperature.IsAvailable
                        ? $"{metrics.CpuTemperature.Celsius:F0} °C"
                        : T(
                            "DashboardCpuTemperatureUnavailable");

                UpdatePerformanceScore();
                UpdateSystemSummary();
            }
            catch
            {
                // Aplicația rămâne funcțională
                // dacă o citire eșuează.
            }
            finally
            {
                _isRefreshingSystemInfo = false;
            }
        }

        private async Task
            ClearPerformanceHistoryAsync()
        {
            bool confirmed =
                NativeConfirmationDialog.Ask(
                    Application.Current.MainWindow,
                    T("DashboardHistoryClearTitle"),
                    T("DashboardHistoryClearConfirmation"),
                    T("CommonYes"),
                    T("CommonNo"));

            if (!confirmed)
            {
                return;
            }

            try
            {
                await _performanceHistoryRecorder
                    .ClearHistoryAsync();

                _lastPersistentHistoryRefreshUtc =
                    DateTime.MinValue;

                if (SelectedHistoryRange !=
                    PerformanceHistoryRange
                        .LiveFiveMinutes)
                {
                    DisplayedMetricsHistory =
                        Array.Empty<
                            SystemMetricsHistoryPoint>();

                    PerformanceAnalysis =
                        CreateEmptyPerformanceAnalysis();
                }

                MessageBox.Show(
                    Application.Current.MainWindow,
                    T("DashboardHistoryClearSuccess"),
                    T("DashboardHistoryClearSuccessTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch
            {
                MessageBox.Show(
                    Application.Current.MainWindow,
                    T("DashboardHistoryClearError"),
                    T("CommonError"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async Task LoadSelectedHistoryAsync()
        {
            PerformanceHistoryRange requestedRange =
                SelectedHistoryRange;

            if (requestedRange ==
                PerformanceHistoryRange
                    .LiveFiveMinutes)
            {
                DisplayedMetricsHistory =
                    _metricsHistoryService
                        .GetSnapshot();

                UpdateLivePerformanceAnalysis();

                return;
            }

            if (IsLoadingHistory)
            {
                return;
            }

            IsLoadingHistory = true;

            try
            {
                DateTime toUtc =
                    DateTime.UtcNow;

                DateTime fromUtc =
                    requestedRange switch
                    {
                        PerformanceHistoryRange.LastHour =>
                            toUtc.AddHours(-1),

                        PerformanceHistoryRange.Last24Hours =>
                            toUtc.AddHours(-24),

                        PerformanceHistoryRange.Last7Days =>
                            toUtc.AddDays(-7),

                        _ =>
                            toUtc.AddMinutes(-5)
                    };

                TimeSpan rangeDuration =
                    toUtc - fromUtc;

                DateTime previousToUtc =
                    fromUtc;

                DateTime previousFromUtc =
                    previousToUtc - rangeDuration;

                Task<IReadOnlyList<
                    PerformanceHistoryRecord>>
                    currentRecordsTask =
                        _performanceHistoryRecorder
                            .GetRecordsAsync(
                                fromUtc,
                                toUtc);

                Task<IReadOnlyList<
                    PerformanceHistoryRecord>>
                    previousRecordsTask =
                        _performanceHistoryRecorder
                            .GetRecordsAsync(
                                previousFromUtc,
                                previousToUtc);

                await Task.WhenAll(
                    currentRecordsTask,
                    previousRecordsTask);

                IReadOnlyList<
                    PerformanceHistoryRecord> records =
                    await currentRecordsTask;

                IReadOnlyList<
                    PerformanceHistoryRecord>
                    previousRecords =
                        await previousRecordsTask;

                if (SelectedHistoryRange !=
                    requestedRange)
                {
                    return;
                }

                DisplayedMetricsHistory =
                    records
                        .Select(record =>
                            new SystemMetricsHistoryPoint
                            {
                                Timestamp =
                                    record.Timestamp
                                        .ToLocalTime(),

                                CpuUsage =
                                    record.CpuUsage,

                                RamUsage =
                                    record.RamUsage,

                                DiskUsage =
                                    record.DiskUsage
                            })
                        .ToList();

                PerformanceAnalysis =
                    _performanceHistoryAnalysisService
                        .Analyze(
                            records,
                            previousRecords);

                _lastPersistentHistoryRefreshUtc =
                    DateTime.UtcNow;
            }
            catch
            {
                if (SelectedHistoryRange ==
                    requestedRange)
                {
                    DisplayedMetricsHistory =
                        Array.Empty<
                            SystemMetricsHistoryPoint>();

                    PerformanceAnalysis =
                        CreateEmptyPerformanceAnalysis();
                }
            }
            finally
            {
                IsLoadingHistory = false;

                if (SelectedHistoryRange !=
                    requestedRange)
                {
                    _ = LoadSelectedHistoryAsync();
                }
            }
        }

        private void UpdateLivePerformanceAnalysis()
        {
            IReadOnlyList<SystemMetricsHistoryPoint>
                livePoints =
                    _metricsHistoryService
                        .GetSnapshot();

            List<PerformanceHistoryRecord>
                liveRecords =
                    livePoints
                        .TakeLast(60)
                        .Select(point =>
                            new PerformanceHistoryRecord
                            {
                                Timestamp =
                                    point.Timestamp
                                        .ToUniversalTime(),

                                CpuUsage =
                                    point.CpuUsage,

                                RamUsage =
                                    point.RamUsage,

                                DiskUsage =
                                    point.DiskUsage
                            })
                        .ToList();

            PerformanceAnalysis =
                _performanceHistoryAnalysisService
                    .Analyze(
                        liveRecords,
                        Array.Empty<
                            PerformanceHistoryRecord>());
        }

        private void ProcessPerformanceAlerts(
    SystemMetrics metrics)
        {
            try
            {
                IReadOnlyList<PerformanceAlert>
                    newAlerts =
                        _performanceAlertService
                            .Evaluate(
                                metrics);

                foreach (PerformanceAlert alert
                         in newAlerts)
                {
                    PerformanceAlerts.Insert(
                        0,
                        alert);
                }

                while (PerformanceAlerts.Count > 20)
                {
                    PerformanceAlerts.RemoveAt(
                        PerformanceAlerts.Count - 1);
                }

                PerformanceAlert? mostImportantAlert =
                    newAlerts
                        .OrderByDescending(
                            alert =>
                                alert.Severity)
                        .FirstOrDefault();

                if (mostImportantAlert != null)
                {
                    LatestPerformanceAlert =
                        mostImportantAlert;

                    _ = PlayCriticalAlertSoundIfEnabledAsync(
                       mostImportantAlert);
                }
            }
            catch
            {
                // Alertele nu trebuie să blocheze
                // monitorizarea principală.
            }
        }

        private async Task
    PlayCriticalAlertSoundIfEnabledAsync(
        PerformanceAlert alert)
        {
            if (alert.Severity !=
                PerformanceAlertSeverity.Critical)
            {
                return;
            }

            if (!_performanceAlertService
                .Settings
                .EnableSoundForCriticalAlerts)
            {
                return;
            }

            try
            {
                const int soundRepetitions = 3;

                for (int index = 0;
                     index < soundRepetitions;
                     index++)
                {
                    System.Media.SystemSounds
                        .Exclamation
                        .Play();

                    if (index <
                        soundRepetitions - 1)
                    {
                        await Task.Delay(
                            TimeSpan.FromSeconds(1));
                    }
                }
            }
            catch
            {
                // Lipsa sunetului nu trebuie să
                // afecteze funcționarea aplicației.
            }
        }

        private string GetPerformanceAlertMessage()
        {
            if (LatestPerformanceAlert == null)
            {
                return string.Empty;
            }

            string resourceKey =
                (LatestPerformanceAlert.Type,
                 LatestPerformanceAlert.Severity)
                switch
                {
                    (
                        PerformanceAlertType.CpuHigh,
                        PerformanceAlertSeverity.Critical) =>
                        "PerformanceAlertCpuCritical",

                    (
                        PerformanceAlertType.CpuHigh,
                        _) =>
                        "PerformanceAlertCpuWarning",

                    (
                        PerformanceAlertType.RamHigh,
                        PerformanceAlertSeverity.Critical) =>
                        "PerformanceAlertRamCritical",

                    (
                        PerformanceAlertType.RamHigh,
                        _) =>
                        "PerformanceAlertRamWarning",

                    (
                        PerformanceAlertType.DiskHigh,
                        PerformanceAlertSeverity.Critical) =>
                        "PerformanceAlertDiskCritical",

                    (
                        PerformanceAlertType.DiskHigh,
                        _) =>
                        "PerformanceAlertDiskWarning",

                    (
                        PerformanceAlertType
                            .CpuTemperatureHigh,
                        PerformanceAlertSeverity.Critical) =>
                        "PerformanceAlertTemperatureCritical",

                    _ =>
                        "PerformanceAlertTemperatureWarning"
                };

            return T(
                resourceKey,
                LatestPerformanceAlert.CurrentValue,
                LatestPerformanceAlert.Threshold);
        }

        private void DismissPerformanceAlert(
            object? parameter)
        {
            if (LatestPerformanceAlert == null)
            {
                return;
            }

            LatestPerformanceAlert.IsAcknowledged =
                true;

            LatestPerformanceAlert = null;
        }

        private static PerformanceHistoryAnalysis
            CreateEmptyPerformanceAnalysis()
        {
            return new PerformanceHistoryAnalysis
            {
                HasEnoughData = false,
                SampleCount = 0,
                CpuTrend = PerformanceTrend.Unknown,
                RamTrend = PerformanceTrend.Unknown,
                DiskTrend = PerformanceTrend.Unknown,
                OverallTrend = PerformanceTrend.Unknown
            };
        }

        private string
            GetPerformanceRecommendationText()
        {
            return PerformanceRecommendation.Type switch
            {
                PerformanceAnalysisRecommendationType
                    .Good =>
                    T(
                        "DashboardAnalysisRecommendationGood"),

                PerformanceAnalysisRecommendationType
                    .CpuHigh =>
                    T(
                        "DashboardAnalysisRecommendationCpuHigh",
                        PerformanceAnalysis
                            .AverageCpuUsage
                            .ToString("F1")),

                PerformanceAnalysisRecommendationType
                    .CpuIncreasing =>
                    T(
                        "DashboardAnalysisRecommendationCpuIncreasing",
                        Math.Abs(
                                PerformanceAnalysis
                                    .CpuChange)
                            .ToString("F1")),

                PerformanceAnalysisRecommendationType
                    .CpuTemperatureHigh =>
                    T(
                        "DashboardAnalysisRecommendationCpuTemperatureHigh",
                        PerformanceAnalysis
                            .AverageCpuTemperature
                            .GetValueOrDefault()
                            .ToString("F1"),
                        PerformanceAnalysis
                            .MaximumCpuTemperature
                            .GetValueOrDefault()
                            .ToString("F1")),

                PerformanceAnalysisRecommendationType
                    .RamHigh =>
                    T(
                        "DashboardAnalysisRecommendationRamHigh",
                        PerformanceAnalysis
                            .AverageRamUsage
                            .ToString("F1")),

                PerformanceAnalysisRecommendationType
                    .RamIncreasing =>
                    T(
                        "DashboardAnalysisRecommendationRamIncreasing",
                        Math.Abs(
                                PerformanceAnalysis
                                    .RamChange)
                            .ToString("F1")),

                PerformanceAnalysisRecommendationType
                    .DiskHigh =>
                    T(
                        "DashboardAnalysisRecommendationDiskHigh",
                        PerformanceAnalysis
                            .AverageDiskUsage
                            .ToString("F1")),

                PerformanceAnalysisRecommendationType
                    .DiskIncreasing =>
                    T(
                        "DashboardAnalysisRecommendationDiskIncreasing",
                        Math.Abs(
                                PerformanceAnalysis
                                    .DiskChange)
                            .ToString("F1")),

                PerformanceAnalysisRecommendationType
                    .MultipleIssues =>
                    T(
                        "DashboardAnalysisRecommendationMultiple"),

                _ =>
                    T(
                        "DashboardAnalysisRecommendationInsufficient")
            };
        }

        private void
            NotifyPerformanceAnalysisProperties()
        {
            OnPropertyChanged(
                nameof(HasCpuTemperatureAnalysis));

            OnPropertyChanged(
                nameof(CpuTemperatureAnalysisText));

            OnPropertyChanged(
                nameof(AnalysisSampleCountText));

            OnPropertyChanged(
                nameof(CpuAnalysisTrendText));

            OnPropertyChanged(
                nameof(RamAnalysisTrendText));

            OnPropertyChanged(
                nameof(DiskAnalysisTrendText));

            OnPropertyChanged(
                nameof(OverallAnalysisTrendText));

            OnPropertyChanged(
                nameof(CpuAnalysisChangeText));

            OnPropertyChanged(
                nameof(RamAnalysisChangeText));

            OnPropertyChanged(
                nameof(DiskAnalysisChangeText));

            OnPropertyChanged(
                nameof(CpuAnalysisTrendBrush));

            OnPropertyChanged(
                nameof(RamAnalysisTrendBrush));

            OnPropertyChanged(
                nameof(DiskAnalysisTrendBrush));

            OnPropertyChanged(
                nameof(OverallAnalysisTrendBrush));
        }

        private static string GetPerformanceTrendText(
            PerformanceTrend trend)
        {
            return trend switch
            {
                PerformanceTrend.Improving =>
                    T(
                        "DashboardAnalysisTrendImproving"),

                PerformanceTrend.Stable =>
                    T(
                        "DashboardAnalysisTrendStable"),

                PerformanceTrend.Degrading =>
                    T(
                        "DashboardAnalysisTrendDegrading"),

                _ =>
                    T(
                        "DashboardAnalysisTrendUnknown")
            };
        }

        private static string
            GetPerformanceChangeText(
                PerformanceTrend trend,
                double change)
        {
            if (trend == PerformanceTrend.Unknown)
            {
                return "--";
            }

            return
                $"{change:+0.0;-0.0;0.0} %";
        }

        private static Brush GetPerformanceTrendBrush(
            PerformanceTrend trend)
        {
            return trend switch
            {
                PerformanceTrend.Improving =>
                    Brushes.LimeGreen,

                PerformanceTrend.Stable =>
                    Brushes.Gold,

                PerformanceTrend.Degrading =>
                    Brushes.OrangeRed,

                _ =>
                    Brushes.LightGray
            };
        }

        private void UpdateSystemSummary()
        {
            OnPropertyChanged(nameof(CpuSummary));
            OnPropertyChanged(nameof(CpuSummaryIcon));
            OnPropertyChanged(nameof(CpuSummaryBrush));

            OnPropertyChanged(nameof(RamSummary));
            OnPropertyChanged(nameof(RamSummaryIcon));
            OnPropertyChanged(nameof(RamSummaryBrush));

            OnPropertyChanged(nameof(DiskSummary));
            OnPropertyChanged(nameof(DiskSummaryIcon));
            OnPropertyChanged(nameof(DiskSummaryBrush));

            OnPropertyChanged(nameof(HealthSummaryText));
            OnPropertyChanged(nameof(HealthSummaryIcon));
            OnPropertyChanged(nameof(HealthSummaryBrush));

            OnPropertyChanged(nameof(OverallRecommendation));
            OnPropertyChanged(
                nameof(RecommendationBackground));
        }

        private void UpdatePerformanceScore()
        {
            int performanceScore =
                _systemHealthCalculator
                    .CalculatePerformanceScore(
                        CpuUsageValue,
                        RamUsageValue,
                        DiskUsageValue);

            _healthStateService
                .UpdatePerformanceScore(
                    performanceScore);
        }

        private void HealthStateService_HealthChanged(
            object? sender,
            EventArgs e)
        {
            OnPropertyChanged(
                nameof(HealthScore));

            OnPropertyChanged(
                nameof(HealthStatus));

            OnPropertyChanged(
                nameof(HealthSummary));

            UpdateSystemSummary();
            NotifyPerformanceAnalysisProperties();

            OnPropertyChanged(
                nameof(PerformanceRecommendationText));

            OnPropertyChanged(
                nameof(PerformanceRecommendationBrush));

            OnPropertyChanged(
                nameof(PerformanceRecommendationBackground));
        }

        private static string GetUsageStatus(
            double usage)
        {
            if (usage >= 85)
            {
                return T(
                    "DashboardUsageCritical");
            }

            if (usage >= 60)
            {
                return T(
                    "DashboardUsageHigh");
            }

            return T(
                "DashboardUsageNormal");
        }

        private void LanguageManager_LanguageChanged(
    object? sender,
    EventArgs e)
        {
            CpuStatus =
                GetUsageStatus(
                    CpuUsageValue);

            if (!IsCpuTemperatureAvailable)
            {
                CpuTemperature =
                    T(
                        "DashboardCpuTemperatureUnavailable");
            }

            // Actualizează imediat textul pentru timpul
            // de funcționare după schimbarea limbii.
            Uptime =
                new UptimeService()
                    .GetWindowsUptime();

            // Actualizează textele localizate
            // din sumarul sistemului.
            UpdateSystemSummary();

            // Actualizează textele localizate
            // din analiza performanței.
            NotifyPerformanceAnalysisProperties();

            OnPropertyChanged(
                nameof(PerformanceRecommendationText));

            // Actualizează textele alertei active.
            OnPropertyChanged(
                nameof(PerformanceAlertTitle));

            OnPropertyChanged(
                nameof(PerformanceAlertSeverityText));

            OnPropertyChanged(
                nameof(PerformanceAlertMessage));

            OnPropertyChanged(
                nameof(PerformanceAlertDurationText));
        }

        public event PropertyChangedEventHandler?
            PropertyChanged;

        protected void OnPropertyChanged(
            [CallerMemberName]
            string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(
                    propertyName));
        }
    }
}
