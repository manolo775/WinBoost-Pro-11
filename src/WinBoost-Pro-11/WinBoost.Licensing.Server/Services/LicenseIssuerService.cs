using System;
using System.Threading;
using System.Threading.Tasks;
using WinBoost.Licensing.Server.Data;
using WinBoost.Licensing.Server.Models;

namespace WinBoost.Licensing.Server.Services
{
    public sealed class LicenseIssuerService
    {
        private const string ProductName =
            "WinBoost Pro 11";

        private readonly LicenseRepository
            _licenseRepository;

        private readonly LicenseSigningService
            _licenseSigningService;

        public LicenseIssuerService(
            LicenseRepository licenseRepository,
            LicenseSigningService licenseSigningService)
        {
            _licenseRepository =
                licenseRepository;

            _licenseSigningService =
                licenseSigningService;
        }

        public async Task<SignedLicenseResponse>
            IssueAsync(
                PurchaseRecord purchase,
                CancellationToken cancellationToken =
                    default)
        {
            ArgumentNullException.ThrowIfNull(
                purchase);

            LicenseRecord? existingLicense =
                await _licenseRepository
                    .FindByPurchaseSessionIdAsync(
                        purchase.SessionId,
                        cancellationToken);

            LicenseRecord license;

            if (existingLicense != null)
            {
                license =
                    existingLicense;
            }
            else
            {
                DateTime activatedAtUtc =
                    DateTime.UtcNow;

                DateTime? expiresAtUtc =
                    CalculateExpiration(
                        purchase.Plan,
                        activatedAtUtc);

                license =
                    new LicenseRecord
                    {
                        LicenseId =
                            Guid.NewGuid()
                                .ToString("N"),

                        CustomerEmail =
                            purchase.CustomerEmail,

                        DeviceId =
                            purchase.DeviceId,

                        ProductName =
                            ProductName,

                        LicenseType =
                            "Licensed",

                        Plan =
                            purchase.Plan,

                        PurchaseSessionId =
                            purchase.SessionId,

                        ActivatedAtUtc =
                            activatedAtUtc,

                        ExpiresAtUtc =
                            expiresAtUtc,

                        IsRevoked =
                            false,

                        RevokedAtUtc =
                            null,

                        CreatedAtUtc =
                            activatedAtUtc,

                        UpdatedAtUtc =
                            activatedAtUtc
                    };

                license =
                    await _licenseRepository
                        .CreateAsync(
                            license,
                            cancellationToken);
            }

            if (license.IsRevoked)
            {
                throw new InvalidOperationException(
                    "The license has been revoked.");
            }

            var unsignedLicense =
                new SignedLicenseResponse
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
                        license.ActivatedAtUtc,

                    ExpiresAt =
                        license.ExpiresAtUtc,

                    DeviceId =
                        license.DeviceId,

                    Signature =
                        string.Empty
                };

            return _licenseSigningService
                .Sign(
                    unsignedLicense);
        }

        private static DateTime?
            CalculateExpiration(
                LicensePlan plan,
                DateTime activatedAtUtc)
        {
            return plan switch
            {
                LicensePlan.PromotionalLifetime =>
                    null,

                LicensePlan.OneMonth =>
                    activatedAtUtc.AddMonths(1),

                LicensePlan.ThreeMonths =>
                    activatedAtUtc.AddMonths(3),

                LicensePlan.SixMonths =>
                    activatedAtUtc.AddMonths(6),

                LicensePlan.OneYear =>
                    activatedAtUtc.AddYears(1),

                _ =>
                    throw new InvalidOperationException(
                        "The license plan cannot be issued.")
            };
        }
    }
}