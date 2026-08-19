using System;
using WinBoost.App.Models;

namespace WinBoost.App.Services.Licensing
{
    public sealed class LicenseResponseValidator
    {
        private readonly LicenseSignatureVerifier
            _signatureVerifier;

        private readonly DeviceIdentityService
            _deviceIdentityService;

        public LicenseResponseValidator()
        {
            _signatureVerifier =
                new LicenseSignatureVerifier();

            _deviceIdentityService =
                new DeviceIdentityService();
        }

        public LicenseActivationResult Validate(
            SignedLicenseResponse response,
            string publicKeyPem)
        {
            ArgumentNullException.ThrowIfNull(
                response);

            bool signatureValid =
                _signatureVerifier.Verify(
                    response,
                    publicKeyPem);

            if (!signatureValid)
            {
                return new LicenseActivationResult
                {
                    Status =
                        LicenseActivationStatus.InvalidKey,

                    Message =
                        "The license signature is invalid."
                };
            }

            if (!string.Equals(
                    response.ProductName,
                    LicenseSecurityConfiguration
                        .ProductName,
                    StringComparison.Ordinal))
            {
                return new LicenseActivationResult
                {
                    Status =
                        LicenseActivationStatus.InvalidKey,

                    Message =
                        "The license is for another product."
                };
            }

            string currentDeviceId =
                _deviceIdentityService
                    .GetDeviceId();

            if (!string.Equals(
                    response.DeviceId,
                    currentDeviceId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return new LicenseActivationResult
                {
                    Status =
                        LicenseActivationStatus.InvalidKey,

                    Message =
                        "The license belongs to another device."
                };
            }

            if (!IsPlanConfigurationValid(
                    response))
            {
                return new LicenseActivationResult
                {
                    Status =
                        LicenseActivationStatus.InvalidKey,

                    Message =
                        "The license plan configuration is invalid."
                };
            }

            if (response.ExpiresAt.HasValue &&
                response.ExpiresAt.Value
                    .ToUniversalTime() <=
                DateTime.UtcNow)
            {
                return new LicenseActivationResult
                {
                    Status =
                        LicenseActivationStatus.Expired,

                    Message =
                        "The license has expired."
                };
            }

            LicenseStatus licenseStatus =
                string.Equals(
                    response.LicenseType,
                    "Trial",
                    StringComparison.OrdinalIgnoreCase)
                    ? LicenseStatus.Trial
                    : LicenseStatus.Licensed;

            var licenseInfo =
                new LicenseInfo
                {
                    Status =
                        licenseStatus,

                    LicenseId =
                        response.LicenseId,

                    CustomerEmail =
                        response.CustomerEmail,

                    LicenseType =
                        response.LicenseType,

                    Plan =
                        response.Plan,

                    ActivatedAt =
                        response.ActivatedAt,

                    ExpiresAt =
                        response.ExpiresAt,

                    LicensedTo =
                        response.CustomerEmail,

                    LicenseKey =
                        string.Empty
                };

            return new LicenseActivationResult
            {
                Status =
                    LicenseActivationStatus.Success,

                License =
                    licenseInfo,

                Message =
                    "The license is valid."
            };
        }

        private static bool IsPlanConfigurationValid(
            SignedLicenseResponse response)
        {
            switch (response.Plan)
            {
                case LicensePlan.PromotionalLifetime:

                    return !response.ExpiresAt.HasValue;

                case LicensePlan.OneMonth:
                case LicensePlan.ThreeMonths:
                case LicensePlan.SixMonths:
                case LicensePlan.OneYear:

                    return response.ExpiresAt.HasValue &&
                           response.ExpiresAt.Value
                               .ToUniversalTime() >
                           response.ActivatedAt
                               .ToUniversalTime();

                default:

                    return false;
            }
        }
    }
}