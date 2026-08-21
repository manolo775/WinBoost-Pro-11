using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace WinBoost.App.Services.Licensing
{
    public sealed class TrialDeviceTokenService
    {
        private const string MachineGuidRegistryPath =
            @"SOFTWARE\Microsoft\Cryptography";

        private const string MachineGuidValueName =
            "MachineGuid";

        private const string DomainSeparator =
            "WinBoostPro11|TrialDevice|v1";

        public string GetTrialDeviceToken()
        {
            string machineGuid =
                ReadMachineGuid();

            if (string.IsNullOrWhiteSpace(
                    machineGuid))
            {
                return string.Empty;
            }

            string normalizedMachineGuid =
                machineGuid
                    .Trim()
                    .ToUpperInvariant();

            string source =
                DomainSeparator +
                "|" +
                normalizedMachineGuid;

            byte[] sourceBytes =
                Encoding.UTF8.GetBytes(
                    source);

            byte[] hash =
                SHA256.HashData(
                    sourceBytes);

            return Convert.ToHexString(
                hash);
        }

        private static string ReadMachineGuid()
        {
            try
            {
                RegistryView registryView =
                    Environment.Is64BitOperatingSystem
                        ? RegistryView.Registry64
                        : RegistryView.Default;

                using RegistryKey baseKey =
                    RegistryKey.OpenBaseKey(
                        RegistryHive.LocalMachine,
                        registryView);

                using RegistryKey? key =
                    baseKey.OpenSubKey(
                        MachineGuidRegistryPath,
                        writable: false);

                object? value =
                    key?.GetValue(
                        MachineGuidValueName);

                return value?.ToString()
                    ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}