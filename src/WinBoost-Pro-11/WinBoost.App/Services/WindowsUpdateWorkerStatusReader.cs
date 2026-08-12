using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace WinBoost.App.Services.WindowsUpdate
{
    public sealed class WindowsUpdateWorkerStatus
    {
        public string State
        {
            get;
            set;
        } = "Idle";

        public int Percent
        {
            get;
            set;
        }

        public int CurrentUpdate
        {
            get;
            set;
        }

        public int TotalUpdates
        {
            get;
            set;
        }

        public string CurrentUpdateTitle
        {
            get;
            set;
        } = string.Empty;

        public string Message
        {
            get;
            set;
        } = string.Empty;

        public bool RebootRequired
        {
            get;
            set;
        }

        public bool IsCompleted
        {
            get;
            set;
        }

        public bool IsSuccessful
        {
            get;
            set;
        }

        public string ErrorMessage
        {
            get;
            set;
        } = string.Empty;
    }

    public sealed class WindowsUpdateWorkerStatusReader
    {
        private static readonly string
            StatusFilePath =
                Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.LocalApplicationData),
                    "WinBoost",
                    "windows-update-status.json");

        public string FilePath =>
            StatusFilePath;

        public Task<WindowsUpdateWorkerStatus?>
            ReadAsync()
        {
            return Task.Run(
                ReadInternal);
        }

        private static WindowsUpdateWorkerStatus?
            ReadInternal()
        {
            if (!File.Exists(
                StatusFilePath))
            {
                return null;
            }

            try
            {
                string json =
                    File.ReadAllText(
                        StatusFilePath);

                if (string.IsNullOrWhiteSpace(
                    json))
                {
                    return null;
                }

                return JsonSerializer.Deserialize<
                    WindowsUpdateWorkerStatus>(
                        json);
            }
            catch
            {
                return null;
            }
        }
    }
}