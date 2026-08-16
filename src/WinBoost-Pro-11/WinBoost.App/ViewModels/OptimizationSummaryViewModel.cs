using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WinBoost.App.Localization;
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

        public OptimizationSummaryViewModel()
        {
            LanguageManager.Instance.LanguageChanged +=
                LanguageManager_LanguageChanged;
        }

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

        private void LanguageManager_LanguageChanged(
            object? sender,
            EventArgs e)
        {
            Summary.RefreshLocalization();
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