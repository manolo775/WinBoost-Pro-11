using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace WinBoost.App.Services
{
    public sealed class CpuMonitorService
    {
        private readonly PerformanceCounter _cpuCounter =
            new PerformanceCounter(
                "Processor",
                "% Processor Time",
                "_Total");

        public async Task<float> GetCpuUsageAsync()
        {
            _cpuCounter.NextValue();

            await Task.Delay(500);

            float cpuUsage = _cpuCounter.NextValue();

            return Math.Clamp(cpuUsage, 0f, 100f);
        }
    }
}