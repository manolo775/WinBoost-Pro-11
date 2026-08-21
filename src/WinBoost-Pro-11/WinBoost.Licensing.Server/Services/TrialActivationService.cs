using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WinBoost.Licensing.Server.Data;
using WinBoost.Licensing.Server.Models;

namespace WinBoost.Licensing.Server.Services
{
    public sealed class TrialActivationService
    {
        private const string ProductName =
            "WinBoost Pro 11";

        private static readonly TimeSpan
            TrialDuration =
                TimeSpan.FromHours(24);

        private readonly TrialRepository
            _trialRepository;

        private readonly LicenseRepository
            _licenseRepository;

        private readonly LicenseSigningService
            _licenseSigningService;

        public TrialActivationService(
            TrialRepository trialRepository,
            LicenseRepository licenseRepository,
            LicenseSigningService licenseSigningService)
        {
            _trialRepository =
                trialRepository;

            _licenseRepository =
                licenseRepository;

            _licenseSigningService =
                licenseSigningService;
        }

        public async Task<TrialActivationResponse>
            ActivateAsync(
                TrialActivationRequest request,
                CancellationToken cancellationToken =
                    default)
        {
            if (request == null)
            {
                return Error(
                    "INVALID_REQUEST",
                    "The trial activation request is invalid.");
            }

            string deviceId =
                request.DeviceId?.Trim()
                ?? string.Empty;

            string trialDeviceToken =
                request.TrialDeviceToken?.Trim()
                ?? string.Empty;

            string productName =
                request.ProductName?.Trim()
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(
                    deviceId) ||
                string.IsNullOrWhiteSpace(
                    trialDeviceToken) ||
                string.IsNullOrWhiteSpace(
                    productName))
            {
                return Error(
                    "INVALID_REQUEST",
                    "The trial activation request is incomplete.");
            }

            if (!string.Equals(
                    productName,
                    ProductName,
                    StringComparison.Ordinal))
            {
                return Error(
                    "INVALID_PRODUCT",
                    "The product is invalid.");
            }

            LicenseRecord? activePaidLicense =
                  await _licenseRepository
                  .FindActiveByDeviceAsync(
                     deviceId,
                  ProductName,
                  DateTime.UtcNow,
                  cancellationToken);

              if (activePaidLicense != null)
            {
                return Error(
                    "ACTIVE_LICENSE_EXISTS",
                    "An active paid WinBoost Pro 11 license already exists for this device.");
            }

            string tokenHash =
                ComputeSha256(
                    trialDeviceToken);

            TrialRecord? existingTrial =
                await _trialRepository
                    .FindByDeviceTokenHashAsync(
                        tokenHash,
                        ProductName,
                        cancellationToken);

            if (existingTrial != null)
            {
                return await HandleExistingTrialAsync(
                    existingTrial,
                    deviceId,
                    cancellationToken);
            }

            DateTime startedAtUtc =
                DateTime.UtcNow;

            DateTime expiresAtUtc =
                startedAtUtc.Add(
                    TrialDuration);

            string licenseId =
                Guid.NewGuid()
                    .ToString("N");

            var license =
                new LicenseRecord
                {
                    LicenseId =
                        licenseId,

                    CustomerEmail =
                        string.Empty,

                    DeviceId =
                        deviceId,

                    ProductName =
                        ProductName,

                    LicenseType =
                        "Trial",

                    Plan =
                        LicensePlan.Trial,

                    PurchaseSessionId =
                        "trial:" + licenseId,

                    ActivatedAtUtc =
                        startedAtUtc,

                    ExpiresAtUtc =
                        expiresAtUtc,

                    IsRevoked =
                        false,

                    RevokedAtUtc =
                        null,

                    CreatedAtUtc =
                        startedAtUtc,

                    UpdatedAtUtc =
                        startedAtUtc
                };

            var trial =
                new TrialRecord
                {
                    TrialDeviceTokenHash =
                        tokenHash,

                    DeviceId =
                        deviceId,

                    ProductName =
                        ProductName,

                    LicenseId =
                        licenseId,

                    StartedAtUtc =
                        startedAtUtc,

                    ExpiresAtUtc =
                        expiresAtUtc,

                    CreatedAtUtc =
                        startedAtUtc,

                    UpdatedAtUtc =
                        startedAtUtc
                };

            SignedLicenseResponse signedLicense;

