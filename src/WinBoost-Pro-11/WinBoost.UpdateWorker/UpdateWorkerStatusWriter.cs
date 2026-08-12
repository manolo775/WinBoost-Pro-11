using System;
using System.IO;
using System.Text.Json;

namespace WinBoost.UpdateWorker
{
    internal static class UpdateWorkerStatusWriter
    {
        private static readonly string
            StatusDirectory =
                Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.LocalApplicationData),
                    "WinBoost");

        private static readonly string
            StatusFilePath =
                Path.Combine(
                    StatusDirectory,
                    "windows-update-status.json");

        public static string FilePath =>
            StatusFilePath;

        public static void Write(
            UpdateWorkerStatus status)
        {
            try
            {
                Directory.CreateDirectory(
                    StatusDirectory);

                string json =
                    JsonSerializer.Serialize(
                        status,
                        new JsonSerializerOptions
                        {
                            WriteIndented = true
                        });

                string tempFilePath =
                    StatusFilePath + ".tmp";

                File.WriteAllText(
                    tempFilePath,
                    json);

                File.Move(
                    tempFilePath,
                    StatusFilePath,
                    true);
            }
            catch
            {
                // Status reporting must never stop
                // the Windows Update operation.
            }
        }

        public static void Reset()
        {
            Write(
                new UpdateWorkerStatus
                {
                    State = "Idle",
                    Percent = 0,
                    CurrentUpdate = 0,
                    TotalUpdates = 0,
                    CurrentUpdateTitle = string.Empty,
                    Message = string.Empty,
                    RebootRequired = false,
                    IsCompleted = false,
                    IsSuccessful = false,
                    ErrorMessage = string.Empty
                });
        }
    }
}