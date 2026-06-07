using InnPay.Domain.Enums;

namespace InnPay.Domain.Entities;

/// <summary>
/// Universal payment record — one row per payment attempt regardless of provider.
/// Idempotency is enforced via the unique IdempotencyKey.
/// State machine: Pending → Processing → Completed | Failed | Reversed
/// </summary>
public class Payment : BaseEntity
{
    public Guid AccountId { get; set; }
    public Guid WalletId { get; set; }

    public decimal Amount { get; set; }
    public WalletCurrency Currency { get; set; }

    public PaymentProvider Provider { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    /// <summary>Unique client-supplied key — prevents duplicate charges.</summary>
    public string IdempotencyKey { get; set; } = string.Empty;

    /// <summary>Internal InnPay reference (e.g. INN-PAY-xxxxxxxx).</summary>
    public string Reference { get; set; } = string.Empty;

    /// <summary>Provider-side reference / checkout reference returned after initiation.</summary>
    public string? ProviderReference { get; set; }

    /// <summary>Client secret or checkout URL returned to Flutter for SDK handoff.</summary>
    public string? ClientSecret { get; set; }

    public string? FailureReason { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Navigation
    public Account Account { get; set; } = null!;
    public Wallet Wallet { get; set; } = null!;
}
