using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using WinBoost.App.Models;

namespace WinBoost.App.Services
{
    public sealed class ProcessMonitorService
    {
        public Task<List<ProcessInfo>> GetTopProcessesAsync(
            int numberOfProcesses = 5)
        {
            return Task.Run(async () =>
            {
                var samples = new List<ProcessSample>();

                foreach (Process process in Process.GetProcesses())
                {
                    try
                    {
                        samples.Add(
                            new ProcessSample(
                                process,
                                process.TotalProcessorTime));
                    }
                    catch
                    {
                        process.Dispose();
                    }
                }

                Stopwatch stopwatch = Stopwatch.StartNew();

                await Task.Delay(1000).ConfigureAwait(false);

                stopwatch.Stop();

                var results = new List<ProcessInfo>();

                foreach (ProcessSample sample in samples)
                {
                    try
                    {
                        sample.Process.Refresh();

                        TimeSpan cpuDifference =
                            sample.Process.TotalProcessorTime -
                            sample.InitialCpuTime;

                        double cpuUsage =
                            cpuDifference.TotalMilliseconds /
                            (stopwatch.Elapsed.TotalMilliseconds *
                             Environment.ProcessorCount) *
                            100;

                        cpuUsage =
                            Math.Clamp(cpuUsage, 0, 100);

                        double memoryUsageMb =
                            sample.Process.WorkingSet64 /
                            1024d /
                            1024d;

                        results.Add(new ProcessInfo
                        {
                            ProcessId = sample.Process.Id,
                            Name = sample.Process.ProcessName,
                            CpuUsage = cpuUsage,
                            MemoryUsageMb = memoryUsageMb
                        });
                    }
                    catch
                    {
                        // Procesul s-a închis sau accesul este restricționat.
                    }
                    finally
                    {
                        sample.Process.Dispose();
                    }
                }

                return results
                    .OrderByDescending(process => process.CpuUsage)
                    .ThenByDescending(process => process.MemoryUsageMb)
                    .Take(numberOfProcesses)
                    .ToList();
            });
        }

        private sealed record ProcessSample(
            Process Process,
            TimeSpan InitialCpuTime);
    }
}