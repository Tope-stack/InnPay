using InnPay.Domain.Entities;
using InnPay.Domain.Enums;
using InnPay.Domain.Interfaces;
using InnPay.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InnPay.Infrastructure.Repositories;

// ─── USER ─────────────────────────────────────────────────────────────────────

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;
    public UserRepository(AppDbContext db) => _db = db;

    public Task<User?> GetByIdAsync(Guid id)
        => _db.Users.FirstOrDefaultAsync(u => u.Id == id);

    public Task<User?> GetByEmailAsync(string email)
        => _db.Users.FirstOrDefaultAsync(u => u.Email == email);

    public Task<User?> GetByPhoneAsync(string phoneNumber)
        => _db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);

    public Task<bool> EmailExistsAsync(string email)
        => _db.Users.AnyAsync(u => u.Email == email);

    public Task<bool> PhoneExistsAsync(string phoneNumber)
        => _db.Users.AnyAsync(u => u.PhoneNumber == phoneNumber);

    public async Task AddAsync(User user)
        => await _db.Users.AddAsync(user);

    public Task UpdateAsync(User user)
    {
        _db.Users.Update(user);
        return Task.CompletedTask;
    }
}

// ─── ACCOUNT ──────────────────────────────────────────────────────────────────

public class AccountRepository : IAccountRepository
{
    private readonly AppDbContext _db;
    public AccountRepository(AppDbContext db) => _db = db;

    public Task<Account?> GetByIdAsync(Guid id)
        => _db.Accounts
              .Include(a => a.Wallets)
              .Include(a => a.KycDocuments)
              .FirstOrDefaultAsync(a => a.Id == id);

    public Task<Account?> GetByUserIdAsync(Guid userId)
        => _db.Accounts
              .Include(a => a.Wallets)
              .FirstOrDefaultAsync(a => a.UserId == userId);

    public async Task<IEnumerable<Account>> GetAllByUserIdAsync(Guid userId)
        => await _db.Accounts
                    .Include(a => a.Wallets)
                    .Where(a => a.UserId == userId)
                    .ToListAsync();

    public async Task AddAsync(Account account)
        => await _db.Accounts.AddAsync(account);

    public Task UpdateAsync(Account account)
    {
        _db.Accounts.Update(account);
        return Task.CompletedTask;
    }
}

// ─── KYC DOCUMENT ─────────────────────────────────────────────────────────────

public class KycDocumentRepository : IKycDocumentRepository
{
    private readonly AppDbContext _db;
    public KycDocumentRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<KycDocument>> GetByAccountIdAsync(Guid accountId)
        => await _db.KycDocuments
                    .Where(d => d.AccountId == accountId)
                    .OrderByDescending(d => d.CreatedAt)
                    .ToListAsync();

    public Task<KycDocument?> GetByIdAsync(Guid id)
        => _db.KycDocuments.FirstOrDefaultAsync(d => d.Id == id);

    public async Task AddAsync(KycDocument document)
        => await _db.KycDocuments.AddAsync(document);

    public Task UpdateAsync(KycDocument document)
    {
        _db.KycDocuments.Update(document);
        return Task.CompletedTask;
    }
}

// ─── WALLET ───────────────────────────────────────────────────────────────────



// ─── OTP ──────────────────────────────────────────────────────────────────────

public class OtpRepository : IOtpRepository
{
    private readonly AppDbContext _db;
    public OtpRepository(AppDbContext db) => _db = db;

    public Task<OtpCode?> GetActiveOtpAsync(Guid userId, OtpPurpose purpose)
        => _db.OtpCodes.FirstOrDefaultAsync(o =>
            o.UserId == userId &&
            o.Purpose == purpose &&
            !o.IsUsed &&
            o.ExpiresAt > DateTime.UtcNow);

    public async Task AddAsync(OtpCode otp)
        => await _db.OtpCodes.AddAsync(otp);

    public async Task MarkUsedAsync(Guid otpId)
    {
        var otp = await _db.OtpCodes.FindAsync(otpId);
        if (otp is not null)
        {
            otp.IsUsed = true;
            otp.UpdatedAt = DateTime.UtcNow;
        }
    }

    public async Task InvalidateAllForUserAsync(Guid userId, OtpPurpose purpose)
    {
        var otps = await _db.OtpCodes
            .Where(o => o.UserId == userId && o.Purpose == purpose && !o.IsUsed)
            .ToListAsync();

        foreach (var otp in otps)
        {
            otp.IsUsed = true;
            otp.UpdatedAt = DateTime.UtcNow;
        }
    }
}

// ─── REFRESH TOKEN ────────────────────────────────────────────────────────────

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _db;
    public RefreshTokenRepository(AppDbContext db) => _db = db;

    public Task<RefreshToken?> GetByTokenAsync(string token)
        => _db.RefreshTokens.FirstOrDefaultAsync(r => r.Token == token);

    public async Task AddAsync(RefreshToken token)
        => await _db.RefreshTokens.AddAsync(token);

    public Task UpdateAsync(RefreshToken token)
    {
        _db.RefreshTokens.Update(token);
        return Task.CompletedTask;
    }

    public async Task RevokeAllForUserAsync(Guid userId)
    {
        var tokens = await _db.RefreshTokens
            .Where(r => r.UserId == userId && !r.IsRevoked)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.UpdatedAt = DateTime.UtcNow;
        }
    }
}
