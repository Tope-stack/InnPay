using System.ComponentModel.DataAnnotations;

namespace InnPay.Application.DTOs.Request;

public class CreateVirtualCardRequest
{
    [Required] public Guid AccountId { get; set; }

    /// <summary>Must be the USD wallet for this account.</summary>
    [Required] public Guid WalletId { get; set; }

    [Required, StringLength(6, MinimumLength = 6)]
    public string TransactionPin { get; set; } = string.Empty;
}

public class SetSpendingLimitRequest
{
    [Required] public Guid CardId { get; set; }

    [Range(0, double.MaxValue)] public decimal PerTransactionLimit { get; set; } = 0m;
    [Range(0, double.MaxValue)] public decimal MonthlyLimit { get; set; } = 0m;

    [Required, StringLength(6, MinimumLength = 6)]
    public string TransactionPin { get; set; } = string.Empty;
}

public class CardActionRequest
{
    [Required] public Guid CardId { get; set; }

    [Required, StringLength(6, MinimumLength = 6)]
    public string TransactionPin { get; set; } = string.Empty;
}

public class RevealCardDetailsRequest
{
    [Required] public Guid CardId { get; set; }

    [Required, StringLength(6, MinimumLength = 6)]
    public string TransactionPin { get; set; } = string.Empty;
}
