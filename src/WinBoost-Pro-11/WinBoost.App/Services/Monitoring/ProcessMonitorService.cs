using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using WinBoost.App.Models;

namespace WinBoost.App.Services.Monitoring
{
    public sealed class ProcessMonitorService
    {
        public Task<List<ProcessInfo>>
            GetTopProcessesAsync(
                int numberOfProcesses = 5,
                ProcessSortMode sortMode =
                    ProcessSortMode.Cpu)
        {
            return Task.Run(
                async () =>
                {
                    var samples =
                        new List<ProcessSample>();

                    foreach (Process process
                             in Process.GetProcesses())
                    {
                        try
                        {
                            samples.Add(
                                new ProcessSample(
                                    process,
                                    process
                                        .TotalProcessorTime));
                        }
                        catch
                        {
                            process.Dispose();
                        }
                    }

                    Stopwatch stopwatch =
                        Stopwatch.StartNew();

                    await Task.Delay(1000)
                        .ConfigureAwait(false);

                    stopwatch.Stop();

                    var results =
                        new List<ProcessInfo>();

                    foreach (ProcessSample sample
                             in samples)
                    {
                        try
                        {
                            sample.Process.Refresh();

                            TimeSpan cpuDifference =
                                sample.Process
                                    .TotalProcessorTime -
                                sample.InitialCpuTime;

                            double cpuUsage =
                                cpuDifference
                                    .TotalMilliseconds /
                                (stopwatch
                                    .Elapsed
                                    .TotalMilliseconds *
                                 Environment
                                    .ProcessorCount) *
                                100;

                            cpuUsage =
                                Math.Clamp(
                                    cpuUsage,
                                    0,
                                    100);

                            double memoryUsageMb = 0;

                            if (sortMode ==
                                ProcessSortMode.Memory)
                            {
                                memoryUsageMb =
                                    sample.Process
                                        .WorkingSet64 /
                                    1024d /
                                    1024d;
                            }

                            results.Add(
                                new ProcessInfo
                                {
                                    ProcessId =
                                        sample.Process.Id,

                                    Name =
                                        sample.Process.ProcessName,

                                    CpuUsage =
                                        cpuUsage,

                                    MemoryUsageMb =
                                        memoryUsageMb,

                                    ExecutablePath =
                                        string.Empty
                                });
                        }
                        catch
                        {
                            // Procesul s-a închis sau accesul
                            // la datele sale este restricționat.
                        }
                        finally
                        {
                            sample.Process.Dispose();
                        }
                    }

                    List<ProcessInfo> topProcesses =
                        sortMode ==
                        ProcessSortMode.Memory
                            ? results
                                .OrderByDescending(
                                    process =>
                                        process.MemoryUsageMb)
                                .ThenByDescending(
                                    process =>
                                        process.CpuUsage)
                                .Take(numberOfProcesses)
                                .ToList()
                            : results
                                .OrderByDescending(
                                    process =>
                                        process.CpuUsage)
                                .Take(numberOfProcesses)
                                .ToList();

                    foreach (ProcessInfo processInfo
                             in topProcesses)
                    {
                        PopulateProcessDetails(
                            processInfo);
                    }

                    return topProcesses;
                });
        }

        private static void PopulateProcessDetails(
            ProcessInfo processInfo)
        {
            try
            {
                using Process process =
                    Process.GetProcessById(
                        processInfo.ProcessId);

                if (processInfo.MemoryUsageMb <= 0)
                {
                    processInfo.MemoryUsageMb =
                        process.WorkingSet64 /
                        1024d /
                        1024d;
                }

                processInfo.ExecutablePath =
                    GetExecutablePath(process);
            }
            catch
            {
                // Procesul poate fi protejat sau se poate
                // închide înainte de citirea detaliilor.
            }
        }

        private static string GetExecutablePath(
            Process process)
        {
            try
            {
                return process.MainModule?.FileName
                    ?? string.Empty;
            }
            catch
            {
                // Procesele protejate nu permit accesul
                // la locația executabilului.
                return string.Empty;
            }
        }

        private sealed record ProcessSample(
            Process Process,
            TimeSpan InitialCpuTime);
    }
}