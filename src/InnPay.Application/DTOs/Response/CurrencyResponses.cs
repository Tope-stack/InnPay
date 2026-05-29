using InnPay.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnPay.Application.DTOs.Response
{
    // ─── WALLET ──────────────────────────────────────────────────────────────────

    public class WalletDetailResponse
    {
        public Guid WalletId { get; set; }
        public Guid AccountId { get; set; }
        public WalletCurrency Currency { get; set; }
        public string CurrencySymbol { get; set; } = string.Empty;
        public CurrencyAccountModel Model { get; set; }
        public WalletStatus Status { get; set; }

        // Balances
        public decimal Balance { get; set; }
        public decimal AvailableBalance { get; set; }
        public decimal ReservedAmount { get; set; }
        public decimal DailyTransactionLimit { get; set; }

        // Model A: wallet reference
        public string? WalletReference { get; set; }

        // Model B: dedicated account details
        public StandaloneAccountDetails? StandaloneAccount { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class StandaloneAccountDetails
    {
        public string? Iban { get; set; }
        public string? SortCode { get; set; }
        public string? UkAccountNumber { get; set; }
        public string? SwiftCode { get; set; }
        public string? BankName { get; set; }
    }

    public class AccountWalletsResponse
    {
        public Guid AccountId { get; set; }
        public AccountType AccountType { get; set; }
        public List<WalletDetailResponse> Wallets { get; set; } = new();
        public decimal TotalBalanceInNgn { get; set; }   // approximate, converted at current rates
    }

    // ─── FX RATES ────────────────────────────────────────────────────────────────

    public class FxRateResponse
    {
        public WalletCurrency FromCurrency { get; set; }
        public WalletCurrency ToCurrency { get; set; }
        public string Pair { get; set; } = string.Empty;        // e.g. "USD/NGN"
        public decimal MidRate { get; set; }
        public decimal CustomerRate { get; set; }
        public decimal SpreadBps { get; set; }
        public DateTime FetchedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsStale { get; set; }   // true if rate is older than 60 s
    }

    public class AllFxRatesResponse
    {
        public DateTime GeneratedAt { get; set; }
        public bool HasStaleRates { get; set; }
        public List<FxRateResponse> Rates { get; set; } = new();
    }

    // ─── FX CONVERSION ───────────────────────────────────────────────────────────

    /// <summary>Returned after Step 1 — InitiateConversion. Shows preview + rate lock.</summary>
    public class ConversionPreviewResponse
    {
        public Guid RateLockId { get; set; }
        public Guid AccountId { get; set; }

        public WalletCurrency FromCurrency { get; set; }
        public WalletCurrency ToCurrency { get; set; }

        public decimal SourceAmount { get; set; }
        public decimal LockedCustomerRate { get; set; }
        public decimal GrossConvertedAmount { get; set; }
        public decimal FeeAmount { get; set; }
        public string FeeCurrency { get; set; } = string.Empty;
        public decimal NetConvertedAmount { get; set; }

        /// <summary>Rate breakdown shown to the user before confirmation.</summary>
        public string RateSummary { get; set; } = string.Empty;    // e.g. "1 USD = 1,582.45 NGN"

        /// <summary>UTC timestamp when this rate lock expires (30 seconds from initiation).</summary>
        public DateTime LockExpiresAt { get; set; }

        /// <summary>Seconds remaining on the lock — client uses this for countdown timer.</summary>
        public int SecondsRemaining => Math.Max(0, (int)(LockExpiresAt - DateTime.UtcNow).TotalSeconds);
    }

    /// <summary>Returned after Step 2 — ConfirmConversion.</summary>
    public class ConversionResultResponse
    {
        public Guid FxTransactionId { get; set; }
        public string Reference { get; set; } = string.Empty;
        public FxConversionStatus Status { get; set; }

        public WalletCurrency FromCurrency { get; set; }
        public WalletCurrency ToCurrency { get; set; }
        public decimal SourceAmountDebited { get; set; }
        public decimal NetAmountCredited { get; set; }
        public decimal FeeCharged { get; set; }
        public decimal AppliedRate { get; set; }

        public decimal NewFromWalletBalance { get; set; }
        public decimal NewToWalletBalance { get; set; }

        public DateTime ConvertedAt { get; set; }
        public string? FailureReason { get; set; }
    }

    /// <summary>Single FX transaction history entry.</summary>
    public class FxTransactionResponse
    {
        public Guid Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public WalletCurrency FromCurrency { get; set; }
        public WalletCurrency ToCurrency { get; set; }
        public decimal SourceAmount { get; set; }
        public decimal NetConvertedAmount { get; set; }
        public decimal FeeAmount { get; set; }
        public decimal AppliedRate { get; set; }
        public FxConversionStatus Status { get; set; }
        public DateTime ConvertedAt { get; set; }
    }

    public class PagedFxTransactionsResponse
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public List<FxTransactionResponse> Items { get; set; } = new();
    }

    // ─── ADMIN: PAIR CONFIG ───────────────────────────────────────────────────────

    public class CurrencyPairConfigResponse
    {
        public Guid Id { get; set; }
        public WalletCurrency FromCurrency { get; set; }
        public WalletCurrency ToCurrency { get; set; }
        public string PairLabel { get; set; } = string.Empty;
        public decimal SpreadBps { get; set; }
        public decimal FlatFee { get; set; }
        public decimal PercentageFee { get; set; }
        public decimal MinSourceAmount { get; set; }
        public decimal MaxSourceAmount { get; set; }
        public bool IsActive { get; set; }
    }
}
