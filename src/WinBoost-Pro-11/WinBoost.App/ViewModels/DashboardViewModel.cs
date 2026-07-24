using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WinBoost.App.Services;

namespace WinBoost.App.ViewModels
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        private readonly SystemMonitorService _systemMonitorService;

        private string _cpuUsage = "0 %";
        private string _ramUsage = "0 %";
        private string _ramDetails = "-- GB / -- GB";
        private string _diskUsage = "-- %";
        private string _uptime = "--";

        public DashboardViewModel()
        {
            _systemMonitorService = new SystemMonitorService();

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
            float cpuUsage =
                await _systemMonitorService.GetCpuUsageAsync();

            float ramUsage =
                _systemMonitorService.GetRamUsage();

            var ramInfo =
                _systemMonitorService.GetRamInfo();
            string uptime =
               _systemMonitorService.GetWindowsUptime();
            float diskUsage =
               _systemMonitorService.GetDiskUsage();

            CpuUsage = $"{cpuUsage:F1} %";
            RamUsage = $"{ramUsage:F0} %";
            RamDetails =
                $"{ramInfo.UsedGB:F1} GB / {ramInfo.TotalGB:F1} GB";
            Uptime = uptime;
            DiskUsage = $"{diskUsage:F0} %";
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