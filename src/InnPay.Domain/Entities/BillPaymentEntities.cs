using InnPay.Domain.Enums;

namespace InnPay.Domain.Entities;

/// <summary>
/// Utility bill payment record — electricity, airtime, data, cable TV, water, internet.
/// </summary>
public class BillPayment : BaseEntity
{
    public Guid AccountId { get; set; }
    public Guid WalletId { get; set; }

    public BillCategory Category { get; set; }
    public string BillerCode { get; set; } = string.Empty;
    public string BillerName { get; set; } = string.Empty;

    /// <summary>Meter number, phone number, decoder number, etc.</summary>
    public string CustomerReference { get; set; } = string.Empty;
    public string? CustomerName { get; set; }

    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    /// <summary>Provider-side transaction reference.</summary>
    public string? BillerReference { get; set; }

    /// <summary>Prepaid electricity token returned by biller.</summary>
    public string? ElectricityToken { get; set; }

    public string Reference { get; set; } = string.Empty;
    public string? FailureReason { get; set; }

    // Navigation
    public Account Account { get; set; } = null!;
    public Wallet Wallet { get; set; } = null!;
}
