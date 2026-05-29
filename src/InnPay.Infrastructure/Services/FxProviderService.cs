using InnPay.Application.Interfaces;
using InnPay.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace InnPay.Infrastructure.Services
{
    // ─── OPEN EXCHANGE RATES PROVIDER ────────────────────────────────────────────

    /// <summary>
    /// Fetches USD-base rates from openexchangerates.org, then derives all required pairs.
    /// Free plan: base currency is always USD.
    /// Swap for Fixer.io by implementing IFxProviderService with a different HTTP call.
    /// </summary>
    public class OpenExchangeRatesFxProvider : IFxProviderService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;
        private readonly ILogger<OpenExchangeRatesFxProvider> _logger;

        // All currencies we need to resolve from the USD-base response
        private static readonly string[] TargetCurrencies = ["NGN", "USD", "EUR", "GBP"];

        public OpenExchangeRatesFxProvider(
            HttpClient http,
            IConfiguration config,
            ILogger<OpenExchangeRatesFxProvider> logger)
        {
            _http = http;
            _config = config;
            _logger = logger;
        }

        public async Task<IEnumerable<ExternalRateDto>> FetchRatesAsync()
        {
            var appId = _config["FxProvider:OpenExchangeRates:AppId"];
            if (string.IsNullOrEmpty(appId))
            {
                _logger.LogWarning("OpenExchangeRates AppId not configured. Using fallback rates.");
                return GetFallbackRates();
            }

            try
            {
                var url = $"https://openexchangerates.org/api/latest.json?app_id={appId}&symbols=NGN,EUR,GBP";
                var response = await _http.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var body = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(body);

                var rates = doc.RootElement.GetProperty("rates");
                var fetchedAt = DateTime.UtcNow;

                // Base is USD — extract USD→X rates
                var usdToNgn = rates.GetProperty("NGN").GetDecimal();
                var usdToEur = rates.GetProperty("EUR").GetDecimal();
                var usdToGbp = rates.GetProperty("GBP").GetDecimal();

                // Derive cross-rates from USD base
                var results = new List<ExternalRateDto>
            {
                // USD pairs (direct)
                new(WalletCurrency.USD, WalletCurrency.NGN, usdToNgn, fetchedAt),
                new(WalletCurrency.USD, WalletCurrency.EUR, usdToEur, fetchedAt),
                new(WalletCurrency.USD, WalletCurrency.GBP, usdToGbp, fetchedAt),

                // NGN pairs (inverse of USD→X)
                new(WalletCurrency.NGN, WalletCurrency.USD, Round(1m / usdToNgn), fetchedAt),
                new(WalletCurrency.NGN, WalletCurrency.EUR, Round(usdToEur / usdToNgn), fetchedAt),
                new(WalletCurrency.NGN, WalletCurrency.GBP, Round(usdToGbp / usdToNgn), fetchedAt),

                // EUR pairs
                new(WalletCurrency.EUR, WalletCurrency.USD, Round(1m / usdToEur), fetchedAt),
                new(WalletCurrency.EUR, WalletCurrency.NGN, Round(usdToNgn / usdToEur), fetchedAt),
                new(WalletCurrency.EUR, WalletCurrency.GBP, Round(usdToGbp / usdToEur), fetchedAt),

                // GBP pairs
                new(WalletCurrency.GBP, WalletCurrency.USD, Round(1m / usdToGbp), fetchedAt),
                new(WalletCurrency.GBP, WalletCurrency.NGN, Round(usdToNgn / usdToGbp), fetchedAt),
                new(WalletCurrency.GBP, WalletCurrency.EUR, Round(usdToEur / usdToGbp), fetchedAt),
            };

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch rates from OpenExchangeRates. Using fallback.");
                return GetFallbackRates();
            }
        }

        /// <summary>Static fallback rates used when the provider is unavailable or not configured.</summary>
        private static IEnumerable<ExternalRateDto> GetFallbackRates()
        {
            var now = DateTime.UtcNow;
            return
            [
                new(WalletCurrency.USD, WalletCurrency.NGN, 1_580m,   now),
            new(WalletCurrency.EUR, WalletCurrency.NGN, 1_710m,   now),
            new(WalletCurrency.GBP, WalletCurrency.NGN, 2_000m,   now),
            new(WalletCurrency.NGN, WalletCurrency.USD, 0.000633m, now),
            new(WalletCurrency.NGN, WalletCurrency.EUR, 0.000585m, now),
            new(WalletCurrency.NGN, WalletCurrency.GBP, 0.000500m, now),
            new(WalletCurrency.USD, WalletCurrency.EUR, 0.924m,   now),
            new(WalletCurrency.USD, WalletCurrency.GBP, 0.791m,   now),
            new(WalletCurrency.EUR, WalletCurrency.USD, 1.082m,   now),
            new(WalletCurrency.EUR, WalletCurrency.GBP, 0.856m,   now),
            new(WalletCurrency.GBP, WalletCurrency.USD, 1.264m,   now),
            new(WalletCurrency.GBP, WalletCurrency.EUR, 1.168m,   now),
        ];
        }

        private static decimal Round(decimal value) => Math.Round(value, 8);
    }

    // ─── RATE REFRESH BACKGROUND JOB ─────────────────────────────────────────────

    /// <summary>
    /// Hosted service that refreshes FX rates every 60 seconds.
    /// Uses IServiceScopeFactory to resolve scoped services (IFxRateService) safely.
    /// </summary>
    public class FxRateRefreshJob : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<FxRateRefreshJob> _logger;
        private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(60);

        public FxRateRefreshJob(IServiceScopeFactory scopeFactory, ILogger<FxRateRefreshJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("FX rate refresh job started. Interval: {Interval}s.", RefreshInterval.TotalSeconds);

            // Initial fetch on startup
            await RefreshAsync();

            using var timer = new PeriodicTimer(RefreshInterval);
            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RefreshAsync();
            }
        }

        private async Task RefreshAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var fxService = scope.ServiceProvider.GetRequiredService<IFxRateService>();

            try
            {
                await fxService.RefreshRatesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in FX rate refresh job.");
            }
        }
    }
}
