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

    // Payment module (6.1)
    public DbSet<Payment> Payments => Set<Payment>();

    // Transfer module (6.3 internal, 6.4 external)
    public DbSet<InternalTransfer> InternalTransfers => Set<InternalTransfer>();
    public DbSet<TransferSchedule> TransferSchedules => Set<TransferSchedule>();
    public DbSet<SavedBeneficiary> SavedBeneficiaries => Set<SavedBeneficiary>();
    public DbSet<ExternalTransfer> ExternalTransfers => Set<ExternalTransfer>();
    public DbSet<SavedBankAccount> SavedBankAccounts => Set<SavedBankAccount>();

    // Bills module (6.2)
    public DbSet<BillPayment> BillPayments => Set<BillPayment>();

    // Virtual accounts & withdrawals (6.5, 6.6)
    public DbSet<VirtualAccount> VirtualAccounts => Set<VirtualAccount>();
    public DbSet<Withdrawal> Withdrawals => Set<Withdrawal>();

    // Virtual card module (6.7)
    public DbSet<VirtualCard> VirtualCards => Set<VirtualCard>();
    public DbSet<CardTransaction> CardTransactions => Set<CardTransaction>();

    // Gift card module (6.8)
    public DbSet<GiftCardPurchase> GiftCardPurchases => Set<GiftCardPurchase>();

    // Flight module (6.9)
    public DbSet<FlightBooking> FlightBookings => Set<FlightBooking>();
    public DbSet<FlightPassenger> FlightPassengers => Set<FlightPassenger>();

    // Bet funding module (6.10)
    public DbSet<BetFunding> BetFundings => Set<BetFunding>();

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
