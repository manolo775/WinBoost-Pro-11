namespace WinBoost.App.Models
{
    public enum PerformanceTrend
    {
        Unknown,
        Improving,
        Stable,
        Degrading
    }

    public sealed class PerformanceHistoryAnalysis
    {
        public bool HasEnoughData { get; set; }

        public int SampleCount { get; set; }

        public double AverageCpuUsage { get; set; }

        public double MinimumCpuUsage { get; set; }

        public double MaximumCpuUsage { get; set; }

        public double AverageRamUsage { get; set; }

        public double MinimumRamUsage { get; set; }

        public double MaximumRamUsage { get; set; }

        public double AverageDiskUsage { get; set; }

        public double MinimumDiskUsage { get; set; }

        public double MaximumDiskUsage { get; set; }

        public double? AverageCpuTemperature { get; set; }

        public double? MinimumCpuTemperature { get; set; }

        public double? MaximumCpuTemperature { get; set; }

        public double CpuChange { get; set; }

        public double RamChange { get; set; }

        public double DiskChange { get; set; }

        public PerformanceTrend CpuTrend { get; set; }

        public PerformanceTrend RamTrend { get; set; }

        public PerformanceTrend DiskTrend { get; set; }

        public PerformanceTrend OverallTrend { get; set; }
    }
}