namespace WinBoost.App.Models
{
    public sealed class ProcessInfo
    {
        public int ProcessId { get; set; }

        public string Name { get; set; } = string.Empty;

        public double CpuUsage { get; set; }

        public double MemoryUsageMb { get; set; }

        public string CpuUsageText =>
            $"{CpuUsage:F1} %";

        public string MemoryUsageText =>
            $"{MemoryUsageMb:F1} MB";
    }
}