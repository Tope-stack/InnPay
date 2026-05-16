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
    Task<Wallet?> GetByAccountAndCurrencyAsync(Guid accountId, WalletCurrency currency);
    Task<IEnumerable<Wallet>> GetByAccountIdAsync(Guid accountId);
    Task AddAsync(Wallet wallet);
    Task UpdateAsync(Wallet wallet);
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
    Task<int> SaveChangesAsync();
}
