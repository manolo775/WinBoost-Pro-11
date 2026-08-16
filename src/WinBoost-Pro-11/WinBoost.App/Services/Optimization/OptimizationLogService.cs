using System;
using System.Collections.ObjectModel;
using WinBoost.App.Models;

namespace WinBoost.App.Services.Optimization
{
    public sealed class OptimizationLogService
    {
        private static readonly Lazy<OptimizationLogService>
            LazyInstance =
                new(() =>
                    new OptimizationLogService());

        private readonly ObservableCollection<
            OptimizationLogEntry>
            _entries =
                new();

        private readonly ReadOnlyObservableCollection<
            OptimizationLogEntry>
            _readOnlyEntries;

        private OptimizationLogService()
        {
            _readOnlyEntries =
                new ReadOnlyObservableCollection<
                    OptimizationLogEntry>(
                        _entries);
        }

        public static OptimizationLogService Instance =>
            LazyInstance.Value;

        public ReadOnlyObservableCollection<
            OptimizationLogEntry>
            Entries =>
                _readOnlyEntries;

        public void Add(
            string message,
            OptimizationLogLevel level =
                OptimizationLogLevel.Information)
        {
            _entries.Add(
                new OptimizationLogEntry
                {
                    Timestamp =
                        DateTime.Now,

                    Message =
                        message,

                    Level =
                        level
                });
        }

        public void AddResource(
            string resourceKey,
            OptimizationLogLevel level =
                OptimizationLogLevel.Information,
            params object[] resourceArguments)
        {
            _entries.Add(
                new OptimizationLogEntry
                {
                    Timestamp =
                        DateTime.Now,

                    ResourceKey =
                        resourceKey,

                    ResourceArguments =
                        resourceArguments,

                    Level =
                        level
                });
        }

        public void AddResource(
            string resourceKey,
            string argumentResourceKey,
            OptimizationLogLevel level)
        {
            _entries.Add(
                new OptimizationLogEntry
                {
                    Timestamp =
                        DateTime.Now,

                    ResourceKey =
                        resourceKey,

                    ArgumentResourceKey =
                        argumentResourceKey,

                    Level =
                        level
                });
        }

        public void Clear()
        {
            _entries.Clear();
        }
    }
}