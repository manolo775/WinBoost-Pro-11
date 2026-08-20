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

        public async Task<LicenseRecord>
            CreateAsync(
                LicenseRecord license,
                CancellationToken cancellationToken =
                    default)
        {
            _dbContext.Licenses.Add(
                license);

            await _dbContext
                .SaveChangesAsync(
                    cancellationToken);

            return license;
        }
    }
}