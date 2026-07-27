using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using WinBoost.App.Commands;
using WinBoost.App.Models;
using WinBoost.App.Services;

namespace WinBoost.App.ViewModels
{
    public class PerformanceViewModel : DashboardViewModel
    {
        private readonly ProcessMonitorService _processMonitorService;
        private readonly DispatcherTimer _processRefreshTimer;

        private bool _isRefreshingProcesses;
        private bool _isAnalyzing;

        private string _optimizationStatus =
            "Apasă Analyze System pentru verificarea sistemului.";

        public ObservableCollection<ProcessInfo> TopProcesses { get; }

        public ICommand OptimizeCommand { get; }

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

        public string OptimizationButtonText =>
            IsAnalyzing
                ? "Analyzing..."
                : "Analyze System";

        public PerformanceViewModel()
        {
            _processMonitorService =
                new ProcessMonitorService();

            TopProcesses =
                new ObservableCollection<ProcessInfo>();

            OptimizeCommand =
                new RelayCommand(
                    async _ => await AnalyzeSystemAsync(),
                    _ => !IsAnalyzing);

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
            catch
            {
                // Păstrăm interfața funcțională dacă un proces
                // nu poate fi citit temporar.
            }
            finally
            {
                _isRefreshingProcesses = false;
            }
        }

        private async Task AnalyzeSystemAsync()
        {
            if (IsAnalyzing)
                return;

            try
            {
                IsAnalyzing = true;
                OptimizationStatus = "Analizez sistemul...";

                await Task.Delay(1200);

                var recommendations =
                    new List<string>();

                if (CpuUsageValue >= 80)
                {
                    recommendations.Add(
                        "Utilizarea procesorului este ridicată");
                }

                if (RamUsageValue >= 85)
                {
                    recommendations.Add(
                        "Utilizarea memoriei RAM este ridicată");
                }

                if (DiskUsageValue >= 90)
                {
                    recommendations.Add(
                        "Spațiul disponibil pe disc este redus");
                }

                if (recommendations.Count == 0)
                {
                    OptimizationStatus =
                        "Analiză finalizată: sistemul funcționează normal.";
                }
                else
                {
                    OptimizationStatus =
                        "Recomandări: " +
                        string.Join(" • ", recommendations);
                }
            }
            catch (Exception exception)
            {
                OptimizationStatus =
                    $"Analiza nu a putut fi finalizată: {exception.Message}";
            }
            finally
            {
                IsAnalyzing = false;
            }
        }
    }
}