using InnPay.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InnPay.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<KycDocument> KycDocuments => Set<KycDocument>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    // Currency module
    public DbSet<FxRate> FxRates => Set<FxRate>();
    public DbSet<FxRateLock> FxRateLocks => Set<FxRateLock>();
    public DbSet<FxTransaction> FxTransactions => Set<FxTransaction>();
    public DbSet<CurrencyPairConfig> CurrencyPairConfigs => Set<CurrencyPairConfig>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Auto-set UpdatedAt on modified entities
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
