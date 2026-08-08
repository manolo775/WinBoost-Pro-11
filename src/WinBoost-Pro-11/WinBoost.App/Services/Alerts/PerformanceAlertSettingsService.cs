using System;
using System.IO;
using System.Text.Json;
using WinBoost.App.Models;

namespace WinBoost.App.Services.Alerts
{
    public sealed class
        PerformanceAlertSettingsService
    {
        private const string SettingsFileName =
            "performance-alert-settings.json";

        private readonly object _syncRoot =
            new();

        private readonly string _settingsPath;

        private readonly JsonSerializerOptions
            _serializerOptions =
                new()
                {
                    WriteIndented = true
                };

        public PerformanceAlertSettingsService()
        {
            string applicationDataPath =
                Environment.GetFolderPath(
                    Environment.SpecialFolder
                        .LocalApplicationData);

            string applicationDirectory =
                Path.Combine(
                    applicationDataPath,
                    "WinBoostPro11");

            Directory.CreateDirectory(
                applicationDirectory);

            _settingsPath =
                Path.Combine(
                    applicationDirectory,
                    SettingsFileName);
        }

        public PerformanceAlertSettings Load()
        {
            lock (_syncRoot)
            {
                if (!File.Exists(
                        _settingsPath))
                {
                    return new
                        PerformanceAlertSettings();
                }

                try
                {
                    string json =
                        File.ReadAllText(
                            _settingsPath);

                    PerformanceAlertSettings?
                        settings =
                            JsonSerializer.Deserialize<
                                PerformanceAlertSettings>(
                                    json,
                                    _serializerOptions);

                    return settings ??
                        new PerformanceAlertSettings();
                }
                catch
                {
                    return new
                        PerformanceAlertSettings();
                }
            }
        }

        public void Save(
            PerformanceAlertSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(
                    nameof(settings));
            }

            lock (_syncRoot)
            {
                string json =
                    JsonSerializer.Serialize(
                        settings,
                        _serializerOptions);

                string temporaryPath =
                    _settingsPath +
                    ".tmp";

                File.WriteAllText(
                    temporaryPath,
                    json);

                File.Move(
                    temporaryPath,
                    _settingsPath,
                    true);
            }
        }
    }
}