using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using WinBoost.App.Models;

namespace WinBoost.App.Services.Licensing
{
    public sealed class LicenseSignatureVerifier
    {
        public bool Verify(
            SignedLicenseResponse license,
            string publicKeyPem)
        {
            ArgumentNullException.ThrowIfNull(
                license);

            if (string.IsNullOrWhiteSpace(
                    publicKeyPem))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(
                    license.Signature))
            {
                return false;
            }

            try
            {
                string signedData =
                    BuildSignedData(
                        license);

                byte[] dataBytes =
                    Encoding.UTF8.GetBytes(
                        signedData);

                byte[] signatureBytes =
                    Convert.FromBase64String(
                        license.Signature);

                using RSA rsa =
                    RSA.Create();

                rsa.ImportFromPem(
                    publicKeyPem);

                return rsa.VerifyData(
                    dataBytes,
                    signatureBytes,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pss);
            }
            catch (FormatException)
            {
                return false;
            }
            catch (CryptographicException)
            {
                return false;
            }
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