using InnPay.Application.DTOs.Request;
using InnPay.Application.Interfaces;
using InnPay.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InnPay.API.Controllers
{
    /// <summary>
    /// Wallet management and FX conversion endpoints.
    /// All endpoints require authentication.
    /// </summary>
    [Authorize]
    public class WalletsController : BaseController
    {
        private readonly IWalletService _walletService;
        private readonly IFxConversionService _conversionService;
        private readonly ICurrencyPairConfigService _pairConfigService;

        public WalletsController(
            IWalletService walletService,
            IFxConversionService conversionService,
            ICurrencyPairConfigService pairConfigService)
        {
            _walletService = walletService;
            _conversionService = conversionService;
            _pairConfigService = pairConfigService;
        }

        // ─── GET WALLETS ────────────────────────────────────────────────────────

        /// <summary>
        /// GET /api/v1/wallets/{accountId}
        /// Returns all active wallets and balances for an account.
        /// Includes both Model A (sub-wallets) and Model B (standalone accounts).
        /// Also returns an approximate total NGN value across all wallets.
        /// </summary>
        [HttpGet("{accountId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetWallets(Guid accountId)
        {
            var result = await _walletService.GetWalletsAsync(accountId);
            return FromResult(result);
        }

        // ─── ACTIVATE WALLET ────────────────────────────────────────────────────

        /// <summary>
        /// POST /api/v1/wallets/activate
        /// Activates a currency wallet for an account.
        /// - Personal (Tier 2 KYC): activates Model A sub-wallet for USD, EUR, or GBP.
        /// - Business (verified KYC): activates Model A sub-wallet.
        /// - Corporate: activates Model B standalone account (IBAN / sort code) by default.
        /// - Set <c>requestStandaloneAccount: true</c> to request Model B for eligible Business accounts.
        /// </summary>
        [HttpPost("activate")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ActivateWallet([FromBody] ActivateWalletRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _walletService.ActivateWalletAsync(request);
            return FromResult(result);
        }

        // ─── FX CONVERSION: STEP 1 ───────────────────────────────────────────────

        /// <summary>
        /// POST /api/v1/wallets/convert
        /// Step 1: Initiates an FX conversion.
        /// - Fetches the live rate and locks it for 30 seconds.
        /// - Reserves the source amount (soft hold on the wallet).
        /// - Returns a <c>rateLockId</c>, the conversion preview, and a countdown timer.
        /// - The client must call /convert/confirm within 30 seconds, or the lock expires.
        /// </summary>
        [HttpPost("convert")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> InitiateConversion([FromBody] InitiateConversionRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _conversionService.InitiateConversionAsync(request);
            return FromResult(result);
        }

        // ─── FX CONVERSION: STEP 2 ───────────────────────────────────────────────

        /// <summary>
        /// POST /api/v1/wallets/convert/confirm
        /// Step 2: Confirms and executes the conversion using the locked rate.
        /// - Validates the transaction PIN.
        /// - Debits the source wallet and credits the destination wallet atomically.
        /// - Returns updated balances and the FX transaction reference.
        /// - Returns 422 if the 30-second lock has expired.
        /// </summary>
        [HttpPost("convert/confirm")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> ConfirmConversion([FromBody] ConfirmConversionRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _conversionService.ConfirmConversionAsync(request);
            return FromResult(result);
        }

        // ─── FX CONVERSION HISTORY ───────────────────────────────────────────────

        /// <summary>
        /// GET /api/v1/wallets/conversions/{accountId}?page=1&amp;pageSize=20
        /// Returns paginated FX conversion history for an account.
        /// </summary>
        [HttpGet("conversions/{accountId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetConversionHistory(
            Guid accountId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize is < 1 or > 100) pageSize = 20;

            var result = await _conversionService.GetConversionHistoryAsync(accountId, page, pageSize);
            return FromResult(result);
        }

        // ─── ADMIN: CURRENCY PAIR CONFIG ─────────────────────────────────────────

        /// <summary>
        /// GET /api/v1/wallets/config/pairs
        /// [Admin] Returns all active currency pair spread and fee configurations.
        /// </summary>
        [HttpGet("config/pairs")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPairConfigs()
        {
            var result = await _pairConfigService.GetAllConfigsAsync();
            return FromResult(result);
        }

        /// <summary>
        /// POST /api/v1/wallets/config/pairs
        /// [Admin] Creates or updates the spread and fee configuration for a currency pair.
        /// </summary>
        [HttpPost("config/pairs")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpsertPairConfig([FromBody] UpsertCurrencyPairConfigRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _pairConfigService.UpsertConfigAsync(request);
            return FromResult(result);
        }

        /// <summary>
        /// PUT /api/v1/wallets/{walletId}/status
        /// [Admin] Freeze or unfreeze a wallet.
        /// </summary>
        [HttpPut("{walletId:guid}/status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SetWalletStatus(Guid walletId, [FromQuery] WalletStatus status)
        {
            var result = await _walletService.SetWalletStatusAsync(walletId, status);
            return FromResult(result);
        }
    }
}
