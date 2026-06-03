using InnPay.Application.Common;
using InnPay.Application.DTOs.Request;
using InnPay.Application.DTOs.Response;
using InnPay.Application.Interfaces;
using InnPay.Domain.Entities;
using InnPay.Domain.Enums;
using InnPay.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnPay.Application.Services
{
    public class WalletService : IWalletService
    {
        private readonly IUnitOfWork _uow;
        private readonly IFxRateService _fxRateService;

        // Supported currencies in display order
        private static readonly WalletCurrency[] AllCurrencies =
            [WalletCurrency.NGN, WalletCurrency.USD, WalletCurrency.EUR, WalletCurrency.GBP];

        public WalletService(IUnitOfWork uow, IFxRateService fxRateService)
        {
            _uow = uow;
            _fxRateService = fxRateService;
        }

        // ─── GET WALLETS ─────────────────────────────────────────────────────────

        public async Task<ServiceResult<AccountWalletsResponse>> GetWalletsAsync(Guid accountId, CancellationToken cancellationToken = default)
        {
            var account = await _uow.Accounts.GetByIdAsync(accountId);
            if (account is null)
                return ServiceResult<AccountWalletsResponse>.Fail("Account not found.", 404);

            var wallets = (await _uow.Wallets.GetByAccountIdAsync(accountId)).ToList();

            var walletDtos = wallets.Select(MapToDetail).ToList();

            // Compute approximate total NGN value
            var totalNgn = await ComputeTotalInNgnAsync(wallets);

            return ServiceResult<AccountWalletsResponse>.Success(new AccountWalletsResponse
            {
                AccountId = accountId,
                AccountType = account.AccountType,
                Wallets = walletDtos,
                TotalBalanceInNgn = totalNgn
            });
        }

        // ─── ACTIVATE WALLET ─────────────────────────────────────────────────────

        public async Task<ServiceResult<WalletDetailResponse>> ActivateWalletAsync(ActivateWalletRequest request, CancellationToken cancellationToken = default)
        {
            var account = await _uow.Accounts.GetByIdAsync(request.AccountId);
            if (account is null)
                return ServiceResult<WalletDetailResponse>.Fail("Account not found.", 404);

            // ── Eligibility checks ──────────────────────────────────────────────

            if (account.Status != AccountStatus.Active)
                return ServiceResult<WalletDetailResponse>.Fail("Account must be active before activating currency wallets.");

            if (request.Currency != WalletCurrency.NGN)
            {
                var eligible = (account.AccountType, account.KycTier) switch
                {
                    (AccountType.Personal, KycTier.Tier2) => true,
                    (AccountType.Personal, KycTier.CorporateVerified) => true,
                    (AccountType.Business, KycTier.BusinessVerified) => true,
                    (AccountType.Business, KycTier.CorporateVerified) => true,
                    (AccountType.Corporate, KycTier.CorporateVerified) => true,
                    _ => false
                };

                if (!eligible)
                    return ServiceResult<WalletDetailResponse>.Fail(
                        $"{request.Currency} wallet requires Tier 2 KYC (Personal) or verified KYC (Business/Corporate).", 403);
            }

            // ── Existing wallet check ────────────────────────────────────────────

            var existing = await _uow.Wallets.GetByAccountAndCurrencyAsync(request.AccountId, request.Currency);
            if (existing is not null)
            {
                if (existing.IsActive)
                    return ServiceResult<WalletDetailResponse>.Fail($"{request.Currency} wallet is already active.");

                // Reactivate a previously closed wallet
                existing.IsActive = true;
                existing.Status = WalletStatus.Active;
                existing.UpdatedAt = DateTime.UtcNow;
                await _uow.Wallets.UpdateAsync(existing);
                await _uow.SaveChangesAsync(cancellationToken);
                return ServiceResult<WalletDetailResponse>.Success(MapToDetail(existing));
            }

            // ── Determine model ──────────────────────────────────────────────────

            var model = DetermineModel(account, request);

            var wallet = new Wallet
            {
                AccountId = request.AccountId,
                Currency = request.Currency,
                Model = model,
                Balance = 0m,
                AvailableBalance = 0m,
                ReservedAmount = 0m,
                Status = WalletStatus.Active,
                IsActive = true,
                DailyTransactionLimit = DefaultDailyLimit(account.AccountType, request.Currency)
            };

            // ── Model A: assign wallet reference ─────────────────────────────────

            if (model == CurrencyAccountModel.WalletModel && request.Currency != WalletCurrency.NGN)
                wallet.WalletReference = GenerateWalletReference(request.Currency);

            // ── Model B: assign dedicated account numbers ─────────────────────────

            if (model == CurrencyAccountModel.StandaloneAccount)
                AssignStandaloneDetails(wallet);

            await _uow.Wallets.AddAsync(wallet);
            await _uow.SaveChangesAsync(cancellationToken);

            return ServiceResult<WalletDetailResponse>.Success(MapToDetail(wallet), 201);
        }

        // ─── SET WALLET STATUS (admin) ────────────────────────────────────────────

        public async Task<ServiceResult<WalletDetailResponse>> SetWalletStatusAsync(Guid walletId, WalletStatus status, CancellationToken cancellationToken = default)
        {
            var wallet = await _uow.Wallets.GetByIdAsync(walletId);
            if (wallet is null)
                return ServiceResult<WalletDetailResponse>.Fail("Wallet not found.", 404);

            wallet.Status = status;
            wallet.IsActive = status == WalletStatus.Active;
            wallet.UpdatedAt = DateTime.UtcNow;

            await _uow.Wallets.UpdateAsync(wallet);
            await _uow.SaveChangesAsync(cancellationToken);

            return ServiceResult<WalletDetailResponse>.Success(MapToDetail(wallet));
        }

        // ─── PRIVATE HELPERS ─────────────────────────────────────────────────────

        private static CurrencyAccountModel DetermineModel(Domain.Entities.Account account, ActivateWalletRequest request)
        {
            if (!request.RequestStandaloneAccount)
                return CurrencyAccountModel.WalletModel;

            // Only Corporate can get Model B by default; Business needs explicit eligibility
            return account.AccountType == AccountType.Corporate
                ? CurrencyAccountModel.StandaloneAccount
                : CurrencyAccountModel.WalletModel;
        }

        private static void AssignStandaloneDetails(Wallet wallet)
        {
            // In production these are issued by a banking partner (e.g. Railsbank, Currencycloud).
            // Here we generate realistic-format placeholders that get replaced by the real values
            // when the partner's webhook confirms provisioning.
            switch (wallet.Currency)
            {
                case WalletCurrency.USD:
                    wallet.Iban = GenerateVirtualIban("US");
                    wallet.SwiftCode = "INNPUS33";
                    wallet.BankName = "InnPay Virtual Banking (USD)";
                    break;

                case WalletCurrency.EUR:
                    wallet.Iban = GenerateVirtualIban("DE");     // SEPA-compatible DE IBAN format
                    wallet.SwiftCode = "INNPDE22";
                    wallet.BankName = "InnPay Virtual Banking (EUR)";
                    break;

                case WalletCurrency.GBP:
                    wallet.SortCode = GenerateSortCode();
                    wallet.UkAccountNumber = GenerateUkAccountNumber();
                    wallet.SwiftCode = "INNPGB2L";
                    wallet.BankName = "InnPay Virtual Banking (GBP)";
                    break;
            }
        }

        private async Task<decimal> ComputeTotalInNgnAsync(List<Wallet> wallets)
        {
            var total = 0m;
            foreach (var wallet in wallets.Where(w => w.IsActive && w.Balance > 0))
            {
                if (wallet.Currency == WalletCurrency.NGN)
                {
                    total += wallet.Balance;
                }
                else
                {
                    var rateResult = await _fxRateService.GetRateAsync(wallet.Currency, WalletCurrency.NGN);
                    if (rateResult.IsSuccess && rateResult.Data is not null)
                        total += wallet.Balance * rateResult.Data.CustomerRate;
                }
            }
            return Math.Round(total, 2);
        }

        private static decimal DefaultDailyLimit(AccountType accountType, WalletCurrency currency) =>
            (accountType, currency) switch
            {
                (AccountType.Personal, WalletCurrency.NGN) => 5_000_000m,
                (AccountType.Business, WalletCurrency.NGN) => 10_000_000m,
                (AccountType.Corporate, _) => 0m,        // custom / negotiated
                _ => 0m         // FX wallets: set by compliance
            };

        private static WalletDetailResponse MapToDetail(Wallet w) => new()
        {
            WalletId = w.Id,
            AccountId = w.AccountId,
            Currency = w.Currency,
            CurrencySymbol = CurrencySymbol(w.Currency),
            Model = w.Model,
            Status = w.Status,
            Balance = w.Balance,
            AvailableBalance = w.AvailableBalance,
            ReservedAmount = w.ReservedAmount,
            DailyTransactionLimit = w.DailyTransactionLimit,
            WalletReference = w.WalletReference,
            StandaloneAccount = w.Model == CurrencyAccountModel.StandaloneAccount
                ? new StandaloneAccountDetails
                {
                    Iban = w.Iban,
                    SortCode = w.SortCode,
                    UkAccountNumber = w.UkAccountNumber,
                    SwiftCode = w.SwiftCode,
                    BankName = w.BankName
                }
                : null,
            CreatedAt = w.CreatedAt,
            UpdatedAt = w.UpdatedAt
        };

        private static string CurrencySymbol(WalletCurrency c) => c switch
        {
            WalletCurrency.NGN => "₦",
            WalletCurrency.USD => "$",
            WalletCurrency.EUR => "€",
            WalletCurrency.GBP => "£",
            _ => c.ToString()
        };

        private static string GenerateWalletReference(WalletCurrency currency)
        {
            var suffix = Random.Shared.Next(10_000_000, 99_999_999);
            return $"INN-{currency}-{suffix}";
        }

        private static string GenerateVirtualIban(string countryCode)
        {
            var checkDigits = Random.Shared.Next(10, 99);
            var bban = string.Concat(Enumerable.Range(0, 18).Select(_ => Random.Shared.Next(0, 10)));
            return $"{countryCode}{checkDigits}{bban}";
        }

        private static string GenerateSortCode()
        {
            var d = () => Random.Shared.Next(10, 99);
            return $"{d()}-{d()}-{d()}";
        }

        private static string GenerateUkAccountNumber()
            => Random.Shared.Next(10_000_000, 99_999_999).ToString();
    }
}
