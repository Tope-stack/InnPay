using InnPay.Domain.Entities;
using InnPay.Domain.Interfaces;
using InnPay.Infrastructure.Persistence;
using InnPay.Infrastructure.Repositories;

namespace InnPay.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;

    public UnitOfWork(AppDbContext db)
    {
        _db = db;
        Users = new UserRepository(db);
        Accounts = new AccountRepository(db);
        KycDocuments = new KycDocumentRepository(db);
        Wallets = new WalletRepository(db);
        Otps = new OtpRepository(db);
        RefreshTokens = new RefreshTokenRepository(db);
        FxRates = new FxRateRepository(db);
        FxRateLocks = new FxRateLockRepository(db);
        FxTransactions = new FxTransactionRepository(db);
        CurrencyPairConfigs = new CurrencyPairConfigRepository(db);
    }

    public IUserRepository Users { get; }
    public IAccountRepository Accounts { get; }
    public IKycDocumentRepository KycDocuments { get; }
    public IWalletRepository Wallets { get; }
    public IOtpRepository Otps { get; }
    public IRefreshTokenRepository RefreshTokens { get; }


    // Currency module
    public IFxRateRepository FxRates { get; }
    public IFxRateLockRepository FxRateLocks { get; }
    public IFxTransactionRepository FxTransactions { get; }
    public ICurrencyPairConfigRepository CurrencyPairConfigs { get; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _db.SaveChangesAsync(cancellationToken);
}
