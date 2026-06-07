using InnPay.Application.DTOs.Request;
using InnPay.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InnPay.API.Controllers;

/// <summary>Universal payment gateway — initiate and verify payments via Paystack, Flutterwave, Stripe.</summary>
[Authorize]
public class PaymentsController : BaseController
{
    private readonly IPaymentGatewayService _paymentService;
    public PaymentsController(IPaymentGatewayService paymentService) => _paymentService = paymentService;

    /// <summary>POST /api/v1/payments/initiate — create a payment intent.</summary>
    [HttpPost("initiate")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Initiate([FromBody] InitiatePaymentRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        return FromResult(await _paymentService.InitiateAsync(request, ct));
    }

    /// <summary>POST /api/v1/payments/verify — verify payment status with provider.</summary>
    [HttpPost("verify")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Verify([FromBody] VerifyPaymentRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        return FromResult(await _paymentService.VerifyAsync(request, ct));
    }
}
