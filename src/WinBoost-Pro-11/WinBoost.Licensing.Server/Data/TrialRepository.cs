using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace WinBoost.Licensing.Server.Data
{
    public sealed class TrialRepository
    {
        private readonly LicensingDbContext
            _dbContext;

        public TrialRepository(
            LicensingDbContext dbContext)
        {
            _dbContext =
                dbContext;
        }

        public Task<TrialRecord?>
            FindByDeviceTokenHashAsync(
                string trialDeviceTokenHash,
                string productName,
                CancellationToken cancellationToken =
                    default)
        {
            return _dbContext
                .Trials
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    trial =>
                        trial.TrialDeviceTokenHash ==
                            trialDeviceTokenHash &&
                        trial.ProductName ==
                            productName,
                    cancellationToken);
        }

        public Task<TrialRecord?>
            FindByLicenseIdAsync(
                string licenseId,
                CancellationToken cancellationToken =
                    default)
        {
            return _dbContext
                .Trials
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    trial =>
                        trial.LicenseId ==
                            licenseId,
                    cancellationToken);
        }

        public async Task<TrialRecord>
            CreateWithLicenseAsync(
                TrialRecord trial,
                LicenseRecord license,
                CancellationToken cancellationToken =
                    default)
        {
            ArgumentNullException.ThrowIfNull(
                trial);

            ArgumentNullException.ThrowIfNull(
                license);

            await using var transaction =
                await _dbContext.Database
                    .BeginTransactionAsync(
                        cancellationToken);

            try
            {
                _dbContext.Licenses.Add(
                    license);

                _dbContext.Trials.Add(
                    trial);

                await _dbContext
                    .SaveChangesAsync(
                        cancellationToken);

                await transaction
                    .CommitAsync(
                        cancellationToken);

                return trial;
            }
            catch
            {
                await transaction
                    .RollbackAsync(
                        cancellationToken);

                throw;
            }
        }
    }
}