using System;

namespace WinBoost.App.Models
{
    public sealed class SystemMetricsHistoryPoint
    {
        public DateTime Timestamp { get; init; }

        public double CpuUsage { get; init; }

        public double RamUsage { get; init; }

        public double DiskUsage { get; init; }

        public string TimeLabel =>
            Timestamp.ToString("HH:mm:ss");
    }
}