using System;
using System.IO;
using System.Text.Json;
using WinBoost.App.Models;

namespace WinBoost.App.Services.History
{
    public sealed class
        PerformanceHistorySettingsService
    {
        private const string SettingsFileName =
            "performance-history-settings.json";

        private readonly object _syncRoot =
            new();

        private readonly string _settingsPath;

        private readonly JsonSerializerOptions
            _serializerOptions =
                new()
                {
                    WriteIndented = true
                };

        public PerformanceHistorySettingsService()
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

        public PerformanceHistorySettings Load()
        {
            lock (_syncRoot)
            {
                if (!File.Exists(_settingsPath))
                {
                    return new
                        PerformanceHistorySettings();
                }

                try
                {
                    string json =
                        File.ReadAllText(
                            _settingsPath);

                    PerformanceHistorySettings?
                        settings =
                        JsonSerializer.Deserialize<
                            PerformanceHistorySettings>(
                                json,
                                _serializerOptions);

                    return settings ??
                        new PerformanceHistorySettings();
                }
                catch
                {
                    return new
                        PerformanceHistorySettings();
                }
            }
        }

        public void Save(
            PerformanceHistorySettings settings)
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