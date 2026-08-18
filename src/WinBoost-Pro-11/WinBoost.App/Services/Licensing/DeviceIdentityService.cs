using System;
using System.IO;

namespace WinBoost.App.Services.Licensing
{
    public sealed class DeviceIdentityService
    {
        private const string DeviceIdFileName =
            "device.id";

        private readonly string _deviceIdFilePath;

        public DeviceIdentityService()
        {
            string appDataFolder =
                Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.LocalApplicationData),
                    "WinBoostPro11");

            Directory.CreateDirectory(
                appDataFolder);

            _deviceIdFilePath =
                Path.Combine(
                    appDataFolder,
                    DeviceIdFileName);
        }

        public string GetDeviceId()
        {
            try
            {
                if (File.Exists(
                        _deviceIdFilePath))
                {
                    string existingDeviceId =
                        File.ReadAllText(
                            _deviceIdFilePath)
                            .Trim();

                    if (Guid.TryParse(
                            existingDeviceId,
                            out _))
                    {
                        return existingDeviceId;
                    }
                }

                string newDeviceId =
                    Guid.NewGuid()
                        .ToString("D");

                File.WriteAllText(
                    _deviceIdFilePath,
                    newDeviceId);

                return newDeviceId;
            }
            catch
            {
                return Guid.NewGuid()
                    .ToString("D");
            }
        }
    }
}