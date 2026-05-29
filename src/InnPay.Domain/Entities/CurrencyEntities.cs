using InnPay.Domain.Enums;

namespace InnPay.Domain.Entities;

/// <summary>
/// Snapshot of a live FX rate for a currency pair, refreshed every 60 seconds.
/// Stored so we have an audit trail of the rate used for each conversion.
/// </summary>
public class FxRate : BaseEntity
{
    public WalletCurrency FromCurrency { get; set; }
    public WalletCurrency ToCurrency { get; set; }
    public decimal MidRate { get; set; }
    public decimal CustomerRate { get; set; }
    public decimal SpreadBps { get; set; }
    public FxRateSource Source { get; set; }
    public DateTime FetchedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// 30-second rate lock created when a user initiates a conversion.
/// </summary>
public class FxRateLock : BaseEntity
{
    public Guid AccountId { get; set; }
    public Guid FxRateId { get; set; }

    public WalletCurrency FromCurrency { get; set; }
    public WalletCurrency ToCurrency { get; set; }

    public decimal SourceAmount { get; set; }
    public decimal LockedCustomerRate { get; set; }
    public decimal EstimatedConvertedAmount { get; set; }
    public decimal EstimatedFeeAmount { get; set; }

    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; } = false;
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    public Account Account { get; set; } = null!;
    public FxRate FxRate { get; set; } = null!;
}

/// <summary>
/// Completed (or failed) FX conversion record.
/// </summary>
public class FxTransaction : BaseEntity
{
    public Guid AccountId { get; set; }
    public Guid? RateLockId { get; set; }

    public Guid FromWalletId { get; set; }
    public Guid ToWalletId { get; set; }

    public WalletCurrency FromCurrency { get; set; }
    public WalletCurrency ToCurrency { get; set; }

    public decimal SourceAmount { get; set; }
    public decimal AppliedRate { get; set; }
    public decimal GrossConvertedAmount { get; set; }
    public decimal FeeAmount { get; set; }
    public WalletCurrency FeeCurrency { get; set; }
    public decimal NetConvertedAmount { get; set; }

    public FxConversionStatus Status { get; set; } = FxConversionStatus.Pending;
    public string? FailureReason { get; set; }
    public string Reference { get; set; } = string.Empty;
    public DateTime ConvertedAt { get; set; } = DateTime.UtcNow;

    public Account Account { get; set; } = null!;
    public Wallet FromWallet { get; set; } = null!;
    public Wallet ToWallet { get; set; } = null!;
    public FxRateLock? RateLock { get; set; }
}

/// <summary>
/// Admin-configurable spread and fee per currency pair.
/// </summary>
public class CurrencyPairConfig : BaseEntity
{
    public WalletCurrency FromCurrency { get; set; }
    public WalletCurrency ToCurrency { get; set; }
    public decimal SpreadBps { get; set; }
    public decimal FlatFee { get; set; }
    public decimal PercentageFee { get; set; }
    public decimal MinSourceAmount { get; set; }
    public decimal MaxSourceAmount { get; set; }
    public bool IsActive { get; set; } = true;

    public string PairLabel => $"{FromCurrency} → {ToCurrency}";
}
