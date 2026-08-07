using System;
using System.Collections.Generic;
using System.Linq;
using WinBoost.App.Models;

namespace WinBoost.App.Services.Monitoring
{
    public sealed class SystemMetricsHistoryService
    {
        private const int MaximumHistoryPoints = 150;

        private readonly object _syncRoot = new();

        private readonly Queue<SystemMetricsHistoryPoint>
            _historyPoints = new();

        public void Add(
            double cpuUsage,
            double ramUsage,
            double diskUsage)
        {
            var point = new SystemMetricsHistoryPoint
            {
                Timestamp = DateTime.Now,
                CpuUsage = cpuUsage,
                RamUsage = ramUsage,
                DiskUsage = diskUsage
            };

            lock (_syncRoot)
            {
                _historyPoints.Enqueue(point);

                while (_historyPoints.Count >
                       MaximumHistoryPoints)
                {
                    _historyPoints.Dequeue();
                }
            }
        }

        public IReadOnlyList<SystemMetricsHistoryPoint>
            GetSnapshot()
        {
            lock (_syncRoot)
            {
                return _historyPoints
                    .ToList();
            }
        }

        public void Clear()
        {
            lock (_syncRoot)
            {
                _historyPoints.Clear();
            }
        }
    }
}