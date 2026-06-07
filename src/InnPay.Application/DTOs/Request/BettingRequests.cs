using InnPay.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace InnPay.Application.DTOs.Request;

public class FundBettingAccountRequest
{
    [Required] public Guid AccountId { get; set; }
    [Required] public Guid WalletId { get; set; }
    [Required] public BettingPlatform Platform { get; set; }

    [Required, StringLength(100, MinimumLength = 2)]
    public string BettingUserId { get; set; } = string.Empty;

    [Required, Range(1, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required, StringLength(6, MinimumLength = 6)]
    public string TransactionPin { get; set; } = string.Empty;
}
