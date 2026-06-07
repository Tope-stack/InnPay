using InnPay.Application.DTOs.Request;
using InnPay.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InnPay.API.Controllers;

/// <summary>NGN virtual account generation via Providus/Wema Bank.</summary>
[Authorize]
[Route("api/v1/accounts/virtual")]
[ApiController]
public class VirtualAccountsController : BaseController
{
    private readonly IVirtualAccountService _vaService;
    public VirtualAccountsController(IVirtualAccountService vaService) => _vaService = vaService;

    /// <summary>POST /api/v1/accounts/virtual/generate — generate or return existing virtual account.</summary>
    [HttpPost("generate")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Generate([FromBody] GenerateVirtualAccountRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        return FromResult(await _vaService.GenerateAsync(request, ct));
    }

    /// <summary>GET /api/v1/accounts/virtual/{accountId} — list all virtual accounts for an account.</summary>
    [HttpGet("{accountId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByAccount(Guid accountId, CancellationToken ct)
        => FromResult(await _vaService.GetByAccountAsync(accountId, ct));
}
