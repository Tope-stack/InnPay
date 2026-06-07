using InnPay.Application.DTOs.Request;
using InnPay.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InnPay.API.Controllers;

/// <summary>Gift card catalogue, purchase, and reveal via Reloadly.</summary>
[Authorize]
public class GiftCardsController : BaseController
{
    private readonly IGiftCardService _giftCardService;
    public GiftCardsController(IGiftCardService giftCardService) => _giftCardService = giftCardService;

    /// <summary>GET /api/v1/giftcards/catalogue?countryCode=US</summary>
    [HttpGet("catalogue")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Catalogue([FromQuery] string? countryCode, CancellationToken ct)
        => FromResult(await _giftCardService.GetCatalogueAsync(countryCode, ct));

    /// <summary>POST /api/v1/giftcards/purchase</summary>
    [HttpPost("purchase")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Purchase([FromBody] PurchaseGiftCardRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        return FromResult(await _giftCardService.PurchaseAsync(request, ct));
    }

    /// <summary>POST /api/v1/giftcards/reveal — decrypt and return redemption code (PIN required).</summary>
    [HttpPost("reveal")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Reveal([FromBody] RevealGiftCardRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        return FromResult(await _giftCardService.RevealAsync(request, ct));
    }

    /// <summary>GET /api/v1/giftcards/purchases/{accountId}</summary>
    [HttpGet("purchases/{accountId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Purchases(Guid accountId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => FromResult(await _giftCardService.GetPurchasesAsync(accountId, page, pageSize, ct));
}
