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
    }

    public IUserRepository Users { get; }
    public IAccountRepository Accounts { get; }
    public IKycDocumentRepository KycDocuments { get; }
    public IWalletRepository Wallets { get; }
    public IOtpRepository Otps { get; }
    public IRefreshTokenRepository RefreshTokens { get; }

    public Task<int> SaveChangesAsync() => _db.SaveChangesAsync();
}
