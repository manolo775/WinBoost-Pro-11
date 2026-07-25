using System;
using System.Threading.Tasks;
using WinBoost.App.Models;

namespace WinBoost.App.Services
{
    public class SystemMonitorService
    {
        private readonly CpuMonitorService _cpuMonitorService;
        private readonly MemoryMonitorService _memoryMonitorService;
        private readonly DiskMonitorService _diskMonitorService;

        public SystemMonitorService()
        {
            _cpuMonitorService = new CpuMonitorService();
            _memoryMonitorService = new MemoryMonitorService();
            _diskMonitorService = new DiskMonitorService();
        }



        public async Task<SystemMetrics> GetSystemMetricsAsync()
        {
            float cpuUsage = await GetSystemCpuUsageAsync();
            float ramUsage = _memoryMonitorService.GetRamUsage();
            var ramInfo = _memoryMonitorService.GetRamInfo();
            float diskUsage = _diskMonitorService.GetDiskUsage();
            string uptime = GetWindowsUptime();

            return new SystemMetrics
            {
                CpuUsage = cpuUsage,
                RamUsage = ramUsage,
                UsedRamGB = ramInfo.UsedGB,
                TotalRamGB = ramInfo.TotalGB,
                DiskUsage = diskUsage,
                Uptime = uptime
            };
        }

        

        public async Task<float> GetSystemCpuUsageAsync()
        {
            return await _cpuMonitorService.GetCpuUsageAsync();
        }

       



        public string GetWindowsUptime()
        {
            TimeSpan uptime =
                TimeSpan.FromMilliseconds(Environment.TickCount64);

            if (uptime.Days > 0)
            {
                return $"{uptime.Days} zile {uptime.Hours} ore";
            }

            return $"{uptime.Hours} ore {uptime.Minutes} min";
        }

    }
}