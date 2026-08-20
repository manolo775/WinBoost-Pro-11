using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using WinBoost.Licensing.Server.Configuration;
using WinBoost.Licensing.Server.Models;

namespace WinBoost.Licensing.Server.Services
{
    public sealed class LicenseSigningService
    {
        private readonly LicenseSigningOptions
            _options;

        public LicenseSigningService(
            IOptions<LicenseSigningOptions> options)
        {
            _options =
                options.Value;
        }

        public SignedLicenseResponse Sign(
            SignedLicenseResponse license)
        {
            ArgumentNullException.ThrowIfNull(
                license);

            if (string.IsNullOrWhiteSpace(
                    _options.PrivateKeyPem))
            {
                throw new InvalidOperationException(
                    "The license signing private key is not configured.");
            }

            string signedData =
                BuildSignedData(
                    license);

            byte[] dataBytes =
                Encoding.UTF8.GetBytes(
                    signedData);

            using RSA rsa =
                RSA.Create();

            rsa.ImportFromPem(
                _options.PrivateKeyPem);

            byte[] signatureBytes =
                rsa.SignData(
                    dataBytes,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pss);

            string signature =
                Convert.ToBase64String(
                    signatureBytes);

            return new SignedLicenseResponse
            {
                LicenseId =
                    license.LicenseId,

                CustomerEmail =
                    license.CustomerEmail,

                ProductName =
                    license.ProductName,

                LicenseType =
                    license.LicenseType,

                Plan =
                    license.Plan,

                ActivatedAt =
                    license.ActivatedAt,

                ExpiresAt =
                    license.ExpiresAt,

                DeviceId =
                    license.DeviceId,

                Signature =
                    signature
            };
        }

        private static string BuildSignedData(
            SignedLicenseResponse license)
        {
            string activatedAt =
                license.ActivatedAt
                    .ToUniversalTime()
                    .ToString(
                        "O",
                        CultureInfo.InvariantCulture);

            string expiresAt =
                license.ExpiresAt.HasValue
                    ? license.ExpiresAt.Value
                        .ToUniversalTime()
                        .ToString(
                            "O",
                            CultureInfo.InvariantCulture)
                    : string.Empty;

            return string.Join(
                "\n",
                license.LicenseId,
                license.CustomerEmail,
                license.ProductName,
                license.LicenseType,
                license.Plan.ToString(),
                activatedAt,
                expiresAt,
                license.DeviceId);
        }
    }
}