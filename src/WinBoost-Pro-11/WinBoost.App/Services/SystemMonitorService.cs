using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using WinBoost.App.Models;
namespace WinBoost.App.Services
{
    public class SystemMonitorService
    {
        private readonly PerformanceCounter _cpuCounter =
            new PerformanceCounter(
                "Processor",
                "% Processor Time",
                "_Total");

        [StructLayout(LayoutKind.Sequential)]
        private class MemoryStatusEx
        {
            public uint Length;
            public uint MemoryLoad;
            public ulong TotalPhysicalMemory;
            public ulong AvailablePhysicalMemory;
            public ulong TotalPageFile;
            public ulong AvailablePageFile;
            public ulong TotalVirtualMemory;
            public ulong AvailableVirtualMemory;
            public ulong AvailableExtendedVirtualMemory;

            public MemoryStatusEx()
            {
                Length = (uint)Marshal.SizeOf(typeof(MemoryStatusEx));
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx(
            [In, Out] MemoryStatusEx memoryStatus);

        public async Task<SystemMetrics> GetSystemMetricsAsync()
        {
            float cpuUsage = await GetSystemCpuUsageAsync();
            float ramUsage = GetRamUsage();
            var ramInfo = GetRamInfo();
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

        public async Task<float> GetCpuUsageAsync()
        {
            using var process = Process.GetCurrentProcess();

            var startTime = DateTime.UtcNow;
            var startCpuUsage = process.TotalProcessorTime;

            await Task.Delay(500);

            var endTime = DateTime.UtcNow;
            var endCpuUsage = process.TotalProcessorTime;

            var cpuUsedMilliseconds =
                (endCpuUsage - startCpuUsage).TotalMilliseconds;

            var totalMilliseconds =
                (endTime - startTime).TotalMilliseconds;

            var cpuUsage =
                cpuUsedMilliseconds /
                (Environment.ProcessorCount * totalMilliseconds) *
                100;

            return (float)cpuUsage;
        }

        public async Task<float> GetSystemCpuUsageAsync()
        {
            _cpuCounter.NextValue();

            await Task.Delay(500);

            float cpuUsage = _cpuCounter.NextValue();

            return Math.Clamp(cpuUsage, 0f, 100f);
        }

        public float GetRamUsage()
        {
            var memoryStatus = new MemoryStatusEx();

            bool success = GlobalMemoryStatusEx(memoryStatus);

            if (!success)
            {
                return 0;
            }

            return memoryStatus.MemoryLoad;
        }

        public (double UsedGB, double TotalGB) GetRamInfo()
        {
            var memoryStatus = new MemoryStatusEx();

            bool success = GlobalMemoryStatusEx(memoryStatus);

            if (!success)
            {
                return (0, 0);
            }

            double total =
                memoryStatus.TotalPhysicalMemory /
                1024d / 1024d / 1024d;

            double available =
                memoryStatus.AvailablePhysicalMemory /
                1024d / 1024d / 1024d;

            double used = total - available;

            return (used, total);
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