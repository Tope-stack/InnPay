using InnPay.Domain.Enums;

namespace InnPay.Domain.Entities;

public class KycDocument : BaseEntity
{
    public Guid AccountId { get; set; }
    public DocumentType DocumentType { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string? OriginalFileName { get; set; }
    public KycStatus Status { get; set; } = KycStatus.Pending;
    public string? RejectionReason { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedByAdminId { get; set; }

    public Account Account { get; set; } = null!;
}

public class Wallet : BaseEntity
{
    public Guid AccountId { get; set; }
    public WalletCurrency Currency { get; set; }

    public CurrencyAccountModel Model { get; set; } = CurrencyAccountModel.WalletModel;

    public decimal Balance { get; set; } = 0m;
    public decimal AvailableBalance { get; set; } = 0m;
    public decimal ReservedAmount { get; set; } = 0m;

    public WalletStatus Status { get; set; } = WalletStatus.Active;
    public bool IsActive { get; set; } = false;
    public decimal DailyTransactionLimit { get; set; }

    public string? WalletReference { get; set; }
    public string? Iban { get; set; }
    public string? SortCode { get; set; }
    public string? UkAccountNumber { get; set; }
    public string? SwiftCode { get; set; }
    public string? BankName { get; set; }

    public Account Account { get; set; } = null!;
    public ICollection<FxTransaction> FxTransactionsFrom { get; set; } = new List<FxTransaction>();
    public ICollection<FxTransaction> FxTransactionsTo { get; set; } = new List<FxTransaction>();
}

public class TeamMember : BaseEntity
{
    public Guid AccountId { get; set; }
    public Guid UserId { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime InvitedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AcceptedAt { get; set; }

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
    public string? PhoneNumber { get; set; }

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

    public User User { get; set; } = null!;
}
