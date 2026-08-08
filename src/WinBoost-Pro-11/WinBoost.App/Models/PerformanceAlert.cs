using System;

namespace WinBoost.App.Models
{
    public enum PerformanceAlertType
    {
        CpuHigh,
        RamHigh,
        DiskHigh,
        CpuTemperatureHigh
    }

    public enum PerformanceAlertSeverity
    {
        Warning,
        Critical
    }

    public sealed class PerformanceAlert
    {
        public Guid Id { get; set; } =
            Guid.NewGuid();

        public PerformanceAlertType Type
        {
            get;
            set;
        }

        public PerformanceAlertSeverity Severity
        {
            get;
            set;
        }

        public DateTime CreatedAtUtc
        {
            get;
            set;
        }

        public double CurrentValue
        {
            get;
            set;
        }

        public double Threshold
        {
            get;
            set;
        }

        public TimeSpan SustainedDuration
        {
            get;
            set;
        }

        public bool IsAcknowledged
        {
            get;
            set;
        }
    }
}