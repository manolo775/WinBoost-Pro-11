using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace WinBoost.Licensing.Server.Data
{
    public sealed class LicenseRepository
    {
        private readonly LicensingDbContext
            _dbContext;

        public LicenseRepository(
            LicensingDbContext dbContext)
        {
            _dbContext =
                dbContext;
        }

        public Task<LicenseRecord?>
            FindByPurchaseSessionIdAsync(
                string purchaseSessionId,
                CancellationToken cancellationToken =
                    default)
        {
            return _dbContext
                .Licenses
                .FirstOrDefaultAsync(
                    license =>
                        license.PurchaseSessionId ==
                            purchaseSessionId,
                    cancellationToken);
        }

        public Task<LicenseRecord?>
            FindByLicenseIdAsync(
                string licenseId,
                CancellationToken cancellationToken =
                    default)
        {
            return _dbContext
                .Licenses
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    license =>
                        license.LicenseId ==
                            licenseId,
                    cancellationToken);
        }

        public Task<LicenseRecord?>
            FindActiveByDeviceAsync(
                string deviceId,
                string productName,
                DateTime nowUtc,
                CancellationToken cancellationToken =
                    default)
        {
            return _dbContext
                .Licenses
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    license =>
                        license.DeviceId ==
                            deviceId &&
                        license.ProductName ==
                            productName &&
                        !license.IsRevoked &&
                        (
                            license.ExpiresAtUtc == null ||
                            license.ExpiresAtUtc >
                                nowUtc
                        ),
                    cancellationToken);
        }

        public async Task<LicenseRecord>
            CreateAsync(
                LicenseRecord license,
                CancellationToken cancellationToken =
                    default)
        {
            if (license == null)
            {
                throw new ArgumentNullException(
                    nameof(license));
            }

            _dbContext.Licenses.Add(
                license);

            await _dbContext
                .SaveChangesAsync(
                    cancellationToken);

            return license;
        }
    }
}