using InnPay.Application.DTOs.Request;
using InnPay.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InnPay.API.Controllers;

/// <summary>USD virtual card management — create, freeze, unfreeze, delete, spending limits.</summary>
[Authorize]
[Route("api/v1/cards/virtual")]
[ApiController]
public class VirtualCardsController : BaseController
{
    private readonly IVirtualCardService _cardService;
    public VirtualCardsController(IVirtualCardService cardService) => _cardService = cardService;

    /// <summary>POST /api/v1/cards/virtual/create</summary>
    [HttpPost("create")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateVirtualCardRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        return FromResult(await _cardService.CreateAsync(request, ct));
    }

    /// <summary>POST /api/v1/cards/virtual/reveal — return decrypted PAN + CVV (PIN required).</summary>
    [HttpPost("reveal")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Reveal([FromBody] RevealCardDetailsRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        return FromResult(await _cardService.RevealDetailsAsync(request, ct));
    }

    /// <summary>POST /api/v1/cards/virtual/freeze</summary>
    [HttpPost("freeze")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Freeze([FromBody] CardActionRequest request, CancellationToken ct)
        => FromResult(await _cardService.FreezeAsync(request, ct));

    /// <summary>POST /api/v1/cards/virtual/unfreeze</summary>
    [HttpPost("unfreeze")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Unfreeze([FromBody] CardActionRequest request, CancellationToken ct)
        => FromResult(await _cardService.UnfreezeAsync(request, ct));

    /// <summary>POST /api/v1/cards/virtual/delete — permanently cancel the card.</summary>
    [HttpPost("delete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete([FromBody] CardActionRequest request, CancellationToken ct)
        => FromResult(await _cardService.DeleteAsync(request, ct));

    /// <summary>POST /api/v1/cards/virtual/spending-limit</summary>
    [HttpPost("spending-limit")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetSpendingLimit([FromBody] SetSpendingLimitRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        return FromResult(await _cardService.SetSpendingLimitAsync(request, ct));
    }

    /// <summary>GET /api/v1/cards/virtual/history/{cardId}?page=1&amp;pageSize=20</summary>
    [HttpGet("history/{cardId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> History(Guid cardId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => FromResult(await _cardService.GetTransactionHistoryAsync(cardId, page, pageSize, ct));
}
