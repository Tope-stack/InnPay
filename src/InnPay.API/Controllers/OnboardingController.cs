using InnPay.Application.DTOs.Request;
using InnPay.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InnPay.API.Controllers;

/// <summary>
/// Handles registration flows for all three account types,
/// OTP verification, and transaction PIN setup.
/// All endpoints are public (no auth required).
/// </summary>
[AllowAnonymous]
public class OnboardingController : BaseController
{
    private readonly IOnboardingService _onboarding;

    public OnboardingController(IOnboardingService onboarding)
        => _onboarding = onboarding;

    // ─── PERSONAL ──────────────────────────────────────────────────────────

    /// <summary>Step 1: Register a personal account.</summary>
    [HttpPost("personal/register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterPersonal([FromBody] RegisterPersonalRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _onboarding.RegisterPersonalAsync(request, cancellationToken);
        return FromResult(result);
    }

    // ─── BUSINESS ──────────────────────────────────────────────────────────

    /// <summary>Step 1: Register a business account.</summary>
    [HttpPost("business/register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterBusiness([FromBody] RegisterBusinessRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _onboarding.RegisterBusinessAsync(request, cancellationToken);
        return FromResult(result);
    }

    // ─── CORPORATE ─────────────────────────────────────────────────────────

    /// <summary>Step 1: Initiate corporate account registration.</summary>
    [HttpPost("corporate/register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterCorporate([FromBody] RegisterCorporateRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _onboarding.RegisterCorporateAsync(request, cancellationToken);
        return FromResult(result);
    }

    // ─── OTP VERIFICATION ──────────────────────────────────────────────────

    /// <summary>Step 2: Verify phone number via OTP.</summary>
    [HttpPost("verify-otp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _onboarding.VerifyPhoneOtpAsync(request, cancellationToken);
        return FromResult(result);
    }

    /// <summary>Resend OTP to the user's phone number.</summary>
    [HttpPost("resend-otp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ResendOtp([FromBody] ResendOtpRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _onboarding.ResendOtpAsync(request, cancellationToken);
        return FromResult(result);
    }

    // ─── TRANSACTION PIN ───────────────────────────────────────────────────

    /// <summary>Step 3: Set a 6-digit transaction PIN (completes onboarding).</summary>
    [HttpPost("set-pin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SetTransactionPin([FromBody] SetTransactionPinRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _onboarding.SetTransactionPinAsync(request, cancellationToken);
        return FromResult(result);
    }

    // ─── ACCOUNT DETAILS ───────────────────────────────────────────────────

    /// <summary>Get account details including wallets. Requires authentication.</summary>
    [HttpGet("account/{accountId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAccount(Guid accountId, CancellationToken cancellationToken)
    {
        var result = await _onboarding.GetAccountAsync(accountId, cancellationToken);
        return FromResult(result);
    }
}
