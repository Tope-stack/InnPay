using InnPay.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace InnPay.Application.DTOs.Request;

// ── Internal Transfer ──────────────────────────────────────────────────────────

public class InternalTransferRequest
{
    [Required] public Guid SenderAccountId { get; set; }

    /// <summary>InnPay account ID, phone number, or username of the recipient.</summary>
    [Required, StringLength(100, MinimumLength = 3)]
    public string RecipientIdentifier { get; set; } = string.Empty;

    [Required, Range(1, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; set; }

    [Required] public WalletCurrency Currency { get; set; }

    [StringLength(255)] public string? Note { get; set; }

    /// <summary>Transaction PIN — validated before executing the transfer.</summary>
    [Required, StringLength(6, MinimumLength = 6, ErrorMessage = "PIN must be exactly 6 digits.")]
    public string TransactionPin { get; set; } = string.Empty;

    /// <summary>If true, saves recipient as a beneficiary for quick future transfers.</summary>
    public bool SaveBeneficiary { get; set; } = false;
}

public class ScheduleTransferRequest
{
    [Required] public Guid SenderAccountId { get; set; }
    [Required] public Guid ReceiverAccountId { get; set; }

    [Required, Range(1, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required] public WalletCurrency Currency { get; set; }
    [Required] public TransferFrequency Frequency { get; set; }

    [Required] public DateTime StartDate { get; set; }

    [StringLength(255)] public string? Note { get; set; }

    [Required, StringLength(6, MinimumLength = 6)]
    public string TransactionPin { get; set; } = string.Empty;
}

public class CancelScheduleRequest
{
    [Required] public Guid ScheduleId { get; set; }
    [Required] public Guid AccountId { get; set; }
}

// ── External Transfer ──────────────────────────────────────────────────────────

public class ExternalTransferRequest
{
    [Required] public Guid SenderAccountId { get; set; }
    [Required] public Guid SenderWalletId { get; set; }

    [Required, StringLength(10, MinimumLength = 2)]
    public string BankCode { get; set; } = string.Empty;

    [Required, StringLength(20, MinimumLength = 10)]
    public string AccountNumber { get; set; } = string.Empty;

    [Required, Range(1, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required] public WalletCurrency Currency { get; set; }

    [Required, StringLength(6, MinimumLength = 6)]
    public string TransactionPin { get; set; } = string.Empty;

    public bool SaveBankAccount { get; set; } = false;
}

public class ValidateBankAccountRequest
{
    [Required, StringLength(10, MinimumLength = 2)]
    public string BankCode { get; set; } = string.Empty;

    [Required, StringLength(20, MinimumLength = 10)]
    public string AccountNumber { get; set; } = string.Empty;

    public WalletCurrency Currency { get; set; } = WalletCurrency.NGN;
}
