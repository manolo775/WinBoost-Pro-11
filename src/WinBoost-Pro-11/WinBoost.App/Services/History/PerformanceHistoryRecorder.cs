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
                TimeSpan.FromDays(14);

        private static readonly SemaphoreSlim
            DatabaseSemaphore =
                new(1, 1);

        private static DateTime
            _lastRecordedUtc =
                DateTime.MinValue;

        private readonly PerformanceHistoryDatabase
            _database;

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

            await DatabaseSemaphore.WaitAsync();

            try
            {
                nowUtc =
                    DateTime.UtcNow;

                if (nowUtc - _lastRecordedUtc <
                    RecordingInterval)
                {
                    return;
                }

                var record =
                    new PerformanceHistoryRecord
                    {
                        Timestamp =
                            nowUtc,

                        CpuUsage =
                            cpuUsage,

                        RamUsage =
                            ramUsage,

                        DiskUsage =
                            diskUsage,

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

                _lastRecordedUtc =
                    nowUtc;
            }
            finally
            {
                DatabaseSemaphore.Release();
            }
        }

        public async Task<
            IReadOnlyList<PerformanceHistoryRecord>>
            GetRecordsAsync(
                DateTime from,
                DateTime to)
        {
            await DatabaseSemaphore.WaitAsync();

            try
            {
                return await Task.Run(() =>
                    _database.GetRecords(
                        from,
                        to));
            }
            finally
            {
                DatabaseSemaphore.Release();
            }
        }

        public async Task ClearHistoryAsync()
        {
            await DatabaseSemaphore.WaitAsync();

            try
            {
                await Task.Run(() =>
                    _database.DeleteAll());

                _lastRecordedUtc =
                    DateTime.MinValue;
            }
            finally
            {
                DatabaseSemaphore.Release();
            }
        }
    }
}