using System;

namespace WinBoost.App.Models
{
    public sealed class OptimizationHistoryEntry
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
                ? "Succes"
                : "Finalizat cu erori";

        public string RecoveredSpaceText =>
            FormatBytes(
                RecoveredBytes);

        public string DeletedFilesText =>
            DeletedFiles.ToString();

        public string DurationText =>
            $"{DurationSeconds:F1} sec";

        public string OperationsText =>
            $"{SuccessfulOperations} reușite, " +
            $"{FailedOperations} eșuate, " +
            $"{SkippedOperations} omise";

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

            int unitIndex =
                0;

            while (value >= 1024 &&
                   unitIndex < units.Length - 1)
            {
                value /=
                    1024;

                unitIndex++;
            }

            return
                $"{value:F2} {units[unitIndex]}";
        }
    }
}