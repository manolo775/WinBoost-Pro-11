using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace WinBoost.App.Models
{
    public sealed class OptimizationSummary :
        INotifyPropertyChanged
    {
        public bool IsVisible
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

        public int TotalOperations =>
            SuccessfulOperations +
            FailedOperations +
            SkippedOperations;

        public string RecoveredSpaceText =>
            FormatBytes(
                RecoveredBytes);

        public string DeletedFilesText =>
            DeletedFiles.ToString(
                CultureInfo.CurrentCulture);

        public string DurationText =>
            string.Format(
                CultureInfo.CurrentCulture,
                "{0:F1} sec",
                DurationSeconds);

        public string OperationsText =>
            string.Format(
                CultureInfo.CurrentCulture,
                "{0}/{1}",
                SuccessfulOperations,
                TotalOperations);

        public void RefreshLocalization()
        {
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
                System.Math.Max(
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