            try
            {
                signedLicense =
                    CreateSignedTrialLicense(
                        licenseId,
                        deviceId,
                        startedAtUtc,
                        expiresAtUtc);
            }
            catch
            {
                return Error(
                    "LICENSE_SIGNING_ERROR",
                    "The trial license could not be signed.");
            }

            try
            {
                await _trialRepository
                    .CreateWithLicenseAsync(
                        trial,
                        license,
                        cancellationToken);
            }
            catch (DbUpdateException)
            {
                // Another request may have created
                // the Trial at almost the same time.
                TrialRecord? concurrentTrial =
                    await _trialRepository
                        .FindByDeviceTokenHashAsync(
                            tokenHash,
                            ProductName,
                            cancellationToken);

                if (concurrentTrial != null)
                {
                    return await HandleExistingTrialAsync(
                        concurrentTrial,
                        deviceId,
                        cancellationToken);
                }

                return Error(
                    "TRIAL_ALREADY_USED",
                    "The free trial has already been used on this device.");
            }
            catch
            {
                return Error(
                    "DATABASE_ERROR",
                    "The free trial could not be created.");
            }

            return Success(
                signedLicense);
        }

        private async Task<TrialActivationResponse>
            HandleExistingTrialAsync(
                TrialRecord existingTrial,
                string requestedDeviceId,
                CancellationToken cancellationToken)
        {
            if (!string.Equals(
                    existingTrial.DeviceId,
                    requestedDeviceId,
                    StringComparison.Ordinal))
            {
                return Error(
                    "TRIAL_ALREADY_USED",
                    "The free trial has already been used on this device.");
            }

            if (existingTrial.ExpiresAtUtc <=
                DateTime.UtcNow)
            {
                return Error(
                    "TRIAL_EXPIRED",
                    "The free trial has expired.");
            }

            LicenseRecord? existingLicense =
                await _licenseRepository
                    .FindByLicenseIdAsync(
                        existingTrial.LicenseId,
                        cancellationToken);

            if (existingLicense == null)
            {
                return Error(
                    "TRIAL_LICENSE_NOT_FOUND",
                    "The trial license could not be found.");
            }

            if (existingLicense.IsRevoked)
            {
                return Error(
                    "TRIAL_REVOKED",
                    "The trial license is no longer active.");
            }

            try
            {
                SignedLicenseResponse signedLicense =
                    CreateSignedTrialLicense(
                        existingTrial.LicenseId,
                        existingTrial.DeviceId,
                        existingTrial.StartedAtUtc,
                        existingTrial.ExpiresAtUtc);

                return Success(
                    signedLicense);
            }
            catch
            {
                return Error(
                    "LICENSE_SIGNING_ERROR",
                    "The trial license could not be signed.");
            }
        }

        private SignedLicenseResponse
            CreateSignedTrialLicense(
                string licenseId,
                string deviceId,
                DateTime activatedAtUtc,
                DateTime expiresAtUtc)
        {
            var unsignedLicense =
                new SignedLicenseResponse
                {
                    LicenseId =
                        licenseId,

                    CustomerEmail =
                        string.Empty,

                    ProductName =
                        ProductName,

                    LicenseType =
                        "Trial",

                    Plan =
                        LicensePlan.Trial,

                    ActivatedAt =
                        activatedAtUtc,

                    ExpiresAt =
                        expiresAtUtc,

                    DeviceId =
                        deviceId,

                    Signature =
                        string.Empty
                };

            return _licenseSigningService
                .Sign(
                    unsignedLicense);
        }

        private static string ComputeSha256(
            string value)
        {
            byte[] bytes =
                Encoding.UTF8.GetBytes(
                    value);

            byte[] hash =
                SHA256.HashData(
                    bytes);

            return Convert.ToHexString(
                hash);
        }

        private static TrialActivationResponse
            Success(
                SignedLicenseResponse license)
        {
            return new TrialActivationResponse
            {
                Success =
                    true,

                ErrorCode =
                    string.Empty,

                Message =
                    "The 24-hour free trial is active.",

                License =
                    license
            };
        }

        private static TrialActivationResponse
            Error(
                string errorCode,
                string message)
        {
            return new TrialActivationResponse
            {
                Success =
                    false,

                ErrorCode =
                    errorCode ?? string.Empty,

                Message =
                    message ?? string.Empty,

                License =
                    null
            };
        }
    }
}