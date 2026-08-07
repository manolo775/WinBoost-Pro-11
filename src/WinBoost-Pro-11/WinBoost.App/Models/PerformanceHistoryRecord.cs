using System;

namespace WinBoost.App.Models
{
    public sealed class PerformanceHistoryRecord
    {
        public DateTime Timestamp { get; init; }

        public double CpuUsage { get; init; }

        public double RamUsage { get; init; }

        public double DiskUsage { get; init; }

        public double? CpuTemperature { get; init; }
    }
}