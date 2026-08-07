using MaterialDesignThemes.Wpf;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;
using WinBoost.App.Models;
using WinBoost.App.Services.Health;
using WinBoost.App.Services.Monitoring;
using System.Collections.Generic;

using WinBoost.App.Localization;


namespace WinBoost.App.ViewModels
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        private readonly SystemMonitorService
            _systemMonitorService;

        private readonly SystemMetricsHistoryService
            _metricsHistoryService;

        private readonly SystemHealthCalculator
            _systemHealthCalculator;

        private readonly SystemHealthStateService
            _healthStateService;

        private readonly DispatcherTimer
            _refreshTimer;

        private bool _isRefreshingSystemInfo;

        private string _cpuUsage = "0 %";
        private string _ramUsage = "0 %";
        private string _ramDetails = "-- GB / -- GB";
        private string _diskUsage = "-- %";
        private string _uptime = "--";
        private string _cpuStatus = "Normal";

        private double _cpuUsageValue;
        private double _ramUsageValue;
        private double _diskUsageValue;

        public DashboardViewModel()
        {
            _systemMonitorService =
                new SystemMonitorService();

            _metricsHistoryService =
                 new SystemMetricsHistoryService();

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
                        TimeSpan.FromSeconds(2)
                };

            _refreshTimer.Tick +=
                RefreshTimer_Tick;

            StartMonitoring();
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

        public IReadOnlyList<SystemMetricsHistoryPoint>
             MetricsHistory =>
             _metricsHistoryService.GetSnapshot();

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



        public void StartMonitoring()
        {
            if (_refreshTimer.IsEnabled)
            {
                return;
            }

            _refreshTimer.Start();

            _ = UpdateSystemInfoAsync();
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

                OnPropertyChanged(nameof(MetricsHistory));

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
            OnPropertyChanged(nameof(RecommendationBackground));
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

            UpdateSystemSummary();
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