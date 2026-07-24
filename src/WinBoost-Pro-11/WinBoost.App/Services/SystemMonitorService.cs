using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace WinBoost.App.Services
{
    public class SystemMonitorService
    {
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
    }
}