using System;
using System.Threading;
using System.Threading.Tasks;
using WinBoost.Licensing.Server.Data;
using WinBoost.Licensing.Server.Models;

namespace WinBoost.Licensing.Server.Services
{
    public sealed class LicenseRevocationCheckService
    {
        private const string ProductName =
            "WinBoost Pro 11";

        private readonly LicenseRepository
            _licenseRepository;

        public LicenseRevocationCheckService(
            LicenseRepository licenseRepository)
        {
            _licenseRepository =
                licenseRepository;
        }

        public async Task<LicenseRevocationCheckResponse>
            CheckAsync(
                LicenseRevocationCheckRequest request,
                CancellationToken cancellationToken =
                    default)
        {
            if (request == null)
            {
                return Error(
                    "INVALID_REQUEST",
                    "The revocation check request is invalid.");
            }

            string licenseId =
                request.LicenseId?.Trim()
                ?? string.Empty;

            string deviceId =
                request.DeviceId?.Trim()
                ?? string.Empty;

            string productName =
                request.ProductName?.Trim()
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(
                    licenseId) ||
                string.IsNullOrWhiteSpace(
                    deviceId) ||
                string.IsNullOrWhiteSpace(
                    productName))
            {
                return Error(
                    "INVALID_REQUEST",
                    "The revocation check request is incomplete.");
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

            LicenseRecord? license =
                await _licenseRepository
                    .FindByLicenseIdAsync(
                        licenseId,
                        cancellationToken);

            if (license == null)
            {
                return Error(
                    "LICENSE_NOT_FOUND",
                    "The license was not found.");
            }

            if (!string.Equals(
                    license.DeviceId,
                    deviceId,
                    StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(
                    license.ProductName,
                    ProductName,
                    StringComparison.Ordinal))
            {
                return Error(
                    "LICENSE_MISMATCH",
                    "The license does not match this device or product.");
            }

            if (license.IsRevoked)
            {
                return new LicenseRevocationCheckResponse
                {
                    Success =
                        true,

                    IsRevoked =
                        true,

                    ErrorCode =
                        string.Empty,

                    Message =
                        "The license has been revoked."
                };
            }

            return new LicenseRevocationCheckResponse
            {
                Success =
                    true,

                IsRevoked =
                    false,

                ErrorCode =
                    string.Empty,

                Message =
                    "The license has not been revoked."
            };
        }

        private static LicenseRevocationCheckResponse
            Error(
                string errorCode,
                string message)
        {
            return new LicenseRevocationCheckResponse
            {
                Success =
                    false,

                IsRevoked =
                    false,

                ErrorCode =
                    errorCode ?? string.Empty,

                Message =
                    message ?? string.Empty
            };
        }
    }
}