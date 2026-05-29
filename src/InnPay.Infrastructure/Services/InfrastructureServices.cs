using InnPay.Application.Interfaces;
using InnPay.Domain.Entities;
using InnPay.Domain.Enums;
using InnPay.Domain.Interfaces;
using InnPay.Infrastructure.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MimeKit;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

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
    private readonly IEmailService _email;

    public OtpService(IUnitOfWork uow, ISmsService sms, IEmailService email)
    {
        _uow = uow;
        _sms = sms;
        _email = email;
    }

    public async Task<string> GenerateAndSendOtpAsync(Guid userId, string phoneNumber, string email, OtpPurpose purpose)
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

        var (smsBody, emailSubject, emailBody) = purpose switch
        {
            OtpPurpose.PhoneVerification => (
                $"Your InnPay verification code is: {code}. Valid for 10 minutes.",
                "Verify Your InnPay Account",
                BuildEmailTemplate("Phone Verification", code)),

            OtpPurpose.PasswordReset => (
                $"Your InnPay password reset code is: {code}. Valid for 10 minutes.",
                "InnPay Password Reset Code",
                BuildEmailTemplate("Password Reset", code)),

            OtpPurpose.TransactionPin => (
                $"Your InnPay PIN reset code is: {code}. Valid for 10 minutes.",
                "InnPay PIN Reset Code",
                BuildEmailTemplate("PIN Reset", code)),

            _ => (
                $"Your InnPay code is: {code}. Valid for 10 minutes.",
                "Your InnPay Code",
                BuildEmailTemplate("Verification", code))
        };

        // Fire both — don't let one failure block the other
        await Task.WhenAll(
            _sms.SendAsync(phoneNumber, smsBody),
            _email.SendAsync(email, emailSubject, emailBody)
        );

        return code;
    }

    private static string BuildEmailTemplate(string purposeLabel, string code) => $"""
    <div style="font-family:Arial,sans-serif;max-width:480px;margin:auto;padding:32px;border:1px solid #e5e7eb;border-radius:8px;">
        <h2 style="color:#1a1a2e;">InnPay — {purposeLabel}</h2>
        <p style="color:#4b5563;">Use the code below to complete your request. It expires in <strong>10 minutes</strong>.</p>
        <div style="font-size:36px;font-weight:bold;letter-spacing:8px;color:#4f46e5;text-align:center;padding:24px 0;">
            {code}
        </div>
        <p style="color:#9ca3af;font-size:12px;">If you did not request this, please ignore this email or contact support.</p>
    </div>
    """;

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

public interface IEmailService
{
    Task SendAsync(string toEmail, string subject, string body);
}

public class ZohoEmailService : IEmailService
{
    private readonly ZohoSmtpSettings _settings;
    private readonly ILogger<ZohoEmailService> _logger;

    public ZohoEmailService(IOptions<ZohoSmtpSettings> settings, ILogger<ZohoEmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string body)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;

        message.Body = new BodyBuilder
        {
            HtmlBody = body
        }.ToMessageBody();

        using var client = new SmtpClient();

        await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_settings.Username, _settings.Password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);

        _logger.LogInformation("Email sent to {Email} with subject '{Subject}'", toEmail, subject);
    }
}
