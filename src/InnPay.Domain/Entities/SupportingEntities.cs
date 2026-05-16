using InnPay.Domain.Enums;

namespace InnPay.Domain.Entities;

public class KycDocument : BaseEntity
{
    public Guid AccountId { get; set; }
    public DocumentType DocumentType { get; set; }
    public string FileUrl { get; set; } = string.Empty;      // path/URL to stored file
    public string? OriginalFileName { get; set; }
    public KycStatus Status { get; set; } = KycStatus.Pending;
    public string? RejectionReason { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedByAdminId { get; set; }

    // Navigation
    public Account Account { get; set; } = null!;
}

public class Wallet : BaseEntity
{
    public Guid AccountId { get; set; }
    public WalletCurrency Currency { get; set; }
    public decimal Balance { get; set; } = 0m;
    public bool IsActive { get; set; } = false;
    public decimal DailyTransactionLimit { get; set; }

    // Navigation
    public Account Account { get; set; } = null!;
}

public class TeamMember : BaseEntity
{
    public Guid AccountId { get; set; }         // the business/corporate account
    public Guid UserId { get; set; }            // the user being added
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime InvitedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AcceptedAt { get; set; }

    // Navigation
    public Account Account { get; set; } = null!;
    public User User { get; set; } = null!;
}

public class OtpCode : BaseEntity
{
    public Guid UserId { get; set; }
    public string Code { get; set; } = string.Empty;
    public OtpPurpose Purpose { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; } = false;
    public string? PhoneNumber { get; set; }    // snapshot at time of send

    // Navigation
    public User User { get; set; } = null!;
}

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; } = false;
    public string? ReplacedByToken { get; set; }
    public string? CreatedByIp { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}
