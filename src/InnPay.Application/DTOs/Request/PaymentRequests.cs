using InnPay.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace InnPay.Application.DTOs.Request;

public class InitiatePaymentRequest
{
    [Required] public Guid AccountId { get; set; }
    [Required] public Guid WalletId { get; set; }

    [Required, Range(1, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; set; }

    [Required] public WalletCurrency Currency { get; set; }
    [Required] public PaymentMethod Method { get; set; }

    /// <summary>Client-supplied idempotency key — prevents duplicate charges on retry.</summary>
    [Required, StringLength(100, MinimumLength = 8)]
    public string IdempotencyKey { get; set; } = string.Empty;

    /// <summary>Optional metadata / description surfaced in the transaction record.</summary>
    [StringLength(255)]
    public string? Description { get; set; }
}

public class VerifyPaymentRequest
{
    [Required] public string Reference { get; set; } = string.Empty;
    [Required] public PaymentProvider Provider { get; set; }
}
