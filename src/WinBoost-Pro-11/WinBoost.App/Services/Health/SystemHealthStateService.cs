using System;
using WinBoost.App.Models;

namespace WinBoost.App.Services.Health
{
    public class SystemHealthStateService
    {
        private static readonly Lazy<SystemHealthStateService>
            LazyInstance =
                new(() => new SystemHealthStateService());

        private readonly object
            _syncRoot =
                new();

        private readonly SystemHealthCalculator
            _calculator;

        private readonly SystemHealthSummary
            _summary;

        private readonly SystemHealthRawData
            _rawData;

        private SystemHealthStateService()
        {
            _calculator =
                new SystemHealthCalculator();

            _summary =
                new SystemHealthSummary
                {
                    PerformanceScore = 80,
                    ServicesScore = 80,
                    StartupScore = 80,
                    PrivacyScore = 80,
                    WindowsUpdateScore = 80
                };

            _rawData =
                new SystemHealthRawData();
        }

        public static SystemHealthStateService Instance =>
            LazyInstance.Value;

        public SystemHealthSummary Summary =>
            _summary;

        public SystemHealthRawData RawData =>
            _rawData;

        public event EventHandler?
            HealthChanged;

        public void UpdatePerformanceScore(
     int score)
        {
            int normalizedScore =
                NormalizeScore(
                    score);

            lock (_syncRoot)
            {
                _summary.PerformanceScore =
                    normalizedScore;

                WinBoostHealthScoreService
                    .Instance
                    .PerformanceScore =
                        normalizedScore;
            }

            OnHealthChanged();
        }

        public void UpdateServicesData(
            int totalServices,
            int criticalServices,
            int optionalServices,
            int lowRiskServices)
        {
            lock (_syncRoot)
            {
                _rawData.Services.TotalServices =
                    Math.Max(0, totalServices);

                _rawData.Services.CriticalServices =
                    Math.Max(0, criticalServices);

                _rawData.Services.OptionalServices =
                    Math.Max(0, optionalServices);

                _rawData.Services.LowRiskServices =
                    Math.Max(0, lowRiskServices);

                _summary.ServicesScore =
                    _calculator.CalculateServicesScore(
                        _rawData.Services.TotalServices,
                        _rawData.Services.CriticalServices,
                        _rawData.Services.OptionalServices,
                        _rawData.Services.LowRiskServices);

                WinBoostHealthScoreService
                      .Instance
                      .ServicesScore =
                       _summary.ServicesScore;

            }

            OnHealthChanged();
        }

        public void UpdateStartupData(
            int totalStartupApps,
            int enabledStartupApps)
        {
            lock (_syncRoot)
            {
                _rawData.Startup.TotalStartupApps =
                    Math.Max(0, totalStartupApps);

                _rawData.Startup.EnabledStartupApps =
                    Math.Clamp(
                        enabledStartupApps,
                        0,
                        _rawData.Startup.TotalStartupApps);

                _summary.StartupScore =
                    _calculator.CalculateStartupScore(
                        _rawData.Startup.TotalStartupApps,
                        _rawData.Startup.EnabledStartupApps);

                WinBoostHealthScoreService
                          .Instance
                          .StartupScore =
                           _summary.StartupScore;

            }

            OnHealthChanged();
        }

        public void UpdatePrivacyData(
            int totalChecks,
            int passedChecks)
        {
            lock (_syncRoot)
            {
                _rawData.Privacy.TotalChecks =
                    Math.Max(0, totalChecks);

                _rawData.Privacy.PassedChecks =
                    Math.Clamp(
                        passedChecks,
                        0,
                        _rawData.Privacy.TotalChecks);

                _summary.PrivacyScore =
                    _calculator.CalculatePrivacyScore(
                        _rawData.Privacy.TotalChecks,
                        _rawData.Privacy.PassedChecks);

                WinBoostHealthScoreService
                    .Instance
                    .PrivacyScore =
                        _summary.PrivacyScore;
            }

            OnHealthChanged();
        }

        public void UpdateWindowsUpdateData(
            int pendingUpdates,
            bool requiresRestart)
        {
            lock (_syncRoot)
            {
                _rawData.WindowsUpdate.PendingUpdates =
                    Math.Max(0, pendingUpdates);

                _rawData.WindowsUpdate.RequiresRestart =
                    requiresRestart;

                _summary.WindowsUpdateScore =
                    _calculator.CalculateWindowsUpdateScore(
                        _rawData.WindowsUpdate.PendingUpdates,
                        _rawData.WindowsUpdate.RequiresRestart);

                WinBoostHealthScoreService
                       .Instance
                       .WindowsUpdateScore =
                       _summary.WindowsUpdateScore;

            }

            OnHealthChanged();
        }

        private static int NormalizeScore(
            int score)
        {
            return Math.Clamp(
                score,
                0,
                100);
        }

        private void OnHealthChanged()
        {
            HealthChanged?.Invoke(
                this,
                EventArgs.Empty);
        }
    }
}