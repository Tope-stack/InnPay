using InnPay.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace InnPay.Application.DTOs.Request;

public class GetBillersRequest
{
    public BillCategory? Category { get; set; }
}

public class ValidateBillCustomerRequest
{
    [Required, StringLength(50, MinimumLength = 2)]
    public string BillerCode { get; set; } = string.Empty;

    /// <summary>Meter number, phone number, decoder number, etc.</summary>
    [Required, StringLength(50, MinimumLength = 2)]
    public string CustomerReference { get; set; } = string.Empty;
}

public class PayBillRequest
{
    [Required] public Guid AccountId { get; set; }
    [Required] public Guid WalletId { get; set; }

    [Required, StringLength(50, MinimumLength = 2)]
    public string BillerCode { get; set; } = string.Empty;

    [Required, StringLength(50, MinimumLength = 2)]
    public string CustomerReference { get; set; } = string.Empty;

    [Required, Range(1, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required, StringLength(6, MinimumLength = 6)]
    public string TransactionPin { get; set; } = string.Empty;
}
