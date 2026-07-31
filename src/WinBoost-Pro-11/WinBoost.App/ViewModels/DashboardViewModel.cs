using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Threading;
using WinBoost.App.Services.Monitoring;

namespace WinBoost.App.ViewModels
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        private readonly SystemMonitorService _systemMonitorService;
        private readonly DispatcherTimer _refreshTimer;

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

        private int _healthScore;
        private string _healthStatus = "Calculating...";

        public DashboardViewModel()
        {
            _systemMonitorService = new SystemMonitorService();

            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };

            _refreshTimer.Tick += RefreshTimer_Tick;

            StartMonitoring();
        }

        public double CpuUsageValue
        {
            get => _cpuUsageValue;

            set
            {
                if (Math.Abs(_cpuUsageValue - value) < 0.01)
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
                if (Math.Abs(_ramUsageValue - value) < 0.01)
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
                if (Math.Abs(_diskUsageValue - value) < 0.01)
                {
                    return;
                }

                _diskUsageValue = value;
                OnPropertyChanged();
            }
        }

        public int HealthScore
        {
            get => _healthScore;

            set
            {
                if (_healthScore == value)
                {
                    return;
                }

                _healthScore = value;
                OnPropertyChanged();
            }
        }

        public string HealthStatus
        {
            get => _healthStatus;

            set
            {
                if (_healthStatus == value)
                {
                    return;
                }

                _healthStatus = value;
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
                    await _systemMonitorService.GetSystemMetricsAsync();

                CpuUsageValue = metrics.CpuUsage;
                RamUsageValue = metrics.RamUsage;
                DiskUsageValue = metrics.DiskUsage;

                CpuStatus = GetUsageStatus(metrics.CpuUsage);

                CpuUsage = $"{metrics.CpuUsage:F1} %";
                RamUsage = $"{metrics.RamUsage:F0} %";

                RamDetails =
                    $"{metrics.UsedRamGB:F1} GB / " +
                    $"{metrics.TotalRamGB:F1} GB";

                DiskUsage = $"{metrics.DiskUsage:F0} %";
                Uptime = metrics.Uptime;

                UpdateHealthScore();
            }
            catch
            {
                // Aplicația rămâne funcțională dacă o citire eșuează.
            }
            finally
            {
                _isRefreshingSystemInfo = false;
            }
        }

        private void UpdateHealthScore()
        {
            var score = 100;

            if (CpuUsageValue > 80)
            {
                score -= 15;
            }
            else if (CpuUsageValue > 60)
            {
                score -= 8;
            }

            if (RamUsageValue > 90)
            {
                score -= 25;
            }
            else if (RamUsageValue > 75)
            {
                score -= 15;
            }
            else if (RamUsageValue > 60)
            {
                score -= 8;
            }

            if (DiskUsageValue > 90)
            {
                score -= 20;
            }
            else if (DiskUsageValue > 80)
            {
                score -= 10;
            }

            HealthScore = Math.Clamp(score, 0, 100);

            HealthStatus = HealthScore switch
            {
                >= 90 => "Excellent",
                >= 75 => "Good",
                >= 60 => "Needs attention",
                _ => "Critical"
            };
        }

        private static string GetUsageStatus(double usage)
        {
            if (usage >= 85)
            {
                return "Critic";
            }

            if (usage >= 60)
            {
                return "Ridicat";
            }

            return "Normal";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}