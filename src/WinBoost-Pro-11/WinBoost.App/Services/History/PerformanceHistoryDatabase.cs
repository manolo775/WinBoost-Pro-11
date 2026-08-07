using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiteDB;
using WinBoost.App.Models;

namespace WinBoost.App.Services.History
{
    public sealed class PerformanceHistoryDatabase
    {
        private const string DatabaseFileName =
      "performance-history.litedb";

        private const string CollectionName =
            "performance_history";

        private readonly string _databasePath;

        public PerformanceHistoryDatabase()
        {
            string applicationDataPath =
                Environment.GetFolderPath(
                    Environment.SpecialFolder
                        .LocalApplicationData);

            string databaseDirectory =
                Path.Combine(
                    applicationDataPath,
                    "WinBoostPro11");

            Directory.CreateDirectory(
                databaseDirectory);

            _databasePath =
                Path.Combine(
                    databaseDirectory,
                    DatabaseFileName);

            InitializeDatabase();
        }

        public void Save(
            PerformanceHistoryRecord record)
        {
            using var database =
                new LiteDatabase(
                    _databasePath);

            ILiteCollection<PerformanceHistoryRecord>
                collection =
                database.GetCollection<
                    PerformanceHistoryRecord>(
                    CollectionName);

            collection.Insert(record);
        }

        public IReadOnlyList<PerformanceHistoryRecord>
            GetRecords(
                DateTime from,
                DateTime to)
        {
            using var database =
                new LiteDatabase(
                    _databasePath);

            ILiteCollection<PerformanceHistoryRecord>
                collection =
                database.GetCollection<
                    PerformanceHistoryRecord>(
                    CollectionName);

            return collection
                .Find(record =>
                    record.Timestamp >= from &&
                    record.Timestamp <= to)
                .OrderBy(record =>
                    record.Timestamp)
                .ToList();
        }

        public void DeleteAll()
        {
            using var database =
                new LiteDatabase(
                    _databasePath);

            ILiteCollection<PerformanceHistoryRecord>
                collection =
                database.GetCollection<
                    PerformanceHistoryRecord>(
                    CollectionName);

            collection.DeleteAll();
        }

        public void DeleteOlderThan(
            DateTime threshold)
        {
            using var database =
                new LiteDatabase(
                    _databasePath);

            ILiteCollection<PerformanceHistoryRecord>
                collection =
                database.GetCollection<
                    PerformanceHistoryRecord>(
                    CollectionName);

            collection.DeleteMany(record =>
                record.Timestamp < threshold);
        }

        private void InitializeDatabase()
        {
            using var database =
                new LiteDatabase(
                    _databasePath);

            ILiteCollection<PerformanceHistoryRecord>
                collection =
                database.GetCollection<
                    PerformanceHistoryRecord>(
                    CollectionName);

            collection.EnsureIndex(
                record => record.Timestamp);
        }
    }
}