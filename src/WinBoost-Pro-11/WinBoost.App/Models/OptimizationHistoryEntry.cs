using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using WinBoost.App.Localization;

namespace WinBoost.App.Models
{
    public sealed class OptimizationHistoryEntry
        : INotifyPropertyChanged
    {
        public DateTime CompletedAt
        {
            get;
            set;
        }

        public bool IsSuccessful
        {
            get;
            set;
        }

        public long DeletedFiles
        {
            get;
            set;
        }

        public long RecoveredBytes
        {
            get;
            set;
        }

        public double DurationSeconds
        {
            get;
            set;
        }

        public int SuccessfulOperations
        {
            get;
            set;
        }

        public int FailedOperations
        {
            get;
            set;
        }

        public int SkippedOperations
        {
            get;
            set;
        }

        public string CompletedAtText =>
            CompletedAt.ToString(
                "dd.MM.yyyy HH:mm:ss");

        public string StatusText =>
            IsSuccessful
                ? LocalizationHelper.Get(
                    "OptimizationHistoryStatusSuccess")
                : LocalizationHelper.Get(
                    "OptimizationHistoryStatusCompletedWithErrors");

        public string RecoveredSpaceText =>
            FormatBytes(
                RecoveredBytes);

        public string DeletedFilesText =>
            DeletedFiles.ToString(
                CultureInfo.CurrentCulture);

        public string DurationText =>
            LocalizationHelper.Format(
                "OptimizationHistoryDurationFormat",
                DurationSeconds);

        public string OperationsText =>
            LocalizationHelper.Format(
                "OptimizationHistoryOperationsFormat",
                SuccessfulOperations,
                FailedOperations,
                SkippedOperations);

        public void RefreshLocalization()
        {
            OnPropertyChanged(
                nameof(StatusText));

            OnPropertyChanged(
                nameof(RecoveredSpaceText));

            OnPropertyChanged(
                nameof(DeletedFilesText));

            OnPropertyChanged(
                nameof(DurationText));

            OnPropertyChanged(
                nameof(OperationsText));
        }

        private static string FormatBytes(
            long bytes)
        {
            string[] units =
            {
                "B",
                "KB",
                "MB",
                "GB",
                "TB"
            };

            double value =
                Math.Max(
                    0,
                    bytes);

            int unitIndex = 0;

            while (value >= 1024 &&
                   unitIndex < units.Length - 1)
            {
                value /= 1024;

                unitIndex++;
            }

            return string.Format(
                CultureInfo.CurrentCulture,
                "{0:F2} {1}",
                value,
                units[unitIndex]);
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