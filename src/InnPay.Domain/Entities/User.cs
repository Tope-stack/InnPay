using InnPay.Domain.Enums;

namespace InnPay.Domain.Entities;

public class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string TransactionPinHash { get; set; } = string.Empty;
    public bool IsPhoneVerified { get; set; } = false;
    public bool IsEmailVerified { get; set; } = false;
    public AccountStatus Status { get; set; } = AccountStatus.Pending;

    // Navigation
    public ICollection<Account> Accounts { get; set; } = new List<Account>();
    public ICollection<OtpCode> OtpCodes { get; set; } = new List<OtpCode>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
