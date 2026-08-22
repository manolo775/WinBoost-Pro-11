using System;
using System.IO;
using System.Text.Json;

namespace WinBoost.SelfUpdateWorker
{
    internal static class UpdateResultStore
    {
        private static readonly string ResultDirectory =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "WinBoost",
                "Update");

        private static readonly string ResultFilePath =
            Path.Combine(
                ResultDirectory,
                "last-update-result.json");

        public static void SaveSuccess()
        {
            Save(
                new UpdateResult
                {
                    Success = true,
                    RolledBack = false,
                    Message =
                        "The WinBoost update was installed successfully.",
                    CompletedAtUtc =
                        DateTime.UtcNow
                });
        }

        public static void SaveFailure(
            string message,
            bool rolledBack)
        {
            Save(
                new UpdateResult
                {
                    Success = false,
                    RolledBack = rolledBack,
                    Message =
                        message ?? string.Empty,
                    CompletedAtUtc =
                        DateTime.UtcNow
                });
        }

        private static void Save(
            UpdateResult result)
        {
            try
            {
                Directory.CreateDirectory(
                    ResultDirectory);

                string json =
                    JsonSerializer.Serialize(
                        result,
                        new JsonSerializerOptions
                        {
                            WriteIndented = true
                        });

                File.WriteAllText(
                    ResultFilePath,
                    json);
            }
            catch (Exception ex)
            {
                UpdateLogger.WriteException(
                    "Could not save update result",
                    ex);
            }
        }

        private sealed class UpdateResult
        {
            public bool Success
            {
                get;
                set;
            }

            public bool RolledBack
            {
                get;
                set;
            }

            public string Message
            {
                get;
                set;
            } = string.Empty;

            public DateTime CompletedAtUtc
            {
                get;
                set;
            }
        }
    }
}