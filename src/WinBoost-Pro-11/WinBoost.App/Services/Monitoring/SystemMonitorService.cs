using System.Threading.Tasks;
using WinBoost.App.Models;

namespace WinBoost.App.Services.Monitoring
{
    public class SystemMonitorService
    {
        private readonly CpuMonitorService
            _cpuMonitorService;

        private readonly MemoryMonitorService
            _memoryMonitorService;

        private readonly DiskMonitorService
            _diskMonitorService;

        private readonly UptimeService
            _uptimeService;

        private readonly CpuTemperatureMonitorService
            _cpuTemperatureMonitorService;

        public SystemMonitorService()
        {
            _cpuMonitorService =
                new CpuMonitorService();

            _memoryMonitorService =
                new MemoryMonitorService();

            _diskMonitorService =
                new DiskMonitorService();

            _uptimeService =
                new UptimeService();

            _cpuTemperatureMonitorService =
                new CpuTemperatureMonitorService();
        }

        public async Task<SystemMetrics>
            GetSystemMetricsAsync()
        {
            float cpuUsage =
                await GetSystemCpuUsageAsync();

            float ramUsage =
                _memoryMonitorService.GetRamUsage();

            var ramInfo =
                _memoryMonitorService.GetRamInfo();

            float diskUsage =
                _diskMonitorService.GetDiskUsage();

            string uptime =
                _uptimeService.GetWindowsUptime();

            CpuTemperatureInfo cpuTemperature =
                _cpuTemperatureMonitorService
                    .GetCpuTemperature();

            return new SystemMetrics
            {
                CpuUsage = cpuUsage,
                RamUsage = ramUsage,
                UsedRamGB = ramInfo.UsedGB,
                TotalRamGB = ramInfo.TotalGB,
                DiskUsage = diskUsage,
                Uptime = uptime,
                CpuTemperature = cpuTemperature
            };
        }

        public async Task<float>
            GetSystemCpuUsageAsync()
        {
            return await _cpuMonitorService
                .GetCpuUsageAsync();
        }
    }
}