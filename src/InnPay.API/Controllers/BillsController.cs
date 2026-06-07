using InnPay.Application.DTOs.Request;
using InnPay.Application.Interfaces;
using InnPay.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InnPay.API.Controllers;

/// <summary>Utility bill payments — electricity, airtime, data, cable TV, water, internet.</summary>
[Authorize]
public class BillsController : BaseController
{
    private readonly IBillsService _billsService;
    public BillsController(IBillsService billsService) => _billsService = billsService;

    /// <summary>GET /api/v1/bills/billers?category=1 — list billers by category.</summary>
    [HttpGet("billers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBillers([FromQuery] BillCategory? category, CancellationToken ct)
        => FromResult(await _billsService.GetBillersAsync(category, ct));

    /// <summary>POST /api/v1/bills/validate — validate customer meter/account number with biller.</summary>
    [HttpPost("validate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Validate([FromBody] ValidateBillCustomerRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        return FromResult(await _billsService.ValidateCustomerAsync(request, ct));
    }

    /// <summary>POST /api/v1/bills/pay — pay a bill and debit wallet.</summary>
    [HttpPost("pay")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Pay([FromBody] PayBillRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        return FromResult(await _billsService.PayBillAsync(request, ct));
    }
}
