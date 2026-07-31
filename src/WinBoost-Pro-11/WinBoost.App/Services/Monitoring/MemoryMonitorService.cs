using System;
using System.Runtime.InteropServices;

namespace WinBoost.App.Services.Monitoring
{
    public sealed class MemoryMonitorService
    {
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

        public float GetRamUsage()
        {
            var memoryStatus = new MemoryStatusEx();

            if (!GlobalMemoryStatusEx(memoryStatus))
                return 0;

            return memoryStatus.MemoryLoad;
        }

        public (double UsedGB, double TotalGB) GetRamInfo()
        {
            var memoryStatus = new MemoryStatusEx();

            if (!GlobalMemoryStatusEx(memoryStatus))
                return (0, 0);

            double total =
                memoryStatus.TotalPhysicalMemory /
                1024d / 1024d / 1024d;

            double available =
                memoryStatus.AvailablePhysicalMemory /
                1024d / 1024d / 1024d;

            return (total - available, total);
        }
    }
}