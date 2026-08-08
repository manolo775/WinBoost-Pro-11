using System;
using System.Collections.Generic;
using WinBoost.App.Models;

namespace WinBoost.App.Services.Alerts
{
    public sealed class
        PerformanceAlertDetectorService
    {
        private readonly object _syncRoot =
            new();

        private readonly Dictionary<
            PerformanceAlertType,
            DateTime> _conditionStartedUtc =
                new();

        private readonly Dictionary<
            PerformanceAlertType,
            DateTime> _lastAlertUtc =
                new();

        private readonly Dictionary<
            PerformanceAlertType,
            PerformanceAlertSeverity>
            _lastAlertSeverity =
                new();

        public IReadOnlyList<PerformanceAlert>
            Evaluate(
                SystemMetrics metrics,
                PerformanceAlertSettings settings,
                DateTime? evaluationTimeUtc = null)
        {
            if (metrics == null)
            {
                throw new ArgumentNullException(
                    nameof(metrics));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(
                    nameof(settings));
            }

            lock (_syncRoot)
            {
                if (!settings.AlertsEnabled)
                {
                    ClearDetectionState();

                    return Array.Empty<
                        PerformanceAlert>();
                }

                DateTime nowUtc =
                    evaluationTimeUtc ??
                    DateTime.UtcNow;

                TimeSpan requiredDuration =
                    TimeSpan.FromSeconds(
                        Math.Max(
                            1,
                            settings
                                .SustainedDurationSeconds));

                TimeSpan warningCooldown =
                    TimeSpan.FromMinutes(
                        Math.Max(
                            1,
                            settings
                                .AlertCooldownMinutes));

                TimeSpan?
                    criticalRepeatInterval =
                        settings
                            .CriticalAlertRepeatIntervalMinutes
                            <= 0
                            ? null
                            : TimeSpan.FromMinutes(
                                settings
                                    .CriticalAlertRepeatIntervalMinutes);

                var alerts =
                    new List<PerformanceAlert>();

                EvaluateMetric(
                    alerts,
                    PerformanceAlertType.CpuHigh,
                    settings.CpuAlertsEnabled,
                    metrics.CpuUsage,
                    settings.CpuWarningThreshold,
                    settings.CpuCriticalThreshold,
                    requiredDuration,
                    warningCooldown,
                    criticalRepeatInterval,
                    nowUtc);

                EvaluateMetric(
                    alerts,
                    PerformanceAlertType.RamHigh,
                    settings.RamAlertsEnabled,
                    metrics.RamUsage,
                    settings.RamWarningThreshold,
                    settings.RamCriticalThreshold,
                    requiredDuration,
                    warningCooldown,
                    criticalRepeatInterval,
                    nowUtc);

                EvaluateMetric(
                    alerts,
                    PerformanceAlertType.DiskHigh,
                    settings.DiskAlertsEnabled,
                    metrics.DiskUsage,
                    settings.DiskWarningThreshold,
                    settings.DiskCriticalThreshold,
                    requiredDuration,
                    warningCooldown,
                    criticalRepeatInterval,
                    nowUtc);

                EvaluateMetric(
                    alerts,
                    PerformanceAlertType
                        .CpuTemperatureHigh,
                    settings
                        .CpuTemperatureAlertsEnabled &&
                    metrics.CpuTemperature.IsAvailable,
                    metrics.CpuTemperature.IsAvailable
                        ? metrics.CpuTemperature.Celsius
                        : 0.0,
                    settings
                        .CpuTemperatureWarningThreshold,
                    settings
                        .CpuTemperatureCriticalThreshold,
                    requiredDuration,
                    warningCooldown,
                    criticalRepeatInterval,
                    nowUtc);

                return alerts;
            }
        }

        public void Reset()
        {
            lock (_syncRoot)
            {
                ClearDetectionState();
            }
        }

        private void EvaluateMetric(
            ICollection<PerformanceAlert> alerts,
            PerformanceAlertType type,
            bool enabled,
            double currentValue,
            double warningThreshold,
            double criticalThreshold,
            TimeSpan requiredDuration,
            TimeSpan warningCooldown,
            TimeSpan? criticalRepeatInterval,
            DateTime nowUtc)
        {
            if (!enabled ||
                double.IsNaN(currentValue) ||
                double.IsInfinity(currentValue))
            {
                ResetCondition(type);

                return;
            }

            double normalizedWarningThreshold =
                Math.Max(
                    0.0,
                    warningThreshold);

            double normalizedCriticalThreshold =
                Math.Max(
                    normalizedWarningThreshold,
                    criticalThreshold);

            if (currentValue <
                normalizedWarningThreshold)
            {
                ResetCondition(type);

                return;
            }

            PerformanceAlertSeverity severity =
                currentValue >=
                normalizedCriticalThreshold
                    ? PerformanceAlertSeverity
                        .Critical
                    : PerformanceAlertSeverity
                        .Warning;

            if (!_conditionStartedUtc.TryGetValue(
                    type,
                    out DateTime conditionStartedUtc))
            {
                _conditionStartedUtc[type] =
                    nowUtc;

                return;
            }

            TimeSpan sustainedDuration =
                nowUtc -
                conditionStartedUtc;

            if (sustainedDuration <
                requiredDuration)
            {
                return;
            }

            bool hasPreviousAlert =
                _lastAlertUtc.TryGetValue(
                    type,
                    out DateTime lastAlertUtc);

            bool severityEscalated =
                severity ==
                    PerformanceAlertSeverity
                        .Critical &&
                _lastAlertSeverity.TryGetValue(
                    type,
                    out PerformanceAlertSeverity
                        previousSeverity) &&
                previousSeverity ==
                    PerformanceAlertSeverity
                        .Warning;

            bool repeatIntervalFinished;

            if (!hasPreviousAlert)
            {
                repeatIntervalFinished =
                    true;
            }
            else if (severity ==
                     PerformanceAlertSeverity
                         .Critical)
            {
                repeatIntervalFinished =
                    criticalRepeatInterval
                        .HasValue &&
                    nowUtc - lastAlertUtc >=
                    criticalRepeatInterval.Value;
            }
            else
            {
                repeatIntervalFinished =
                    nowUtc - lastAlertUtc >=
                    warningCooldown;
            }

            if (!repeatIntervalFinished &&
                !severityEscalated)
            {
                return;
            }

            alerts.Add(
                new PerformanceAlert
                {
                    Type =
                        type,

                    Severity =
                        severity,

                    CreatedAtUtc =
                        nowUtc,

                    CurrentValue =
                        currentValue,

                    Threshold =
                        severity ==
                        PerformanceAlertSeverity
                            .Critical
                            ? normalizedCriticalThreshold
                            : normalizedWarningThreshold,

                    SustainedDuration =
                        sustainedDuration,

                    IsAcknowledged =
                        false
                });

            _lastAlertUtc[type] =
                nowUtc;

            _lastAlertSeverity[type] =
                severity;
        }

        private void ResetCondition(
            PerformanceAlertType type)
        {
            _conditionStartedUtc.Remove(
                type);

            _lastAlertUtc.Remove(
                type);

            _lastAlertSeverity.Remove(
                type);
        }

        private void ClearDetectionState()
        {
            _conditionStartedUtc.Clear();
            _lastAlertUtc.Clear();
            _lastAlertSeverity.Clear();
        }
    }
}