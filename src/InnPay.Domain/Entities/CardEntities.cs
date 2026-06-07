using InnPay.Domain.Enums;

namespace InnPay.Domain.Entities;

/// <summary>
/// USD virtual card issued via Sudo Africa / Union54 / Stripe Issuing.
/// PAN and CVV are AES-256 encrypted at rest; decrypted only on explicit user request.
/// </summary>
public class VirtualCard : BaseEntity
{
    public Guid AccountId { get; set; }

    /// <summary>Must be the USD wallet for this account.</summary>
    public Guid WalletId { get; set; }

    /// <summary>AES-256 encrypted full 16-digit PAN.</summary>
    public string PanEncrypted { get; set; } = string.Empty;

    /// <summary>Last 4 digits stored in plain text for display purposes.</summary>
    public string Last4 { get; set; } = string.Empty;

    /// <summary>Card expiry in MM/YY format — not sensitive, stored plain.</summary>
    public string Expiry { get; set; } = string.Empty;

    /// <summary>AES-256 encrypted CVV.</summary>
    public string CvvEncrypted { get; set; } = string.Empty;

    public VirtualCardStatus Status { get; set; } = VirtualCardStatus.Active;

    /// <summary>Provider card identifier used for lifecycle management (freeze/delete/limits).</summary>
    public string ProviderCardId { get; set; } = string.Empty;

    public decimal SpendingLimitPerTransaction { get; set; } = 0m;   // 0 = no limit
    public decimal SpendingLimitMonthly { get; set; } = 0m;          // 0 = no limit

    public string? TerminationReason { get; set; }

    // Navigation
    public Account Account { get; set; } = null!;
    public Wallet Wallet { get; set; } = null!;
    public ICollection<CardTransaction> Transactions { get; set; } = new List<CardTransaction>();
}

/// <summary>
/// Real-time card transaction pushed via webhook from the card issuer.
/// </summary>
public class CardTransaction : BaseEntity
{
    public Guid VirtualCardId { get; set; }

    public string MerchantName { get; set; } = string.Empty;
    public string MerchantCategory { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public WalletCurrency Currency { get; set; }
    public string ProviderTransactionId { get; set; } = string.Empty;
    public bool IsDeclined { get; set; } = false;
    public string? DeclineReason { get; set; }
    public DateTime TransactedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public VirtualCard VirtualCard { get; set; } = null!;
}
