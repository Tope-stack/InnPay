using InnPay.Application.DTOs.Request;
using InnPay.Application.Interfaces;
using InnPay.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InnPay.API.Controllers;

/// <summary>External bank transfers — NGN via NIP/NIBSS, foreign currency via SWIFT/SEPA.</summary>
[Authorize]
[Route("api/v1/transfers/external")]
[ApiController]
public class ExternalTransfersController : BaseController
{
    private readonly IExternalTransferService _transferService;
    public ExternalTransfersController(IExternalTransferService transferService) => _transferService = transferService;

    /// <summary>GET /api/v1/transfers/external/banks?currency=NGN — list supported banks.</summary>
    [HttpGet("banks")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBanks([FromQuery] WalletCurrency currency = WalletCurrency.NGN, CancellationToken ct = default)
        => FromResult(await _transferService.GetBankListAsync(currency, ct));

    /// <summary>POST /api/v1/transfers/external/validate — validate destination account name.</summary>
    [HttpPost("validate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Validate([FromBody] ValidateBankAccountRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        return FromResult(await _transferService.ValidateBankAccountAsync(request, ct));
    }

    /// <summary>POST /api/v1/transfers/external — initiate an outbound bank transfer.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Initiate([FromBody] ExternalTransferRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        return FromResult(await _transferService.InitiateAsync(request, ct));
    }

    /// <summary>GET /api/v1/transfers/external/history/{accountId}</summary>
    [HttpGet("history/{accountId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> History(Guid accountId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => FromResult(await _transferService.GetHistoryAsync(accountId, page, pageSize, ct));
}
