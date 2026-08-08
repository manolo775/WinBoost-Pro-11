using System;
using System.Collections.Generic;
using System.Linq;
using WinBoost.App.Models;

namespace WinBoost.App.Services.History
{
    public sealed class
        PerformanceHistoryAnalysisService
    {
        private const double TrendThreshold = 3.0;

        public PerformanceHistoryAnalysis Analyze(
            IReadOnlyList<PerformanceHistoryRecord>
                currentRecords,
            IReadOnlyList<PerformanceHistoryRecord>
                previousRecords)
        {
            if (currentRecords == null)
            {
                throw new ArgumentNullException(
                    nameof(currentRecords));
            }

            if (previousRecords == null)
            {
                throw new ArgumentNullException(
                    nameof(previousRecords));
            }

            if (currentRecords.Count == 0)
            {
                return new PerformanceHistoryAnalysis
                {
                    HasEnoughData = false,
                    SampleCount = 0,
                    CpuTrend = PerformanceTrend.Unknown,
                    RamTrend = PerformanceTrend.Unknown,
                    DiskTrend = PerformanceTrend.Unknown,
                    OverallTrend = PerformanceTrend.Unknown
                };
            }

            var analysis =
                new PerformanceHistoryAnalysis
                {
                    HasEnoughData =
                        currentRecords.Count >= 2,

                    SampleCount =
                        currentRecords.Count,

                    AverageCpuUsage =
                        currentRecords.Average(
                            record =>
                                record.CpuUsage),

                    MinimumCpuUsage =
                        currentRecords.Min(
                            record =>
                                record.CpuUsage),

                    MaximumCpuUsage =
                        currentRecords.Max(
                            record =>
                                record.CpuUsage),

                    AverageRamUsage =
                        currentRecords.Average(
                            record =>
                                record.RamUsage),

                    MinimumRamUsage =
                        currentRecords.Min(
                            record =>
                                record.RamUsage),

                    MaximumRamUsage =
                        currentRecords.Max(
                            record =>
                                record.RamUsage),

                    AverageDiskUsage =
                        currentRecords.Average(
                            record =>
                                record.DiskUsage),

                    MinimumDiskUsage =
                        currentRecords.Min(
                            record =>
                                record.DiskUsage),

                    MaximumDiskUsage =
                        currentRecords.Max(
                            record =>
                                record.DiskUsage)
                };

            AddTemperatureStatistics(
                analysis,
                currentRecords);

            AddComparison(
                analysis,
                previousRecords);

            analysis.OverallTrend =
                CalculateOverallTrend(
                    analysis.CpuTrend,
                    analysis.RamTrend,
                    analysis.DiskTrend);

            return analysis;
        }

        private static void AddTemperatureStatistics(
            PerformanceHistoryAnalysis analysis,
            IReadOnlyList<PerformanceHistoryRecord>
                records)
        {
            List<double> temperatures =
                records
                    .Where(
                        record =>
                            record.CpuTemperature
                                .HasValue)
                    .Select(
                        record =>
                            record.CpuTemperature!
                                .Value)
                    .ToList();

            if (temperatures.Count == 0)
            {
                analysis.AverageCpuTemperature = null;
                analysis.MinimumCpuTemperature = null;
                analysis.MaximumCpuTemperature = null;

                return;
            }

            analysis.AverageCpuTemperature =
                temperatures.Average();

            analysis.MinimumCpuTemperature =
                temperatures.Min();

            analysis.MaximumCpuTemperature =
                temperatures.Max();
        }

        private static void AddComparison(
            PerformanceHistoryAnalysis analysis,
            IReadOnlyList<PerformanceHistoryRecord>
                previousRecords)
        {
            if (previousRecords.Count == 0)
            {
                analysis.CpuTrend =
                    PerformanceTrend.Unknown;

                analysis.RamTrend =
                    PerformanceTrend.Unknown;

                analysis.DiskTrend =
                    PerformanceTrend.Unknown;

                return;
            }

            double previousCpuAverage =
                previousRecords.Average(
                    record =>
                        record.CpuUsage);

            double previousRamAverage =
                previousRecords.Average(
                    record =>
                        record.RamUsage);

            double previousDiskAverage =
                previousRecords.Average(
                    record =>
                        record.DiskUsage);

            analysis.CpuChange =
                analysis.AverageCpuUsage -
                previousCpuAverage;

            analysis.RamChange =
                analysis.AverageRamUsage -
                previousRamAverage;

            analysis.DiskChange =
                analysis.AverageDiskUsage -
                previousDiskAverage;

            analysis.CpuTrend =
                CalculateTrend(
                    analysis.CpuChange);

            analysis.RamTrend =
                CalculateTrend(
                    analysis.RamChange);

            analysis.DiskTrend =
                CalculateTrend(
                    analysis.DiskChange);
        }

        private static PerformanceTrend CalculateTrend(
            double change)
        {
            if (Math.Abs(change) <= TrendThreshold)
            {
                return PerformanceTrend.Stable;
            }

            return change < 0
                ? PerformanceTrend.Improving
                : PerformanceTrend.Degrading;
        }

        private static PerformanceTrend
            CalculateOverallTrend(
                PerformanceTrend cpuTrend,
                PerformanceTrend ramTrend,
                PerformanceTrend diskTrend)
        {
            PerformanceTrend[] trends =
            {
                cpuTrend,
                ramTrend,
                diskTrend
            };

            int improvingCount =
                trends.Count(
                    trend =>
                        trend ==
                        PerformanceTrend.Improving);

            int degradingCount =
                trends.Count(
                    trend =>
                        trend ==
                        PerformanceTrend.Degrading);

            int knownCount =
                trends.Count(
                    trend =>
                        trend !=
                        PerformanceTrend.Unknown);

            if (knownCount == 0)
            {
                return PerformanceTrend.Unknown;
            }

            if (degradingCount > improvingCount)
            {
                return PerformanceTrend.Degrading;
            }

            if (improvingCount > degradingCount)
            {
                return PerformanceTrend.Improving;
            }

            return PerformanceTrend.Stable;
        }
    }
}