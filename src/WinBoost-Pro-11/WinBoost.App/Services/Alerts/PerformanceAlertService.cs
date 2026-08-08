using System;
using System.Collections.Generic;
using WinBoost.App.Models;

namespace WinBoost.App.Services.Alerts
{
    public sealed class PerformanceAlertService
    {
        private readonly
            PerformanceAlertDetectorService
            _detectorService;

        private readonly
            PerformanceAlertSettingsService
            _settingsService;

        private PerformanceAlertSettings
            _settings;

        public PerformanceAlertService()
        {
            _detectorService =
                new PerformanceAlertDetectorService();

            _settingsService =
                new PerformanceAlertSettingsService();

            _settings =
                _settingsService.Load();
        }

        public PerformanceAlertSettings Settings =>
            _settings;

        public IReadOnlyList<PerformanceAlert>
            Evaluate(
                SystemMetrics metrics)
        {
            if (metrics == null)
            {
                throw new ArgumentNullException(
                    nameof(metrics));
            }

            return _detectorService.Evaluate(
                metrics,
                _settings);
        }

        public void UpdateSettings(
            PerformanceAlertSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(
                    nameof(settings));
            }

            _settings =
                settings;

            _settingsService.Save(
                _settings);

            _detectorService.Reset();
        }

        public void ReloadSettings()
        {
            _settings =
                _settingsService.Load();

            _detectorService.Reset();
        }

        public void ResetDetection()
        {
            _detectorService.Reset();
        }
    }
}