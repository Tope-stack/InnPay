using InnPay.Application.Common;
using InnPay.Application.DTOs.Response;
using InnPay.Application.Interfaces;
using InnPay.Domain.Entities;
using InnPay.Domain.Enums;
using InnPay.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnPay.Application.Services
{
    public class FxRateService : IFxRateService
    {
        private readonly IUnitOfWork _uow;
        private readonly IFxProviderService _provider;
        private readonly ILogger<FxRateService> _logger;

        // All pairs the platform supports (directional — we store both directions)
        private static readonly (WalletCurrency From, WalletCurrency To)[] SupportedPairs =
        [
            (WalletCurrency.USD, WalletCurrency.NGN),
            (WalletCurrency.EUR, WalletCurrency.NGN),
            (WalletCurrency.GBP, WalletCurrency.NGN),
            (WalletCurrency.USD, WalletCurrency.EUR),
            (WalletCurrency.USD, WalletCurrency.GBP),
            (WalletCurrency.EUR, WalletCurrency.GBP),
            (WalletCurrency.NGN, WalletCurrency.USD),
            (WalletCurrency.NGN, WalletCurrency.EUR),
            (WalletCurrency.NGN, WalletCurrency.GBP),
            (WalletCurrency.EUR, WalletCurrency.USD),
            (WalletCurrency.GBP, WalletCurrency.USD),
            (WalletCurrency.GBP, WalletCurrency.EUR),
         ];

        private const int RateLifetimeSeconds = 60;

        public FxRateService(IUnitOfWork uow, IFxProviderService provider, ILogger<FxRateService> logger)
        {
            _uow = uow;
            _provider = provider;
            _logger = logger;
        }

        // ─── GET ALL RATES ────────────────────────────────────────────────────────

        public async Task<ServiceResult<AllFxRatesResponse>> GetAllRatesAsync(CancellationToken cancellationToken = default)
        {
            var rates = (await _uow.FxRates.GetAllLatestAsync()).ToList();

            var dtos = rates.Select(MapToDto).ToList();
            var hasStale = dtos.Any(r => r.IsStale);

            return ServiceResult<AllFxRatesResponse>.Success(new AllFxRatesResponse
            {
                GeneratedAt = DateTime.UtcNow,
                HasStaleRates = hasStale,
                Rates = dtos
            });
        }

        // ─── GET SINGLE RATE ─────────────────────────────────────────────────────

        public async Task<ServiceResult<FxRateResponse>> GetRateAsync(WalletCurrency from, WalletCurrency to, CancellationToken cancellationToken = default)
        {
            if (from == to)
                return ServiceResult<FxRateResponse>.Fail("Source and target currencies must differ.");

            var rate = await _uow.FxRates.GetLatestAsync(from, to);
            if (rate is null)
                return ServiceResult<FxRateResponse>.Fail($"No rate available for {from}/{to}. Please try again shortly.", 503);

            return ServiceResult<FxRateResponse>.Success(MapToDto(rate));
        }

        // ─── REFRESH RATES (background job) ──────────────────────────────────────

        public async Task RefreshRatesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var externalRates = (await _provider.FetchRatesAsync(cancellationToken)).ToList();
                if (!externalRates.Any())
                {
                    _logger.LogWarning("FX provider returned no rates during refresh.");
                    return;
                }

                // Build a lookup of mid-rates by pair
                var midRateLookup = externalRates
                    .ToDictionary(r => (r.FromCurrency, r.ToCurrency), r => r.MidRate);

                var newRates = new List<FxRate>();
                var now = DateTime.UtcNow;

                foreach (var (from, to) in SupportedPairs)
                {
                    if (!midRateLookup.TryGetValue((from, to), out var midRate))
                    {
                        // Derive from inverse if available
                        if (midRateLookup.TryGetValue((to, from), out var inverseMid) && inverseMid != 0)
                            midRate = Math.Round(1m / inverseMid, 8);
                        else
                            continue;   // skip pair if no data
                    }

                    var config = await _uow.CurrencyPairConfigs.GetAsync(from, to);
                    var spreadBps = config?.SpreadBps ?? 150m;   // default 1.5% spread if not configured

                    var customerRate = ApplySpread(midRate, spreadBps, from, to);

                    newRates.Add(new FxRate
                    {
                        FromCurrency = from,
                        ToCurrency = to,
                        MidRate = midRate,
                        CustomerRate = customerRate,
                        SpreadBps = spreadBps,
                        Source = FxRateSource.OpenExchangeRates,
                        FetchedAt = now,
                        ExpiresAt = now.AddSeconds(RateLifetimeSeconds)
                    });
                }

                await _uow.FxRates.BulkAddAsync(newRates);
                await _uow.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("FX rates refreshed. {Count} pairs updated at {Time}.", newRates.Count, now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh FX rates from provider.");
            }
        }

        // ─── HELPERS ─────────────────────────────────────────────────────────────

        /// <summary>
        /// For currencies we sell (user buys foreign currency), we mark DOWN the mid-rate.
        /// For currencies we buy (user sells foreign currency), we mark UP.
        /// This creates a bid/ask spread that is InnPay's margin.
        /// </summary>
        private static decimal ApplySpread(decimal midRate, decimal spreadBps, WalletCurrency from, WalletCurrency to)
        {
            var spreadFactor = spreadBps / 10_000m;   // bps → fraction

            // When selling NGN to buy FX (e.g. NGN → USD), the user gets less USD → mark rate down
            // When selling FX to get NGN (e.g. USD → NGN), the user gets less NGN → mark rate down
            // Both directions reduce from mid, just applied to the appropriate side
            var customerRate = midRate * (1m - spreadFactor);
            return Math.Round(customerRate, 6);
        }

        private static FxRateResponse MapToDto(FxRate r) => new()
        {
            FromCurrency = r.FromCurrency,
            ToCurrency = r.ToCurrency,
            Pair = $"{r.FromCurrency}/{r.ToCurrency}",
            MidRate = r.MidRate,
            CustomerRate = r.CustomerRate,
            SpreadBps = r.SpreadBps,
            FetchedAt = r.FetchedAt,
            ExpiresAt = r.ExpiresAt,
            IsStale = DateTime.UtcNow > r.ExpiresAt
        };
    }
}
