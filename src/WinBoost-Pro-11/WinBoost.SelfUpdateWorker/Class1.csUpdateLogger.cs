using System;
using System.IO;

namespace WinBoost.SelfUpdateWorker
{
    internal static class UpdateLogger
    {
        private static readonly object SyncRoot =
            new object();

        private static readonly string LogDirectory =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "WinBoost",
                "Logs");

        private static readonly string LogFilePath =
            Path.Combine(
                LogDirectory,
                "self-update.log");

        public static string FilePath =>
            LogFilePath;

        public static void Write(
            string message)
        {
            try
            {
                Directory.CreateDirectory(
                    LogDirectory);

                string line =
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";

                lock (SyncRoot)
                {
                    File.AppendAllText(
                        LogFilePath,
                        line + Environment.NewLine);
                }
            }
            catch
            {
                // Logging must never stop
                // the update process.
            }
        }

        public static void WriteException(
            string context,
            Exception exception)
        {
            Write(
                $"{context}: {exception.GetType().Name} - {exception.Message}");
        }
    }
}