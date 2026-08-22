using System;
using System.IO;
using System.Security.Cryptography;

namespace WinBoost.SelfUpdateWorker
{
    internal static class UpdatePackageSignatureVerifier
    {
        public static bool Verify(
            string packagePath,
            string packageSignature,
            string publicKeyPem)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(
                packagePath);

            ArgumentException.ThrowIfNullOrWhiteSpace(
                packageSignature);

            ArgumentException.ThrowIfNullOrWhiteSpace(
                publicKeyPem);

            if (!File.Exists(
                    packagePath))
            {
                return false;
            }

            byte[] signature;

            try
            {
                signature =
                    Convert.FromBase64String(
                        packageSignature.Trim());
            }
            catch (FormatException)
            {
                return false;
            }

            try
            {
                using RSA rsa =
                    RSA.Create();

                rsa.ImportFromPem(
                    publicKeyPem);

                using FileStream packageStream =
                    File.OpenRead(
                        packagePath);

                return rsa.VerifyData(
                    packageStream,
                    signature,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pss);
            }
            catch (CryptographicException)
            {
                return false;
            }
            catch (IOException)
            {
                return false;
            }
        }
    }
}