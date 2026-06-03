using InnPay.Domain.Entities;
using InnPay.Domain.Enums;

namespace InnPay.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByPhoneAsync(string phoneNumber);
    Task<bool> EmailExistsAsync(string email);
    Task<bool> PhoneExistsAsync(string phoneNumber);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
}

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(Guid id);
    Task<Account?> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<Account>> GetAllByUserIdAsync(Guid userId);
    Task AddAsync(Account account);
    Task UpdateAsync(Account account);
}

public interface IKycDocumentRepository
{
    Task<IEnumerable<KycDocument>> GetByAccountIdAsync(Guid accountId);
    Task<KycDocument?> GetByIdAsync(Guid id);
    Task AddAsync(KycDocument document);
    Task UpdateAsync(KycDocument document);
}

public interface IWalletRepository
{
    Task<Wallet?> GetByIdAsync(Guid id);
    Task<Wallet?> GetByAccountAndCurrencyAsync(Guid accountId, WalletCurrency currency);
    Task<IEnumerable<Wallet>> GetByAccountIdAsync(Guid accountId);
    Task AddAsync(Wallet wallet);
    Task UpdateAsync(Wallet wallet);
}

public interface IFxRateRepository
{
    /// <summary>Returns the most recently fetched non-expired rate for this pair.</summary>
    Task<FxRate?> GetLatestAsync(WalletCurrency from, WalletCurrency to);

    /// <summary>Returns all latest rates keyed by pair — used by GET /fx/rates.</summary>
    Task<IEnumerable<FxRate>> GetAllLatestAsync();

    Task AddAsync(FxRate rate);

    /// <summary>Bulk insert a fresh set of rates fetched from the provider.</summary>
    Task BulkAddAsync(IEnumerable<FxRate> rates);
}

public interface IFxRateLockRepository
{
    Task<FxRateLock?> GetByIdAsync(Guid lockId);
    Task<FxRateLock?> GetActiveByAccountAsync(Guid accountId, WalletCurrency from, WalletCurrency to);
    Task AddAsync(FxRateLock rateLock);
    Task UpdateAsync(FxRateLock rateLock);
}

public interface IFxTransactionRepository
{
    Task<FxTransaction?> GetByIdAsync(Guid id);
    Task<FxTransaction?> GetByReferenceAsync(string reference);
    Task<IEnumerable<FxTransaction>> GetByAccountIdAsync(Guid accountId, int page = 1, int pageSize = 20);
    Task AddAsync(FxTransaction transaction);
    Task UpdateAsync(FxTransaction transaction);
}

public interface ICurrencyPairConfigRepository
{
    Task<CurrencyPairConfig?> GetAsync(WalletCurrency from, WalletCurrency to);
    Task<IEnumerable<CurrencyPairConfig>> GetAllActiveAsync();
    Task AddAsync(CurrencyPairConfig config);
    Task UpdateAsync(CurrencyPairConfig config);
}


public interface IOtpRepository
{
    Task<OtpCode?> GetActiveOtpAsync(Guid userId, OtpPurpose purpose);
    Task AddAsync(OtpCode otp);
    Task MarkUsedAsync(Guid otpId);
    Task InvalidateAllForUserAsync(Guid userId, OtpPurpose purpose);
}

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task AddAsync(RefreshToken token);
    Task UpdateAsync(RefreshToken token);
    Task RevokeAllForUserAsync(Guid userId);
}

public interface IUnitOfWork
{
    IUserRepository Users { get; }
    IAccountRepository Accounts { get; }
    IKycDocumentRepository KycDocuments { get; }
    IWalletRepository Wallets { get; }
    IOtpRepository Otps { get; }
    IRefreshTokenRepository RefreshTokens { get; }

    // Currency module
    IFxRateRepository FxRates { get; }
    IFxRateLockRepository FxRateLocks { get; }
    IFxTransactionRepository FxTransactions { get; }
    ICurrencyPairConfigRepository CurrencyPairConfigs { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
