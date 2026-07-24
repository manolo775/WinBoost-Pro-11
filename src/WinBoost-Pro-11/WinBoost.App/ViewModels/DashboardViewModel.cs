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

        public DashboardViewModel()
        {
            _systemMonitorService = new SystemMonitorService();

            _ = UpdateCpuUsageAsync();
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

        private async Task UpdateCpuUsageAsync()
        {
            float cpuUsage =
                await _systemMonitorService.GetCpuUsageAsync();

            CpuUsage = $"{cpuUsage:F1} %";
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