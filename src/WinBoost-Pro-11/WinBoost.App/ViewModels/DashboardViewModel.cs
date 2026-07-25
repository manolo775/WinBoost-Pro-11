using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Threading;
using WinBoost.App.Services;

namespace WinBoost.App.ViewModels
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        private readonly SystemMonitorService _systemMonitorService;
        private readonly DispatcherTimer _refreshTimer;

        private string _cpuUsage = "0 %";
        private string _ramUsage = "0 %";
        private string _ramDetails = "-- GB / -- GB";
        private string _diskUsage = "-- %";
        private string _uptime = "--";

        public DashboardViewModel()
        {
            _systemMonitorService = new SystemMonitorService();

            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };

            _refreshTimer.Tick += async (_, _) =>
            {
                await UpdateSystemInfoAsync();
            };

            _refreshTimer.Start();

            _ = UpdateSystemInfoAsync();
        }

        public string CpuUsage
        {
            get => _cpuUsage;
            set
            {
                if (_cpuUsage == value)
                    return;

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
                    return;

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
                    return;

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
                    return;

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
                    return;

                _uptime = value;
                OnPropertyChanged();
            }
        }

        private async Task UpdateSystemInfoAsync()
        {
            var metrics =
                await _systemMonitorService.GetSystemMetricsAsync();

            CpuUsage = $"{metrics.CpuUsage:F1} %";
            RamUsage = $"{metrics.RamUsage:F0} %";

            RamDetails =
                $"{metrics.UsedRamGB:F1} GB / {metrics.TotalRamGB:F1} GB";

            DiskUsage = $"{metrics.DiskUsage:F0} %";
            Uptime = metrics.Uptime;
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