using System.ComponentModel.DataAnnotations;

namespace InnPay.Application.DTOs.Request;

public class GenerateVirtualAccountRequest
{
    [Required] public Guid AccountId { get; set; }
    [Required] public Guid WalletId { get; set; }

    /// <summary>Optional label for Corporate cost-centre sub-accounts (e.g. "Marketing Budget").</summary>
    [StringLength(100)]
    public string? Label { get; set; }
}

public class InitiateWithdrawalRequest
{
    [Required] public Guid AccountId { get; set; }
    [Required] public Guid WalletId { get; set; }

    /// <summary>Use an existing saved bank account, or supply BankCode + AccountNumber below.</summary>
    public Guid? SavedBankAccountId { get; set; }

    [StringLength(10, MinimumLength = 2)]
    public string? BankCode { get; set; }

    [StringLength(20, MinimumLength = 10)]
    public string? AccountNumber { get; set; }

    [Required, Range(1, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required, StringLength(6, MinimumLength = 6)]
    public string TransactionPin { get; set; } = string.Empty;

    public bool SaveBankAccount { get; set; } = false;
}
