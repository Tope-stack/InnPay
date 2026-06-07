using InnPay.Application.DTOs.Request;
using InnPay.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InnPay.API.Controllers;

/// <summary>Internal wallet-to-wallet transfers between InnPay users.</summary>
[Authorize]
[Route("api/v1/transfers/internal")]
[ApiController]
public class InternalTransfersController : BaseController
{
    private readonly IInternalTransferService _transferService;
    public InternalTransfersController(IInternalTransferService transferService) => _transferService = transferService;

    /// <summary>GET /api/v1/transfers/internal/lookup?identifier=phone|email — look up a recipient.</summary>
    [HttpGet("lookup")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Lookup([FromQuery] string identifier, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(identifier)) return BadRequest("identifier is required.");
        return FromResult(await _transferService.LookupRecipientAsync(identifier, ct));
    }

    /// <summary>POST /api/v1/transfers/internal — initiate an atomic internal transfer.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Initiate([FromBody] InternalTransferRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        return FromResult(await _transferService.InitiateAsync(request, ct));
    }

    /// <summary>POST /api/v1/transfers/internal/schedule — set up a recurring transfer.</summary>
    [HttpPost("schedule")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Schedule([FromBody] ScheduleTransferRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        return FromResult(await _transferService.ScheduleAsync(request, ct));
    }

    /// <summary>DELETE /api/v1/transfers/internal/schedule — cancel a recurring transfer.</summary>
    [HttpDelete("schedule")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelSchedule([FromBody] CancelScheduleRequest request, CancellationToken ct)
        => FromResult(await _transferService.CancelScheduleAsync(request, ct));

    /// <summary>GET /api/v1/transfers/internal/history/{accountId}?page=1&amp;pageSize=20</summary>
    [HttpGet("history/{accountId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> History(Guid accountId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => FromResult(await _transferService.GetHistoryAsync(accountId, page, pageSize, ct));
}
