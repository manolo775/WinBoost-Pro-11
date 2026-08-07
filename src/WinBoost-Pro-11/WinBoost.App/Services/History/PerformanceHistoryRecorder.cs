using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WinBoost.App.Models;

namespace WinBoost.App.Services.History
{
    public sealed class PerformanceHistoryRecorder
    {
        private static readonly TimeSpan
            RecordingInterval =
            TimeSpan.FromMinutes(1);

        private static readonly TimeSpan
            RetentionPeriod =
            TimeSpan.FromDays(7);

        private readonly PerformanceHistoryDatabase
            _database;

        private readonly SemaphoreSlim _semaphore =
            new(1, 1);

        private DateTime _lastRecordedUtc =
            DateTime.MinValue;

        public PerformanceHistoryRecorder()
        {
            _database =
                new PerformanceHistoryDatabase();
        }

        public async Task RecordIfDueAsync(
            double cpuUsage,
            double ramUsage,
            double diskUsage,
            CpuTemperatureInfo cpuTemperature)
        {
            DateTime nowUtc =
                DateTime.UtcNow;

            if (nowUtc - _lastRecordedUtc <
                RecordingInterval)
            {
                return;
            }

            await _semaphore.WaitAsync();

            try
            {
                nowUtc = DateTime.UtcNow;

                if (nowUtc - _lastRecordedUtc <
                    RecordingInterval)
                {
                    return;
                }

                _lastRecordedUtc = nowUtc;

                var record =
                    new PerformanceHistoryRecord
                    {
                        Timestamp = nowUtc,
                        CpuUsage = cpuUsage,
                        RamUsage = ramUsage,
                        DiskUsage = diskUsage,

                        CpuTemperature =
                            cpuTemperature.IsAvailable
                                ? cpuTemperature.Celsius
                                : null
                    };

                await Task.Run(() =>
                {
                    _database.Save(record);

                    _database.DeleteOlderThan(
                        nowUtc.Subtract(
                            RetentionPeriod));
                });
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public Task<
            IReadOnlyList<PerformanceHistoryRecord>>
            GetRecordsAsync(
                DateTime from,
                DateTime to)
        {
            return Task.Run(() =>
                _database.GetRecords(
                    from,
                    to));
        }

        public async Task ClearHistoryAsync()
        {
            await Task.Run(() =>
                _database.DeleteAll());

            _lastRecordedUtc =
                DateTime.MinValue;
        }
    }
}