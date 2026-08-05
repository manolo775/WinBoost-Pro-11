using System.ComponentModel;
using System.Runtime.CompilerServices;
using WinBoost.App.Models;
using WinBoost.App.Services.Optimization;

namespace WinBoost.App.ViewModels
{
    public sealed class OptimizationSummaryViewModel :
        INotifyPropertyChanged
    {
        private OptimizationSummary
            _summary =
                new();

        public OptimizationSummary Summary
        {
            get => _summary;

            private set
            {
                _summary = value;
                OnPropertyChanged();
            }
        }

        public bool IsVisible =>
            Summary.IsVisible;

        public void Update(
            OptimizationReport report)
        {
            Summary =
                new OptimizationSummary
                {
                    IsVisible = true,

                    IsSuccessful =
                        report.IsSuccessful,

                    DeletedFiles =
                        report.TotalDeletedFiles,

                    RecoveredBytes =
                        report.TotalRecoveredBytes,

                    DurationSeconds =
                        report.Duration.TotalSeconds,

                    SuccessfulOperations =
                        report.SuccessfulOperations,

                    FailedOperations =
                        report.FailedOperations,

                    SkippedOperations =
                        report.SkippedOperations
                };

            OnPropertyChanged(
                nameof(IsVisible));
        }

        public void Clear()
        {
            Summary =
                new OptimizationSummary();

            OnPropertyChanged(
                nameof(IsVisible));
        }

        public event PropertyChangedEventHandler?
            PropertyChanged;

        private void OnPropertyChanged(
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