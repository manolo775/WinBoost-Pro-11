using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WinBoost.Licensing.Server.Models;

namespace WinBoost.Licensing.Server.Data
{
    public sealed class PurchaseRepository
    {
        private readonly LicensingDbContext
            _dbContext;

        public PurchaseRepository(
            LicensingDbContext dbContext)
        {
            _dbContext =
                dbContext;
        }

        public async Task<PurchaseRecord>
            CreatePendingAsync(
                string sessionId,
                string customerEmail,
                string deviceId,
                string productName,
                LicensePlan plan,
                string paymentProvider,
                CancellationToken cancellationToken =
                    default)
        {
            DateTime nowUtc =
                DateTime.UtcNow;

            var purchase =
                new PurchaseRecord
                {
                    SessionId =
                        sessionId,

                    CustomerEmail =
                        customerEmail,

                    DeviceId =
                        deviceId,

                    ProductName =
                        productName,

                    Plan =
                        plan,

                    Status =
                        "Pending",

                    PaymentProvider =
                        paymentProvider,

                    ProviderTransactionId =
                        string.Empty,

                    CreatedAtUtc =
                        nowUtc,

                    UpdatedAtUtc =
                        nowUtc
                };

            _dbContext.Purchases.Add(
                purchase);

            await _dbContext
                .SaveChangesAsync(
                    cancellationToken);

            return purchase;
        }

        public Task<PurchaseRecord?>
            FindBySessionIdAsync(
                string sessionId,
                CancellationToken cancellationToken =
                    default)
        {
            return _dbContext
                .Purchases
                .FirstOrDefaultAsync(
                    purchase =>
                        purchase.SessionId ==
                            sessionId,
                    cancellationToken);
        }
    }
}