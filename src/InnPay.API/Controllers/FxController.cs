using InnPay.Application.Interfaces;
using InnPay.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InnPay.API.Controllers
{
    /// <summary>
    /// GET /api/v1/fx/rates — returns live exchange rates for all supported currency pairs.
    /// Public endpoint: the mobile app shows rates before login (e.g. on the currency converter screen).
    /// </summary>
    [AllowAnonymous]
    public class FxController : BaseController
    {
        private readonly IFxRateService _fxRateService;

        public FxController(IFxRateService fxRateService) => _fxRateService = fxRateService;

        /// <summary>
        /// Returns live exchange rates for all supported pairs.
        /// Rates are refreshed every 60 seconds server-side.
        /// The response includes an <c>isStale</c> flag per pair if the rate is older than 60 s.
        /// </summary>
        [HttpGet("rates")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllRates(CancellationToken cancellationToken)
        {
            var result = await _fxRateService.GetAllRatesAsync(cancellationToken);
            return FromResult(result);
        }

        /// <summary>
        /// Returns the live rate for a single currency pair (e.g. USD → NGN).
        /// </summary>
        [HttpGet("rates/{fromCurrency}/{toCurrency}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> GetRate(WalletCurrency fromCurrency, WalletCurrency toCurrency, CancellationToken cancellationToken)
        {
            var result = await _fxRateService.GetRateAsync(fromCurrency, toCurrency, cancellationToken);
            return FromResult(result);
        }
    }
}
