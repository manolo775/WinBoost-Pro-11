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

        private static readonly object
            DatabaseSyncRoot =
                new();

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
            lock (DatabaseSyncRoot)
            {
                using LiteDatabase database =
                    OpenDatabase();

                ILiteCollection<
                    PerformanceHistoryRecord>
                    collection =
                        GetCollection(database);

                collection.Insert(record);
            }
        }

        public IReadOnlyList<
            PerformanceHistoryRecord> GetRecords(
            DateTime from,
            DateTime to)
        {
            lock (DatabaseSyncRoot)
            {
                using LiteDatabase database =
                    OpenDatabase();

                ILiteCollection<
                    PerformanceHistoryRecord>
                    collection =
                        GetCollection(database);

                return collection
                    .Find(record =>
                        record.Timestamp >= from &&
                        record.Timestamp <= to)
                    .OrderBy(record =>
                        record.Timestamp)
                    .ToList();
            }
        }

        public void DeleteAll()
        {
            lock (DatabaseSyncRoot)
            {
                using LiteDatabase database =
                    OpenDatabase();

                ILiteCollection<
                    PerformanceHistoryRecord>
                    collection =
                        GetCollection(database);

                collection.DeleteAll();
            }
        }

        public void DeleteOlderThan(
            DateTime threshold)
        {
            lock (DatabaseSyncRoot)
            {
                using LiteDatabase database =
                    OpenDatabase();

                ILiteCollection<
                    PerformanceHistoryRecord>
                    collection =
                        GetCollection(database);

                collection.DeleteMany(record =>
                    record.Timestamp < threshold);
            }
        }

        private void InitializeDatabase()
        {
            lock (DatabaseSyncRoot)
            {
                using LiteDatabase database =
                    OpenDatabase();

                ILiteCollection<
                    PerformanceHistoryRecord>
                    collection =
                        GetCollection(database);

                collection.EnsureIndex(
                    record =>
                        record.Timestamp);
            }
        }

        private LiteDatabase OpenDatabase()
        {
            var connectionString =
                new ConnectionString
                {
                    Filename =
                        _databasePath,

                    Connection =
                        ConnectionType.Shared
                };

            return new LiteDatabase(
                connectionString);
        }

        private static ILiteCollection<
            PerformanceHistoryRecord>
            GetCollection(
                LiteDatabase database)
        {
            return database.GetCollection<
                PerformanceHistoryRecord>(
                    CollectionName);
        }
    }
}