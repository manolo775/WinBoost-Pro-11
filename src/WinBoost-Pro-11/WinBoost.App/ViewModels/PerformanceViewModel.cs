using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Threading;
using WinBoost.App.Models;
using WinBoost.App.Services;

namespace WinBoost.App.ViewModels
{
    public class PerformanceViewModel : DashboardViewModel
    {
        private readonly ProcessMonitorService _processMonitorService;
        private readonly DispatcherTimer _processRefreshTimer;
        private bool _isRefreshingProcesses;

        public ObservableCollection<ProcessInfo> TopProcesses { get; }

        public PerformanceViewModel()
        {
            _processMonitorService =
                new ProcessMonitorService();

            TopProcesses =
                new ObservableCollection<ProcessInfo>();

            _processRefreshTimer =
                new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(5)
                };

            _processRefreshTimer.Tick +=
                ProcessRefreshTimer_Tick;

            _processRefreshTimer.Start();

            _ = RefreshTopProcessesAsync();
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
            finally
            {
                _isRefreshingProcesses = false;
            }
        }
    }
}