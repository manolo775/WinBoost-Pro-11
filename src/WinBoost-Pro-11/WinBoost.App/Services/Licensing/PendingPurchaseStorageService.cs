using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using WinBoost.App.Models;

namespace WinBoost.App.Services.Licensing
{
    public sealed class PendingPurchaseStorageService
    {
        private static readonly byte[] Entropy =
            Encoding.UTF8.GetBytes(
                "WinBoostPro11.PendingPurchase.v1");

        private readonly string
            _storagePath;

        public PendingPurchaseStorageService()
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
                    "pending-purchase.dat");
        }

        public void Save(
            PendingPurchaseInfo purchase)
        {
            ArgumentNullException.ThrowIfNull(
                purchase);

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
                    purchase);

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

        public PendingPurchaseInfo? Load()
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
                        PendingPurchaseInfo>(
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