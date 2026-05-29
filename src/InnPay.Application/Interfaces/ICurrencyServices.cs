using InnPay.Application.Common;
using InnPay.Application.DTOs.Request;
using InnPay.Application.DTOs.Response;
using InnPay.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnPay.Application.Interfaces
{
    public interface IWalletService
    {
        /// <summary>GET /api/wallets/{accountId} — all wallets with balances.</summary>
        Task<ServiceResult<AccountWalletsResponse>> GetWalletsAsync(Guid accountId);

        /// <summary>POST /api/wallets/activate — activate a currency wallet (Model A or B).</summary>
        Task<ServiceResult<WalletDetailResponse>> ActivateWalletAsync(ActivateWalletRequest request);

        /// <summary>Freeze or unfreeze a specific wallet (admin action).</summary>
        Task<ServiceResult<WalletDetailResponse>> SetWalletStatusAsync(Guid walletId, WalletStatus status);
    }

    // ─── FX RATE SERVICE ──────────────────────────────────────────────────────────

    public interface IFxRateService
    {
        /// <summary>GET /api/fx/rates — all live rates for supported pairs.</summary>
        Task<ServiceResult<AllFxRatesResponse>> GetAllRatesAsync();

        /// <summary>Get the live rate for a specific pair (used internally by conversion).</summary>
        Task<ServiceResult<FxRateResponse>> GetRateAsync(WalletCurrency from, WalletCurrency to);

        /// <summary>Called by the background job every 60 seconds to refresh rates.</summary>
        Task RefreshRatesAsync();
    }

    // ─── FX CONVERSION SERVICE ────────────────────────────────────────────────────

    public interface IFxConversionService
    {
        /// <summary>
        /// POST /api/wallets/convert (Step 1) — locks the rate for 30 s and returns a preview.
        /// </summary>
        Task<ServiceResult<ConversionPreviewResponse>> InitiateConversionAsync(InitiateConversionRequest request);

        /// <summary>
        /// POST /api/wallets/convert/confirm (Step 2) — validates the PIN and executes the conversion.
        /// </summary>
        Task<ServiceResult<ConversionResultResponse>> ConfirmConversionAsync(ConfirmConversionRequest request);

        /// <summary>GET /api/wallets/conversions/{accountId} — paginated FX history.</summary>
        Task<ServiceResult<PagedFxTransactionsResponse>> GetConversionHistoryAsync(Guid accountId, int page, int pageSize);
    }

    // ─── ADMIN: PAIR CONFIG SERVICE ───────────────────────────────────────────────

    public interface ICurrencyPairConfigService
    {
        Task<ServiceResult<List<CurrencyPairConfigResponse>>> GetAllConfigsAsync();
        Task<ServiceResult<CurrencyPairConfigResponse>> UpsertConfigAsync(UpsertCurrencyPairConfigRequest request);
    }

    // ─── EXTERNAL FX PROVIDER ─────────────────────────────────────────────────────

    /// <summary>
    /// Abstraction over the third-party FX data provider (Open Exchange Rates / Fixer.io).
    /// Infrastructure implements this; Application depends only on the interface.
    /// </summary>
    public interface IFxProviderService
    {
        /// <summary>Fetch live mid-market rates for all supported pairs from the external provider.</summary>
        Task<IEnumerable<ExternalRateDto>> FetchRatesAsync();
    }

    public record ExternalRateDto(
        WalletCurrency FromCurrency,
        WalletCurrency ToCurrency,
        decimal MidRate,
        DateTime FetchedAt);

}
