namespace WinBoost.App.Models
{
    public sealed class SystemMetrics
    {
        public float CpuUsage { get; set; }

        public float RamUsage { get; set; }

        public double UsedRamGB { get; set; }

        public double TotalRamGB { get; set; }

        public float DiskUsage { get; set; }

        public string Uptime { get; set; } = string.Empty;

        public CpuTemperatureInfo CpuTemperature { get; set; } =
          new CpuTemperatureInfo();
    }
}
