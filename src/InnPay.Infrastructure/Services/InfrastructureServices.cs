using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using InnPay.Application.Interfaces;
using InnPay.Domain.Entities;
using InnPay.Domain.Enums;
using InnPay.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace InnPay.Infrastructure.Services;

// ─── PASSWORD HASHER ─────────────────────────────────────────────────────────
// Uses BCrypt-style PBKDF2 — no external packages required

public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public string Hash(string plainText)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(plainText), salt, Iterations, Algorithm, HashSize);

        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string plainText, string storedHash)
    {
        var parts = storedHash.Split('.');
        if (parts.Length != 2) return false;

        var salt = Convert.FromBase64String(parts[0]);
        var expectedHash = Convert.FromBase64String(parts[1]);

        var actualHash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(plainText), salt, Iterations, Algorithm, HashSize);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}

// ─── TOKEN SERVICE ───────────────────────────────────────────────────────────

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config) => _config = config;

    public string GenerateAccessToken(User user, Account account)
    {
        var jwtKey = _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT key not configured.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new Claim("accountId",   account.Id.ToString()),
            new Claim("accountType", account.AccountType.ToString()),
            new Claim("kycTier",     account.KycTier.ToString()),
            new Claim("fullName",    user.FullName)
        };

        var token = new JwtSecurityToken(
            issuer:   _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims:   claims,
            expires:  DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public RefreshToken GenerateRefreshToken(Guid userId, string? ipAddress = null)
    {
        return new RefreshToken
        {
            UserId = userId,
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedByIp = ipAddress
        };
    }

    public Guid? GetUserIdFromToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            var sub = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
            return sub is not null ? Guid.Parse(sub) : null;
        }
        catch
        {
            return null;
        }
    }
}

// ─── OTP SERVICE ─────────────────────────────────────────────────────────────

public class OtpService : IOtpService
{
    private readonly IUnitOfWork _uow;
    private readonly ISmsService _sms;

    public OtpService(IUnitOfWork uow, ISmsService sms)
    {
        _uow = uow;
        _sms = sms;
    }

    public async Task<string> GenerateAndSendOtpAsync(Guid userId, string phoneNumber, OtpPurpose purpose)
    {
        // Invalidate any existing active OTPs for this purpose
        await _uow.Otps.InvalidateAllForUserAsync(userId, purpose);

        var code = GenerateCode();

        var otp = new OtpCode
        {
            UserId = userId,
            Code = code,
            Purpose = purpose,
            PhoneNumber = phoneNumber,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            IsUsed = false
        };

        await _uow.Otps.AddAsync(otp);
        await _uow.SaveChangesAsync();

        var message = purpose switch
        {
            OtpPurpose.PhoneVerification => $"Your InnPay verification code is: {code}. Valid for 10 minutes.",
            OtpPurpose.PasswordReset     => $"Your InnPay password reset code is: {code}. Valid for 10 minutes.",
            OtpPurpose.TransactionPin    => $"Your InnPay PIN reset code is: {code}. Valid for 10 minutes.",
            _                            => $"Your InnPay code is: {code}. Valid for 10 minutes."
        };

        await _sms.SendAsync(phoneNumber, message);
        return code;
    }

    public async Task<bool> ValidateOtpAsync(Guid userId, string code, OtpPurpose purpose)
    {
        var otp = await _uow.Otps.GetActiveOtpAsync(userId, purpose);
        if (otp is null || otp.Code != code) return false;

        await _uow.Otps.MarkUsedAsync(otp.Id);
        await _uow.SaveChangesAsync();
        return true;
    }

    private static string GenerateCode()
        => RandomNumberGenerator.GetInt32(100_000, 999_999).ToString();
}

// ─── FILE STORAGE SERVICE ────────────────────────────────────────────────────
// Local disk implementation — swap for S3/Azure Blob in production

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;

    public LocalFileStorageService(IConfiguration config)
    {
        _basePath = config["FileStorage:BasePath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> UploadAsync(string base64Content, string fileName, string contentType, string folder)
    {
        var sanitizedFolder = SanitizePath(folder);
        var directory = Path.Combine(_basePath, sanitizedFolder);
        Directory.CreateDirectory(directory);

        var extension = Path.GetExtension(fileName);
        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(directory, uniqueFileName);

        var bytes = Convert.FromBase64String(base64Content);
        await File.WriteAllBytesAsync(filePath, bytes);

        // Return a relative URL path; prefix with a CDN/API base in production
        return $"/uploads/{sanitizedFolder}/{uniqueFileName}";
    }

    public Task DeleteAsync(string fileUrl)
    {
        var relativePath = fileUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(_basePath, "..", relativePath);

        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }

    private static string SanitizePath(string path)
        => string.Join(Path.DirectorySeparatorChar.ToString(),
            path.Split('/', '\\').Select(p => string.Concat(p.Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'))));
}

// ─── SMS SERVICE ABSTRACTION ─────────────────────────────────────────────────

public interface ISmsService
{
    Task SendAsync(string phoneNumber, string message);
}

/// <summary>
/// Console/log stub — replace with Termii, Twilio, or any Nigerian SMS provider.
/// </summary>
public class ConsoleSmsService : ISmsService
{
    public Task SendAsync(string phoneNumber, string message)
    {
        Console.WriteLine($"[SMS → {phoneNumber}] {message}");
        return Task.CompletedTask;
    }
}
