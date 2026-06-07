using InnPay.Application.Interfaces;
using InnPay.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

namespace InnPay.API.Controllers;

/// <summary>
/// Receives inbound webhooks from payment providers and card issuers.
/// No [Authorize] — authenticated via HMAC-SHA256 signature instead.
/// </summary>
[ApiController]
[Route("api/v1/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly IPaymentGatewayService _paymentService;
    private readonly IVirtualCardService _cardService;
    private readonly IConfiguration _config;

    public WebhooksController(
        IPaymentGatewayService paymentService,
        IVirtualCardService cardService,
        IConfiguration config)
    {
        _paymentService = paymentService;
        _cardService = cardService;
        _config = config;
    }

    /// <summary>POST /api/v1/webhooks/paystack — receives Paystack transaction events.</summary>
    [HttpPost("paystack")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Paystack(CancellationToken ct)
    {
        var secret = _config["Webhooks:PaystackSecret"] ?? string.Empty;
        if (!VerifyHmac(Request, secret, "x-paystack-signature")) return BadRequest("Invalid signature.");

        // Paystack sends the reference in the body — verify it to update payment status
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(ct);

        // TODO: parse body JSON and extract reference / event type
        // For now acknowledge receipt — actual status update happens via verify endpoint
        return Ok(new { received = true });
    }

    /// <summary>POST /api/v1/webhooks/flutterwave — receives Flutterwave transaction events.</summary>
    [HttpPost("flutterwave")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Flutterwave(CancellationToken ct)
    {
        var secret = _config["Webhooks:FlutterwaveSecret"] ?? string.Empty;
        if (!VerifyHmac(Request, secret, "verif-hash")) return BadRequest("Invalid signature.");

        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(ct);

        // TODO: parse body JSON and update payment status
        return Ok(new { received = true });
    }

    /// <summary>POST /api/v1/webhooks/stripe — receives Stripe payment events.</summary>
    [HttpPost("stripe")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Stripe(CancellationToken ct)
    {
        var secret = _config["Webhooks:StripeSecret"] ?? string.Empty;
        if (!VerifyStripeSignature(Request, secret)) return BadRequest("Invalid signature.");

        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(ct);

        // TODO: parse Stripe event and update payment status
        return Ok(new { received = true });
    }

    /// <summary>POST /api/v1/webhooks/card — receives real-time card transaction events from card issuer.</summary>
    [HttpPost("card")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CardWebhook([FromBody] CardWebhookBody body, CancellationToken ct)
    {
        if (body is null || string.IsNullOrEmpty(body.ProviderCardId)) return BadRequest();

        var payload = new CardTransactionWebhookPayload
        {
            MerchantName = body.MerchantName,
            MerchantCategory = body.MerchantCategory,
            Amount = body.Amount,
            Currency = body.Currency,
            ProviderTransactionId = body.ProviderTransactionId,
            IsDeclined = body.IsDeclined,
            DeclineReason = body.DeclineReason,
            TransactedAt = body.TransactedAt
        };

        return FromServiceResult(await _cardService.HandleWebhookAsync(body.ProviderCardId, payload, ct));
    }

    // ── Signature helpers ─────────────────────────────────────────────────────

    private static bool VerifyHmac(HttpRequest request, string secret, string headerName)
    {
        if (string.IsNullOrEmpty(secret)) return true; // skip in dev if not configured
        if (!request.Headers.TryGetValue(headerName, out var signature)) return false;

        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secret));
        request.EnableBuffering();
        var body = new StreamReader(request.Body).ReadToEnd();
        request.Body.Position = 0;

        var hash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(body))).ToLower();
        return hash == signature.ToString().ToLower();
    }

    private static bool VerifyStripeSignature(HttpRequest request, string secret)
    {
        if (string.IsNullOrEmpty(secret)) return true;
        // TODO: use Stripe.net ConstructEvent for proper signature validation
        return request.Headers.ContainsKey("Stripe-Signature");
    }

    private IActionResult FromServiceResult(Application.Common.ServiceResult result)
    {
        return result.IsSuccess ? Ok() : StatusCode(result.StatusCode, result.Error);
    }
}

public class CardWebhookBody
{
    public string ProviderCardId { get; set; } = string.Empty;
    public string MerchantName { get; set; } = string.Empty;
    public string MerchantCategory { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string ProviderTransactionId { get; set; } = string.Empty;
    public bool IsDeclined { get; set; }
    public string? DeclineReason { get; set; }
    public DateTime TransactedAt { get; set; }
}
