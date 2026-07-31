namespace WinBoost.App.Models
{
    public class OptimizationResult
    {
        public bool IsSuccessful { get; set; }

        public long DeletedFilesCount { get; set; }

        public long RecoveredBytes { get; set; }

        public string Message { get; set; } =
            string.Empty;

        public string RecoveredSpaceText =>
            FormatBytes(RecoveredBytes);

        private static string FormatBytes(long bytes)
        {
            string[] units =
            {
                "B",
                "KB",
                "MB",
                "GB",
                "TB"
            };

            double value = bytes;
            int unitIndex = 0;

            while (value >= 1024 &&
                   unitIndex < units.Length - 1)
            {
                value /= 1024;
                unitIndex++;
            }

            return $"{value:F2} {units[unitIndex]}";
        }
    }
}