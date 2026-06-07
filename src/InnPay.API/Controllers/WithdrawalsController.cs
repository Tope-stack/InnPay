using InnPay.Application.DTOs.Request;
using InnPay.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InnPay.API.Controllers;

/// <summary>Withdraw funds from InnPay wallet to an external bank account.</summary>
[Authorize]
public class WithdrawalsController : BaseController
{
    private readonly IWithdrawalService _withdrawalService;
    public WithdrawalsController(IWithdrawalService withdrawalService) => _withdrawalService = withdrawalService;

    /// <summary>POST /api/v1/withdrawals/initiate — initiate a withdrawal.</summary>
    [HttpPost("initiate")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Initiate([FromBody] InitiateWithdrawalRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        return FromResult(await _withdrawalService.InitiateAsync(request, ct));
    }

    /// <summary>GET /api/v1/withdrawals/history/{accountId}</summary>
    [HttpGet("history/{accountId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> History(Guid accountId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => FromResult(await _withdrawalService.GetHistoryAsync(accountId, page, pageSize, ct));
}
