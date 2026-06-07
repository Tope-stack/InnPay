using InnPay.Domain.Enums;

namespace InnPay.Domain.Entities;

/// <summary>
/// Bet account funding — supports Bet9ja, Sportybet, 1xBet.
/// Flagged for regulatory compliance per CBN guidelines.
/// </summary>
public class BetFunding : BaseEntity
{
    public Guid AccountId { get; set; }
    public Guid WalletId { get; set; }

    public BettingPlatform Platform { get; set; }

    /// <summary>The user's username / ID on the betting platform.</summary>
    public string BettingUserId { get; set; } = string.Empty;

    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public string Reference { get; set; } = string.Empty;
    public string? ProviderReference { get; set; }
    public string? FailureReason { get; set; }

    /// <summary>Flagged when transaction exceeds platform or CBN limits — requires compliance review.</summary>
    public bool IsFlagged { get; set; } = false;

    // Navigation
    public Account Account { get; set; } = null!;
    public Wallet Wallet { get; set; } = null!;
}
