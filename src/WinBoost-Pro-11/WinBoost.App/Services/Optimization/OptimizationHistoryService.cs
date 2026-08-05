using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using WinBoost.App.Models;

namespace WinBoost.App.Services.Optimization
{
    public sealed class OptimizationHistoryService
    {
        private const int MaximumHistoryEntries =
            50;

        private static readonly Lazy<OptimizationHistoryService>
            LazyInstance =
                new(() =>
                    new OptimizationHistoryService());

        private readonly ObservableCollection<
            OptimizationHistoryEntry>
            _entries =
                new();

        private readonly ReadOnlyObservableCollection<
            OptimizationHistoryEntry>
            _readOnlyEntries;

        private readonly string
            _historyFilePath;

        private readonly JsonSerializerOptions
            _jsonOptions =
                new()
                {
                    WriteIndented = true
                };

        private OptimizationHistoryService()
        {
            _readOnlyEntries =
                new ReadOnlyObservableCollection<
                    OptimizationHistoryEntry>(
                        _entries);

            string applicationDataPath =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData);

            string dataDirectoryPath =
                Path.Combine(
                    applicationDataPath,
                    "WinBoost Pro 11",
                    "Data");

            _historyFilePath =
                Path.Combine(
                    dataDirectoryPath,
                    "optimization-history.json");

            LoadHistory();
        }

        public static OptimizationHistoryService Instance =>
            LazyInstance.Value;

        public ReadOnlyObservableCollection<
            OptimizationHistoryEntry>
            Entries =>
                _readOnlyEntries;

        public void Add(
            OptimizationReport report)
        {
            ArgumentNullException.ThrowIfNull(
                report);

            var entry =
                new OptimizationHistoryEntry
                {
                    CompletedAt =
                        DateTime.Now,

                    IsSuccessful =
                        report.IsSuccessful,

                    DeletedFiles =
                        report.TotalDeletedFiles,

                    RecoveredBytes =
                        report.TotalRecoveredBytes,

                    DurationSeconds =
                        report.Duration.TotalSeconds,

                    SuccessfulOperations =
                        report.SuccessfulOperations,

                    FailedOperations =
                        report.FailedOperations,

                    SkippedOperations =
                        report.SkippedOperations
                };

            _entries.Insert(
                0,
                entry);

            TrimHistory();

            SaveHistory();
        }

        public void Clear()
        {
            _entries.Clear();

            try
            {
                if (File.Exists(
                        _historyFilePath))
                {
                    File.Delete(
                        _historyFilePath);
                }
            }
            catch
            {
                // Istoricul este eliminat din interfață chiar dacă
                // fișierul nu poate fi șters momentan.
            }
        }

        private void LoadHistory()
        {
            try
            {
                if (!File.Exists(
                        _historyFilePath))
                {
                    return;
                }

                string json =
                    File.ReadAllText(
                        _historyFilePath);

                List<OptimizationHistoryEntry>?
                    savedEntries =
                        JsonSerializer.Deserialize<
                            List<OptimizationHistoryEntry>>(
                                json,
                                _jsonOptions);

                if (savedEntries == null)
                {
                    return;
                }

                foreach (OptimizationHistoryEntry entry
                         in savedEntries
                             .OrderByDescending(
                                 item =>
                                     item.CompletedAt)
                             .Take(
                                 MaximumHistoryEntries))
                {
                    _entries.Add(
                        entry);
                }
            }
            catch
            {
                // Un fișier lipsă, corupt sau inaccesibil nu trebuie
                // să împiedice pornirea aplicației.
            }
        }

        private void SaveHistory()
        {
            try
            {
                string? directoryPath =
                    Path.GetDirectoryName(
                        _historyFilePath);

                if (!string.IsNullOrWhiteSpace(
                        directoryPath))
                {
                    Directory.CreateDirectory(
                        directoryPath);
                }

                string json =
                    JsonSerializer.Serialize(
                        _entries.ToList(),
                        _jsonOptions);

                File.WriteAllText(
                    _historyFilePath,
                    json);
            }
            catch
            {
                // Eșecul salvării istoricului nu trebuie să oprească
                // procesul principal de optimizare.
            }
        }

        private void TrimHistory()
        {
            while (_entries.Count >
                   MaximumHistoryEntries)
            {
                _entries.RemoveAt(
                    _entries.Count - 1);
            }
        }
    }
}