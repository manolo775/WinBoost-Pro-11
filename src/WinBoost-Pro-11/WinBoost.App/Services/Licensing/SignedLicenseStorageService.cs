using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using WinBoost.App.Models;

namespace WinBoost.App.Services.Licensing
{
    public sealed class SignedLicenseStorageService
    {
        private static readonly byte[] Entropy =
            Encoding.UTF8.GetBytes(
                "WinBoostPro11.SignedLicense.v1");

        private readonly string
            _storagePath;

        public SignedLicenseStorageService()
        {
            string applicationFolder =
                Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder
                            .LocalApplicationData),
                    "WinBoostPro11");

            _storagePath =
                Path.Combine(
                    applicationFolder,
                    "signed-license.dat");
        }

        public void Save(
            SignedLicenseResponse license)
        {
            ArgumentNullException.ThrowIfNull(
                license);

            string? directory =
                Path.GetDirectoryName(
                    _storagePath);

            if (!string.IsNullOrWhiteSpace(
                    directory))
            {
                Directory.CreateDirectory(
                    directory);
            }

            string json =
                JsonSerializer.Serialize(
                    license);

            byte[] plainBytes =
                Encoding.UTF8.GetBytes(
                    json);

            byte[] protectedBytes =
                ProtectedData.Protect(
                    plainBytes,
                    Entropy,
                    DataProtectionScope.CurrentUser);

            File.WriteAllBytes(
                _storagePath,
                protectedBytes);
        }

        public SignedLicenseResponse? Load()
        {
            if (!File.Exists(
                    _storagePath))
            {
                return null;
            }

            try
            {
                byte[] protectedBytes =
                    File.ReadAllBytes(
                        _storagePath);

                byte[] plainBytes =
                    ProtectedData.Unprotect(
                        protectedBytes,
                        Entropy,
                        DataProtectionScope.CurrentUser);

                string json =
                    Encoding.UTF8.GetString(
                        plainBytes);

                return JsonSerializer
                    .Deserialize<
                        SignedLicenseResponse>(
                        json);
            }
            catch
            {
                return null;
            }
        }

        public void Delete()
        {
            if (File.Exists(
                    _storagePath))
            {
                File.Delete(
                    _storagePath);
            }
        }
    }
}