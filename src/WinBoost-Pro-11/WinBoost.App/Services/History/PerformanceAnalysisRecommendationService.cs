using System;
using WinBoost.App.Models;

namespace WinBoost.App.Services.History
{
    public sealed class
        PerformanceAnalysisRecommendationService
    {
        private const double HighCpuThreshold =
            85.0;

        private const double HighRamThreshold =
            90.0;

        private const double HighDiskThreshold =
            90.0;

        private const double HighAverageTemperature =
            85.0;

        private const double HighMaximumTemperature =
            90.0;

        public PerformanceAnalysisRecommendation
            CreateRecommendation(
                PerformanceHistoryAnalysis analysis)
        {
            if (analysis == null)
            {
                throw new ArgumentNullException(
                    nameof(analysis));
            }

            if (!analysis.HasEnoughData)
            {
                return Create(
                    PerformanceAnalysisRecommendationType
                        .InsufficientData,
                    PerformanceAnalysisSeverity
                        .Information);
            }

            bool cpuHigh =
                analysis.AverageCpuUsage >=
                HighCpuThreshold;

            bool ramHigh =
                analysis.AverageRamUsage >=
                HighRamThreshold;

            bool diskHigh =
                analysis.AverageDiskUsage >=
                HighDiskThreshold;

            bool cpuTemperatureHigh =
                analysis.AverageCpuTemperature
                    .HasValue &&
                analysis.MaximumCpuTemperature
                    .HasValue &&
                (
                    analysis.AverageCpuTemperature
                        .Value >=
                    HighAverageTemperature ||

                    analysis.MaximumCpuTemperature
                        .Value >=
                    HighMaximumTemperature
                );

            bool cpuDegrading =
                analysis.CpuTrend ==
                PerformanceTrend.Degrading;

            bool ramDegrading =
                analysis.RamTrend ==
                PerformanceTrend.Degrading;

            bool diskDegrading =
                analysis.DiskTrend ==
                PerformanceTrend.Degrading;

            bool cpuIssue =
                cpuHigh ||
                cpuTemperatureHigh ||
                cpuDegrading;

            bool ramIssue =
                ramHigh ||
                ramDegrading;

            bool diskIssue =
                diskHigh ||
                diskDegrading;

            int issueCount = 0;

            if (cpuIssue)
            {
                issueCount++;
            }

            if (ramIssue)
            {
                issueCount++;
            }

            if (diskIssue)
            {
                issueCount++;
            }

            if (issueCount >= 2)
            {
                PerformanceAnalysisSeverity severity =
                    cpuHigh ||
                    cpuTemperatureHigh ||
                    ramHigh ||
                    diskHigh
                        ? PerformanceAnalysisSeverity
                            .Critical
                        : PerformanceAnalysisSeverity
                            .Warning;

                return Create(
                    PerformanceAnalysisRecommendationType
                        .MultipleIssues,
                    severity);
            }

            if (cpuTemperatureHigh)
            {
                return Create(
                    PerformanceAnalysisRecommendationType
                        .CpuTemperatureHigh,
                    PerformanceAnalysisSeverity
                        .Critical);
            }

            if (ramHigh)
            {
                return Create(
                    PerformanceAnalysisRecommendationType
                        .RamHigh,
                    PerformanceAnalysisSeverity
                        .Critical);
            }

            if (cpuHigh)
            {
                return Create(
                    PerformanceAnalysisRecommendationType
                        .CpuHigh,
                    PerformanceAnalysisSeverity
                        .Critical);
            }

            if (diskHigh)
            {
                return Create(
                    PerformanceAnalysisRecommendationType
                        .DiskHigh,
                    PerformanceAnalysisSeverity
                        .Critical);
            }

            if (ramDegrading)
            {
                return Create(
                    PerformanceAnalysisRecommendationType
                        .RamIncreasing,
                    PerformanceAnalysisSeverity
                        .Warning);
            }

            if (cpuDegrading)
            {
                return Create(
                    PerformanceAnalysisRecommendationType
                        .CpuIncreasing,
                    PerformanceAnalysisSeverity
                        .Warning);
            }

            if (diskDegrading)
            {
                return Create(
                    PerformanceAnalysisRecommendationType
                        .DiskIncreasing,
                    PerformanceAnalysisSeverity
                        .Warning);
            }

            return Create(
                PerformanceAnalysisRecommendationType
                    .Good,
                PerformanceAnalysisSeverity
                    .Good);
        }

        private static
            PerformanceAnalysisRecommendation Create(
                PerformanceAnalysisRecommendationType
                    type,
                PerformanceAnalysisSeverity severity)
        {
            return new PerformanceAnalysisRecommendation
            {
                Type = type,
                Severity = severity
            };
        }
    }
}