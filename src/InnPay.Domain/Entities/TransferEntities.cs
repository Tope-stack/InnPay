using InnPay.Domain.Enums;

namespace InnPay.Domain.Entities;

/// <summary>
/// Atomic internal transfer between two InnPay wallets.
/// Debit and credit happen inside a single DB transaction — no partial states.
/// </summary>
public class InternalTransfer : BaseEntity
{
    public Guid SenderAccountId { get; set; }
    public Guid ReceiverAccountId { get; set; }
    public Guid SenderWalletId { get; set; }
    public Guid ReceiverWalletId { get; set; }

    public decimal Amount { get; set; }
    public WalletCurrency Currency { get; set; }

    public TransferStatus Status { get; set; } = TransferStatus.Pending;

    /// <summary>Unique internal reference (e.g. INN-TXF-xxxxxxxx).</summary>
    public string Reference { get; set; } = string.Empty;

    /// <summary>Optional note / payment description from sender.</summary>
    public string? Note { get; set; }

    /// <summary>Role of the user who initiated the transfer (for audit).</summary>
    public UserRole InitiatorRole { get; set; }

    public string? FailureReason { get; set; }

    // Navigation
    public Account SenderAccount { get; set; } = null!;
    public Account ReceiverAccount { get; set; } = null!;
    public Wallet SenderWallet { get; set; } = null!;
    public Wallet ReceiverWallet { get; set; } = null!;
}

/// <summary>
/// Recurring transfer schedule — processed by TransferScheduleJob background service.
/// </summary>
public class TransferSchedule : BaseEntity
{
    public Guid SenderAccountId { get; set; }
    public Guid SenderWalletId { get; set; }
    public Guid ReceiverAccountId { get; set; }
    public Guid ReceiverWalletId { get; set; }

    public decimal Amount { get; set; }
    public WalletCurrency Currency { get; set; }

    public TransferFrequency Frequency { get; set; }
    public DateTime NextRunAt { get; set; }
    public bool IsActive { get; set; } = true;

    public string? Note { get; set; }

    // Navigation
    public Account SenderAccount { get; set; } = null!;
    public Wallet SenderWallet { get; set; } = null!;
}

/// <summary>
/// Saved beneficiary — allows sender to quickly pick a previous internal transfer recipient.
/// </summary>
public class SavedBeneficiary : BaseEntity
{
    public Guid OwnerAccountId { get; set; }
    public Guid BeneficiaryAccountId { get; set; }

    public string Nickname { get; set; } = string.Empty;

    // Navigation
    public Account OwnerAccount { get; set; } = null!;
    public Account BeneficiaryAccount { get; set; } = null!;
}

/// <summary>
/// Outbound bank transfer — NGN via NIP/NIBSS, USD/EUR/GBP via SWIFT/SEPA.
/// </summary>
public class ExternalTransfer : BaseEntity
{
    public Guid SenderAccountId { get; set; }
    public Guid SenderWalletId { get; set; }
    public Guid? SavedBankAccountId { get; set; }

    // Destination bank details (snapshot at time of transfer)
    public string DestinationBankCode { get; set; } = string.Empty;
    public string DestinationBankName { get; set; } = string.Empty;
    public string DestinationAccountNumber { get; set; } = string.Empty;
    public string DestinationAccountName { get; set; } = string.Empty;

    public decimal Amount { get; set; }
    public WalletCurrency Currency { get; set; }

    public ExternalTransferStatus Status { get; set; } = ExternalTransferStatus.Pending;

    public string Reference { get; set; } = string.Empty;

    /// <summary>NIBSS session ID for NGN NIP transfers.</summary>
    public string? NibssSessionId { get; set; }

    /// <summary>SWIFT / SEPA reference for foreign currency transfers.</summary>
    public string? SwiftReference { get; set; }

    /// <summary>Rule-based fraud score 0–100. Flagged if > 70.</summary>
    public int FraudScore { get; set; } = 0;
    public bool IsFlagged { get; set; } = false;

    public string? FailureReason { get; set; }

    // Navigation
    public Account SenderAccount { get; set; } = null!;
    public Wallet SenderWallet { get; set; } = null!;
    public SavedBankAccount? SavedBankAccount { get; set; }
}

/// <summary>
/// A saved external bank account for quick withdrawal / external transfer.
/// </summary>
public class SavedBankAccount : BaseEntity
{
    public Guid AccountId { get; set; }

    public string BankCode { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public WalletCurrency Currency { get; set; } = WalletCurrency.NGN;

    public bool IsVerified { get; set; } = false;

    // Navigation
    public Account Account { get; set; } = null!;
}
