using InnPay.Domain.Enums;

namespace InnPay.Domain.Entities;

/// <summary>
/// Gift card purchase via Reloadly API.
/// Redemption code is AES-256 encrypted at rest; decrypted only on user request.
/// </summary>
public class GiftCardPurchase : BaseEntity
{
    public Guid AccountId { get; set; }
    public Guid WalletId { get; set; }

    public string BrandName { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;

    public decimal DenominationUsd { get; set; }
    public decimal AmountCharged { get; set; }      // in wallet currency
    public WalletCurrency WalletCurrency { get; set; }

    public GiftCardStatus Status { get; set; } = GiftCardStatus.Purchased;

    /// <summary>AES-256 encrypted redemption code.</summary>
    public string RedemptionCodeEncrypted { get; set; } = string.Empty;

    /// <summary>Redemption PIN (some brands require it) — stored plain as it is low-risk.</summary>
    public string? RedemptionPin { get; set; }

    public string Reference { get; set; } = string.Empty;
    public string? ReloadlyOrderId { get; set; }
    public string? FailureReason { get; set; }

    // Navigation
    public Account Account { get; set; } = null!;
    public Wallet Wallet { get; set; } = null!;
}
