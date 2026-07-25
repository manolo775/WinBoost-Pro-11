using System;
using System.IO;
using System.Threading.Tasks;
using WinBoost.App.Models;

namespace WinBoost.App.Services
{
    public class SystemMonitorService
    {
        private readonly CpuMonitorService _cpuMonitorService;
        private readonly MemoryMonitorService _memoryMonitorService;


        public SystemMonitorService()
        {
            _cpuMonitorService = new CpuMonitorService();
            _memoryMonitorService = new MemoryMonitorService();
        }



        public async Task<SystemMetrics> GetSystemMetricsAsync()
        {
            float cpuUsage = await GetSystemCpuUsageAsync();
            float ramUsage = _memoryMonitorService.GetRamUsage();
            var ramInfo = _memoryMonitorService.GetRamInfo();
            float diskUsage = GetDiskUsage();
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

        public float GetDiskUsage()
        {
            string systemDrive =
                Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";

            var drive = new DriveInfo(systemDrive);

            if (!drive.IsReady || drive.TotalSize == 0)
            {
                return 0;
            }

            long usedSpace =
                drive.TotalSize - drive.AvailableFreeSpace;

            double usage =
                (double)usedSpace / drive.TotalSize * 100;

            return (float)usage;
        }
    }
}