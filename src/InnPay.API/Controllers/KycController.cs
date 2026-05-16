using InnPay.Application.DTOs.Request;
using InnPay.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InnPay.API.Controllers;

[Authorize]
public class KycController : BaseController
{
    private readonly IKycService _kyc;

    public KycController(IKycService kyc) => _kyc = kyc;

    /// <summary>Get KYC status and all submitted documents for an account.</summary>
    [HttpGet("{accountId:guid}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatus(Guid accountId)
    {
        var result = await _kyc.GetKycStatusAsync(accountId);
        return FromResult(result);
    }

    // ─── PERSONAL KYC ──────────────────────────────────────────────────────

    /// <summary>
    /// Personal Tier 1 KYC: upload a government-issued ID (NIN, passport, or driver's licence).
    /// Optional at sign-up; increases daily NGN limit from ₦50k to ₦500k on approval.
    /// </summary>
    [HttpPost("personal/tier1")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitPersonalTier1([FromBody] UploadPersonalKycTier1Request request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _kyc.SubmitPersonalKycTier1Async(request);
        return FromResult(result);
    }

    /// <summary>
    /// Personal Tier 2 KYC: BVN verification + selfie with ID + proof of address.
    /// Required to unlock ₦5M/day limit and foreign currency wallets.
    /// </summary>
    [HttpPost("personal/tier2")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SubmitPersonalTier2([FromBody] UploadPersonalKycTier2Request request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _kyc.SubmitPersonalKycTier2Async(request);
        return FromResult(result);
    }

    // ─── BUSINESS KYC ──────────────────────────────────────────────────────

    /// <summary>
    /// Business KYC: CAC certificate + business utility bill + director's ID.
    /// Required for full account activation and outbound transactions.
    /// </summary>
    [HttpPost("business")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitBusinessKyc([FromBody] UploadBusinessKycRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _kyc.SubmitBusinessKycAsync(request);
        return FromResult(result);
    }

    // ─── CORPORATE KYC ─────────────────────────────────────────────────────

    /// <summary>
    /// Corporate KYC: Certificate of Incorporation, M&amp;A, Board Resolution,
    /// Beneficial Ownership Declaration, and individual KYC for all UBOs.
    /// Review takes up to 3 business days.
    /// </summary>
    [HttpPost("corporate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitCorporateKyc([FromBody] UploadCorporateKycRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _kyc.SubmitCorporateKycAsync(request);
        return FromResult(result);
    }

    // ─── ADMIN ONLY ────────────────────────────────────────────────────────

    /// <summary>
    /// [Admin] Approve or reject a single KYC document.
    /// Triggers automatic tier upgrade if all documents for the account are approved.
    /// </summary>
    [HttpPost("review")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReviewDocument([FromBody] ReviewKycDocumentRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _kyc.ReviewDocumentAsync(request);
        return FromResult(result);
    }
}
