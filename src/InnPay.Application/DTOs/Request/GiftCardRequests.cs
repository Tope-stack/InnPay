using InnPay.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace InnPay.Application.DTOs.Request;

public class PurchaseGiftCardRequest
{
    [Required] public Guid AccountId { get; set; }
    [Required] public Guid WalletId { get; set; }

    [Required, StringLength(100, MinimumLength = 1)]
    public string BrandName { get; set; } = string.Empty;

    [Required, StringLength(2, MinimumLength = 2)]
    public string CountryCode { get; set; } = string.Empty;

    [Required, Range(1, double.MaxValue)]
    public decimal DenominationUsd { get; set; }

    [Required, StringLength(6, MinimumLength = 6)]
    public string TransactionPin { get; set; } = string.Empty;
}

public class RevealGiftCardRequest
{
    [Required] public Guid PurchaseId { get; set; }

    [Required, StringLength(6, MinimumLength = 6)]
    public string TransactionPin { get; set; } = string.Empty;
}
