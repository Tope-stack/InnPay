using InnPay.Application.DTOs.Request;
using InnPay.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InnPay.API.Controllers;

/// <summary>Bet account funding — Bet9ja, SportyBet, 1xBet.</summary>
[Authorize]
[Route("api/v1/betting")]
[ApiController]
public class BettingController : BaseController
{
    private readonly IBetFundingService _bettingService;
    public BettingController(IBetFundingService bettingService) => _bettingService = bettingService;

    /// <summary>GET /api/v1/betting/platforms — list supported betting platforms.</summary>
    [HttpGet("platforms")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPlatforms(CancellationToken ct)
        => FromResult(await _bettingService.GetPlatformsAsync(ct));

    /// <summary>POST /api/v1/betting/fund — fund a betting account.</summary>
    [HttpPost("fund")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Fund([FromBody] FundBettingAccountRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        return FromResult(await _bettingService.FundAsync(request, ct));
    }

    /// <summary>GET /api/v1/betting/history/{accountId}</summary>
    [HttpGet("history/{accountId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> History(Guid accountId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => FromResult(await _bettingService.GetHistoryAsync(accountId, page, pageSize, ct));
}
