using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using WinBoost.App.Models;

namespace WinBoost.App.Services.Licensing
{
    public sealed class LicenseStorageService
    {
        private const string LicenseFileName =
            "license.dat";

        private static readonly byte[] Entropy =
            Encoding.UTF8.GetBytes(
                "WinBoost-Pro-11-License-v1");

        private readonly string _licenseFilePath;

        public LicenseStorageService()
        {
            string appDataFolder =
                Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.LocalApplicationData),
                    "WinBoostPro11");

            Directory.CreateDirectory(
                appDataFolder);

            _licenseFilePath =
                Path.Combine(
                    appDataFolder,
                    LicenseFileName);
        }

        public void Save(
            LicenseInfo license)
        {
            ArgumentNullException.ThrowIfNull(
                license);

            string json =
                JsonSerializer.Serialize(
                    license);

            byte[] plainData =
                Encoding.UTF8.GetBytes(
                    json);

            byte[] protectedData =
                ProtectedData.Protect(
                    plainData,
                    Entropy,
                    DataProtectionScope.CurrentUser);

            File.WriteAllBytes(
                _licenseFilePath,
                protectedData);
        }

        public LicenseInfo Load()
        {
            if (!File.Exists(
                    _licenseFilePath))
            {
                return CreateUnlicensed();
            }

            try
            {
                byte[] protectedData =
                    File.ReadAllBytes(
                        _licenseFilePath);

                byte[] plainData =
                    ProtectedData.Unprotect(
                        protectedData,
                        Entropy,
                        DataProtectionScope.CurrentUser);

                string json =
                    Encoding.UTF8.GetString(
                        plainData);

                LicenseInfo? license =
                    JsonSerializer.Deserialize<LicenseInfo>(
                        json);

                return license ??
                    CreateUnlicensed();
            }
            catch
            {
                return new LicenseInfo
                {
                    Status =
                        LicenseStatus.Invalid
                };
            }
        }

        public void Delete()
        {
            if (File.Exists(
                    _licenseFilePath))
            {
                File.Delete(
                    _licenseFilePath);
            }
        }

        private static LicenseInfo
            CreateUnlicensed()
        {
            return new LicenseInfo
            {
                Status =
                    LicenseStatus.Unlicensed
            };
        }
    }
}