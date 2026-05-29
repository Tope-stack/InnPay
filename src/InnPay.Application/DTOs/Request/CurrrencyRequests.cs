using InnPay.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnPay.Application.DTOs.Request
{
    public class ActivateWalletRequest
    {
        [Required] public Guid AccountId { get; set; }
        [Required] public WalletCurrency Currency { get; set; }

        /// <summary>
        /// If true and the account is eligible, creates a Model B standalone account
        /// (IBAN / sort-code). Otherwise defaults to Model A sub-wallet.
        /// </summary>
        public bool RequestStandaloneAccount { get; set; } = false;
    }

    // ─── FX CONVERSION ───────────────────────────────────────────────────────────

    /// <summary>
    /// Step 1: client requests a rate preview and 30-second lock.
    /// Returns a RateLockId that must be passed to ConfirmConversionRequest.
    /// </summary>
    public class InitiateConversionRequest
    {
        [Required] public Guid AccountId { get; set; }
        [Required] public WalletCurrency FromCurrency { get; set; }
        [Required] public WalletCurrency ToCurrency { get; set; }

        [Required, Range(0.0001, double.MaxValue, ErrorMessage = "Source amount must be positive.")]
        public decimal SourceAmount { get; set; }
    }

    /// <summary>
    /// Step 2: client confirms conversion within the 30-second lock window.
    /// </summary>
    public class ConfirmConversionRequest
    {
        [Required] public Guid AccountId { get; set; }
        [Required] public Guid RateLockId { get; set; }

        /// <summary>Transaction PIN — required to authorise the debit.</summary>
        [Required, StringLength(6, MinimumLength = 6)]
        public string TransactionPin { get; set; } = string.Empty;
    }

    // ─── ADMIN: PAIR CONFIG ───────────────────────────────────────────────────────

    public class UpsertCurrencyPairConfigRequest
    {
        [Required] public WalletCurrency FromCurrency { get; set; }
        [Required] public WalletCurrency ToCurrency { get; set; }

        [Range(0, 10000)] public decimal SpreadBps { get; set; }
        [Range(0, double.MaxValue)] public decimal FlatFee { get; set; }
        [Range(0, 100)] public decimal PercentageFee { get; set; }
        [Range(0, double.MaxValue)] public decimal MinSourceAmount { get; set; }
        [Range(0, double.MaxValue)] public decimal MaxSourceAmount { get; set; }
        public bool IsActive { get; set; } = true;
    }

}
