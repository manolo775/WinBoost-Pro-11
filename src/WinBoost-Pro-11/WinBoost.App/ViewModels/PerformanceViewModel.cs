using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
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
        private readonly TempFileService _tempFileService;
        private readonly DispatcherTimer _processRefreshTimer;

        private bool _isRefreshingProcesses;
        private bool _isAnalyzing;
        private bool _isApplyingOptimizations;

        private string _optimizationStatus =
            "Apasă Analyze System pentru verificarea sistemului.";

        public ObservableCollection<ProcessInfo> TopProcesses { get; }

        public ObservableCollection<OptimizationRecommendation>
            Recommendations
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
                ? "Analyzing..."
                : "Analyze System";

        public string ApplyButtonText =>
            IsApplyingOptimizations
                ? "Se optimizează..."
                : "Apply Selected";

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

            OptimizeCommand =
                new RelayCommand(
                    async _ => await AnalyzeSystemAsync(),
                    _ => !IsAnalyzing &&
                         !IsApplyingOptimizations);

            ApplyOptimizationsCommand =
                new RelayCommand(
                    async _ => await ApplyOptimizationsAsync(),
                    _ => !IsAnalyzing &&
                         !IsApplyingOptimizations);

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
            OptimizationStatus = "Analizez sistemul...";
            Recommendations.Clear();

            try
            {
                var messages = new List<string>();

                var tempResult =
                    await _tempFileService.AnalyzeAsync();

                if (tempResult.FileCount > 0)
                {
                    string space =
                        FormatBytes(tempResult.TotalBytes);

                    Recommendations.Add(
                        new OptimizationRecommendation
                        {
                            Id = "temp-cleanup",
                            Title =
                                "Curățare fișiere temporare",
                            Description =
                                $"{tempResult.FileCount} fișiere pot elibera aproximativ {space}.",
                            RequiresAdministrator = false,
                            IsSelected = true
                        });

                    messages.Add(
                        $"se pot curăța aproximativ {space} de fișiere temporare");
                }

                if (CpuUsageValue >= 80)
                {
                    messages.Add(
                        "utilizarea procesorului este ridicată");
                }

                if (RamUsageValue >= 85)
                {
                    messages.Add(
                        "utilizarea memoriei RAM este ridicată");
                }

                if (DiskUsageValue >= 90)
                {
                    messages.Add(
                        "spațiul disponibil pe disc este redus");
                }

                OptimizationStatus = messages.Count == 0
                    ? "Analiză finalizată: sistemul funcționează normal."
                    : "Recomandări: " +
                      string.Join(" • ", messages);
            }
            catch (Exception ex)
            {
                OptimizationStatus =
                    $"Analiza nu a putut fi finalizată: {ex.Message}";
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
                    .Where(item => item.IsSelected)
                    .ToList();

            if (selectedItems.Count == 0)
            {
                OptimizationStatus =
                    "Selectează cel puțin o recomandare.";

                return;
            }

            MessageBoxResult confirmation =
                MessageBox.Show(
                    "Vor fi șterse numai fișierele temporare mai vechi de 24 de ore.\n\nContinui?",
                    "Confirmare optimizare",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

            if (confirmation != MessageBoxResult.Yes)
                return;

            IsApplyingOptimizations = true;
            OptimizationStatus = "Se aplică optimizările selectate...";

            try
            {
                int deletedFiles = 0;
                long freedBytes = 0;

                foreach (OptimizationRecommendation item
                         in selectedItems)
                {
                    if (item.Id != "temp-cleanup")
                        continue;

                    var result =
                        await _tempFileService.CleanAsync();

                    deletedFiles += result.DeletedFiles;
                    freedBytes += result.FreedBytes;
                }

                Recommendations.Clear();

                OptimizationStatus =
                    deletedFiles > 0
                        ? $"Curățare finalizată: {deletedFiles} fișiere șterse, {FormatBytes(freedBytes)} eliberați."
                        : "Nu au fost găsite fișiere care să poată fi șterse.";
            }
            catch (Exception ex)
            {
                OptimizationStatus =
                    $"Optimizarea nu a putut fi finalizată: {ex.Message}";
            }
            finally
            {
                IsApplyingOptimizations = false;
            }
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