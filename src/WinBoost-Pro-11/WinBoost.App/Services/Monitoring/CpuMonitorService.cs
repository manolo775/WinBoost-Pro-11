using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace WinBoost.App.Services.Monitoring
{
    public sealed class CpuMonitorService
    {
        private readonly PerformanceCounter _cpuCounter =
            new PerformanceCounter(
                "Processor",
                "% Processor Time",
                "_Total");

        public Task<float> GetCpuUsageAsync()
        {
            return Task.FromResult(0f);
        }
    }
}