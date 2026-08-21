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

        public DbSet<TrialRecord> Trials
        {
            get;
            set;
        } = null!;

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(
                modelBuilder);

            modelBuilder
                .Entity<TrialRecord>()
                .HasIndex(
                    trial =>
                        new
                        {
                            trial.TrialDeviceTokenHash,
                            trial.ProductName
                        })
                .IsUnique();

            modelBuilder
                .Entity<TrialRecord>()
                .HasIndex(
                    trial =>
                        trial.LicenseId)
                .IsUnique();
        }
    }
}