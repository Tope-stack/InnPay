namespace InnPay.Domain.Entities;

/// <summary>
/// NGN virtual account number issued via Providus Bank or Wema Bank.
/// Each user gets a permanent dedicated account; Corporate accounts can have
/// multiple sub-accounts for different cost centres.
/// Inbound transfers auto-credit the linked NGN wallet.
/// </summary>
public class VirtualAccount : BaseEntity
{
    public Guid AccountId { get; set; }
    public Guid WalletId { get; set; }

    public string AccountNumber { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string BankCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;

    /// <summary>Optional label for Corporate cost-centre sub-accounts (e.g. "Marketing Budget").</summary>
    public string? Label { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation
    public Account Account { get; set; } = null!;
    public Wallet Wallet { get; set; } = null!;
}

/// <summary>
/// Withdrawal request — outbound transfer from an InnPay wallet to an external bank account.
/// </summary>
public class Withdrawal : BaseEntity
{
    public Guid AccountId { get; set; }
    public Guid WalletId { get; set; }
    public Guid? SavedBankAccountId { get; set; }

    // Destination snapshot
    public string DestinationBankCode { get; set; } = string.Empty;
    public string DestinationBankName { get; set; } = string.Empty;
    public string DestinationAccountNumber { get; set; } = string.Empty;
    public string DestinationAccountName { get; set; } = string.Empty;

    public decimal Amount { get; set; }
    public Domain.Enums.WalletCurrency Currency { get; set; }

    public Domain.Enums.WithdrawalStatus Status { get; set; } = Domain.Enums.WithdrawalStatus.Pending;

    public string Reference { get; set; } = string.Empty;
    public string? ProviderReference { get; set; }
    public string? FailureReason { get; set; }

    /// <summary>Fraud flag — set when amount exceeds 80% of 7-day average.</summary>
    public bool IsFlagged { get; set; } = false;
    public int FraudScore { get; set; } = 0;

    // Navigation
    public Account Account { get; set; } = null!;
    public Wallet Wallet { get; set; } = null!;
    public SavedBankAccount? SavedBankAccount { get; set; }
}
