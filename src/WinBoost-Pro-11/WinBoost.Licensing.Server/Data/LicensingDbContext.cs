using Microsoft.EntityFrameworkCore;

namespace WinBoost.Licensing.Server.Data
{
    public sealed class LicensingDbContext
        : DbContext
    {
        public LicensingDbContext(
            DbContextOptions<LicensingDbContext> options)
            : base(options)
        {
        }

        public DbSet<PurchaseRecord> Purchases
        {
            get;
            set;
        } = null!;

        public DbSet<LicenseRecord> Licenses
        {
            get;
            set;
        } = null!;
    }
}