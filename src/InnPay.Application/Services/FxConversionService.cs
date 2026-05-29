using InnPay.Application.Common;
using InnPay.Application.DTOs.Request;
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
    public class FxConversionService : IFxConversionService
    {
        private readonly IUnitOfWork _uow;
        private readonly IFxRateService _fxRateService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<FxConversionService> _logger;

        private const int RateLockSeconds = 30;

        public FxConversionService(
            IUnitOfWork uow,
            IFxRateService fxRateService,
            IPasswordHasher passwordHasher,
            ILogger<FxConversionService> logger)
        {
            _uow = uow;
            _fxRateService = fxRateService;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        // ─── STEP 1: INITIATE — lock rate for 30 s ────────────────────────────────

        public async Task<ServiceResult<ConversionPreviewResponse>> InitiateConversionAsync(InitiateConversionRequest request)
        {
            if (request.FromCurrency == request.ToCurrency)
                return ServiceResult<ConversionPreviewResponse>.Fail("Source and target currencies must differ.");

            // ── Account check ────────────────────────────────────────────────────
            var account = await _uow.Accounts.GetByIdAsync(request.AccountId);
            if (account is null)
                return ServiceResult<ConversionPreviewResponse>.Fail("Account not found.", 404);

            if (account.Status != AccountStatus.Active)
                return ServiceResult<ConversionPreviewResponse>.Fail("Account is not active.");

            // ── Wallet checks ────────────────────────────────────────────────────
            var fromWallet = await _uow.Wallets.GetByAccountAndCurrencyAsync(request.AccountId, request.FromCurrency);
            if (fromWallet is null || !fromWallet.IsActive || fromWallet.Status != WalletStatus.Active)
                return ServiceResult<ConversionPreviewResponse>.Fail($"{request.FromCurrency} wallet is not active.");

            var toWallet = await _uow.Wallets.GetByAccountAndCurrencyAsync(request.AccountId, request.ToCurrency);
            if (toWallet is null || !toWallet.IsActive || toWallet.Status != WalletStatus.Active)
                return ServiceResult<ConversionPreviewResponse>.Fail($"{request.ToCurrency} wallet is not active. Please activate it first.");

            // ── Pair config check ────────────────────────────────────────────────
            var config = await _uow.CurrencyPairConfigs.GetAsync(request.FromCurrency, request.ToCurrency);
            if (config is not null && !config.IsActive)
                return ServiceResult<ConversionPreviewResponse>.Fail($"Conversion from {request.FromCurrency} to {request.ToCurrency} is currently unavailable.");

            if (config is not null && request.SourceAmount < config.MinSourceAmount)
                return ServiceResult<ConversionPreviewResponse>.Fail(
                    $"Minimum conversion amount is {request.FromCurrency} {config.MinSourceAmount:N2}.");

            if (config is not null && config.MaxSourceAmount > 0 && request.SourceAmount > config.MaxSourceAmount)
                return ServiceResult<ConversionPreviewResponse>.Fail(
                    $"Maximum conversion amount is {request.FromCurrency} {config.MaxSourceAmount:N2}.");

            // ── Sufficient balance check ─────────────────────────────────────────
            if (fromWallet.AvailableBalance < request.SourceAmount)
                return ServiceResult<ConversionPreviewResponse>.Fail(
                    $"Insufficient balance. Available: {request.FromCurrency} {fromWallet.AvailableBalance:N2}.");

            // ── Rate fetch ───────────────────────────────────────────────────────
            var rateResult = await _fxRateService.GetRateAsync(request.FromCurrency, request.ToCurrency);
            if (!rateResult.IsSuccess || rateResult.Data is null)
                return ServiceResult<ConversionPreviewResponse>.Fail("Exchange rate is temporarily unavailable. Please try again.", 503);

            var rate = rateResult.Data;
            if (rate.IsStale)
                return ServiceResult<ConversionPreviewResponse>.Fail("Exchange rates are being refreshed. Please try again in a moment.", 503);

            // ── Fee calculation ──────────────────────────────────────────────────
            var grossConverted = Math.Round(request.SourceAmount * rate.CustomerRate, 4);
            var (feeAmount, netConverted) = CalculateFee(grossConverted, config);

            // ── Fetch stored FxRate entity for the lock FK ───────────────────────
            var fxRateEntity = await _uow.FxRates.GetLatestAsync(request.FromCurrency, request.ToCurrency);
            if (fxRateEntity is null)
                return ServiceResult<ConversionPreviewResponse>.Fail("Rate record not found.", 503);

            // ── Reserve funds (soft hold) ────────────────────────────────────────
            fromWallet.ReservedAmount += request.SourceAmount;
            fromWallet.AvailableBalance -= request.SourceAmount;
            fromWallet.UpdatedAt = DateTime.UtcNow;
            await _uow.Wallets.UpdateAsync(fromWallet);

            // ── Create rate lock ─────────────────────────────────────────────────
            var rateLock = new FxRateLock
            {
                AccountId = request.AccountId,
                FxRateId = fxRateEntity.Id,
                FromCurrency = request.FromCurrency,
                ToCurrency = request.ToCurrency,
                SourceAmount = request.SourceAmount,
                LockedCustomerRate = rate.CustomerRate,
                EstimatedConvertedAmount = grossConverted,
                EstimatedFeeAmount = feeAmount,
                ExpiresAt = DateTime.UtcNow.AddSeconds(RateLockSeconds),
                IsUsed = false
            };

            await _uow.FxRateLocks.AddAsync(rateLock);
            await _uow.SaveChangesAsync();

            return ServiceResult<ConversionPreviewResponse>.Success(new ConversionPreviewResponse
            {
                RateLockId = rateLock.Id,
                AccountId = request.AccountId,
                FromCurrency = request.FromCurrency,
                ToCurrency = request.ToCurrency,
                SourceAmount = request.SourceAmount,
                LockedCustomerRate = rate.CustomerRate,
                GrossConvertedAmount = grossConverted,
                FeeAmount = feeAmount,
                FeeCurrency = request.ToCurrency.ToString(),
                NetConvertedAmount = netConverted,
                RateSummary = $"1 {request.FromCurrency} = {rate.CustomerRate:N4} {request.ToCurrency}",
                LockExpiresAt = rateLock.ExpiresAt
            });
        }

        // ─── STEP 2: CONFIRM — execute the conversion ─────────────────────────────

        public async Task<ServiceResult<ConversionResultResponse>> ConfirmConversionAsync(ConfirmConversionRequest request)
        {
            // ── Fetch and validate rate lock ─────────────────────────────────────
            var rateLock = await _uow.FxRateLocks.GetByIdAsync(request.RateLockId);
            if (rateLock is null)
                return ServiceResult<ConversionResultResponse>.Fail("Rate lock not found.", 404);

            if (rateLock.AccountId != request.AccountId)
                return ServiceResult<ConversionResultResponse>.Fail("Rate lock does not belong to this account.", 403);

            if (rateLock.IsUsed)
                return ServiceResult<ConversionResultResponse>.Fail("This rate lock has already been used.");

            if (rateLock.IsExpired)
            {
                // Release the reserved funds
                await ReleaseReservedFundsAsync(rateLock);
                return ServiceResult<ConversionResultResponse>.Fail(
                    "The rate lock has expired (30-second window). Please initiate a new conversion.", 422);
            }

            // ── Validate transaction PIN ─────────────────────────────────────────
            var user = await _uow.Users.GetByIdAsync(
                (await _uow.Accounts.GetByIdAsync(request.AccountId))!.UserId);

            if (user is null || !_passwordHasher.Verify(request.TransactionPin, user.TransactionPinHash))
                return ServiceResult<ConversionResultResponse>.Fail("Invalid transaction PIN.", 401);

            // ── Re-fetch wallets ─────────────────────────────────────────────────
            var fromWallet = await _uow.Wallets.GetByAccountAndCurrencyAsync(request.AccountId, rateLock.FromCurrency);
            var toWallet = await _uow.Wallets.GetByAccountAndCurrencyAsync(request.AccountId, rateLock.ToCurrency);

            if (fromWallet is null || toWallet is null)
                return ServiceResult<ConversionResultResponse>.Fail("One or more wallets could not be found.", 500);

            // ── Verify reserved balance still covers the amount ──────────────────
            if (fromWallet.ReservedAmount < rateLock.SourceAmount || fromWallet.Balance < rateLock.SourceAmount)
            {
                await ReleaseReservedFundsAsync(rateLock);
                return ServiceResult<ConversionResultResponse>.Fail("Insufficient balance to complete this conversion.");
            }

            // ── Recalculate fee (in case config changed — defensive) ─────────────
            var config = await _uow.CurrencyPairConfigs.GetAsync(rateLock.FromCurrency, rateLock.ToCurrency);
            var grossConverted = Math.Round(rateLock.SourceAmount * rateLock.LockedCustomerRate, 4);
            var (feeAmount, netConverted) = CalculateFee(grossConverted, config);

            // ── Create FxTransaction record ───────────────────────────────────────
            var fxTx = new FxTransaction
            {
                AccountId = request.AccountId,
                RateLockId = rateLock.Id,
                FromWalletId = fromWallet.Id,
                ToWalletId = toWallet.Id,
                FromCurrency = rateLock.FromCurrency,
                ToCurrency = rateLock.ToCurrency,
                SourceAmount = rateLock.SourceAmount,
                AppliedRate = rateLock.LockedCustomerRate,
                GrossConvertedAmount = grossConverted,
                FeeAmount = feeAmount,
                FeeCurrency = rateLock.ToCurrency,
                NetConvertedAmount = netConverted,
                Status = FxConversionStatus.Pending,
                Reference = GenerateReference(),
                ConvertedAt = DateTime.UtcNow
            };

            await _uow.FxTransactions.AddAsync(fxTx);

            // ── Debit source wallet ───────────────────────────────────────────────
            fromWallet.Balance -= rateLock.SourceAmount;
            fromWallet.ReservedAmount -= rateLock.SourceAmount;
            // AvailableBalance was already reduced at Step 1; no change needed here
            fromWallet.UpdatedAt = DateTime.UtcNow;
            await _uow.Wallets.UpdateAsync(fromWallet);

            // ── Credit destination wallet ─────────────────────────────────────────
            toWallet.Balance += netConverted;
            toWallet.AvailableBalance += netConverted;
            toWallet.UpdatedAt = DateTime.UtcNow;
            await _uow.Wallets.UpdateAsync(toWallet);

            // ── Mark lock as used ─────────────────────────────────────────────────
            rateLock.IsUsed = true;
            rateLock.UpdatedAt = DateTime.UtcNow;
            await _uow.FxRateLocks.UpdateAsync(rateLock);

            // ── Persist ───────────────────────────────────────────────────────────
            fxTx.Status = FxConversionStatus.Completed;
            await _uow.FxTransactions.UpdateAsync(fxTx);
            await _uow.SaveChangesAsync();

            _logger.LogInformation(
                "FX conversion completed. Ref={Ref} | {From}→{To} | {Source} → {Net} | Fee={Fee}",
                fxTx.Reference, fxTx.FromCurrency, fxTx.ToCurrency,
                fxTx.SourceAmount, fxTx.NetConvertedAmount, fxTx.FeeAmount);

            return ServiceResult<ConversionResultResponse>.Success(new ConversionResultResponse
            {
                FxTransactionId = fxTx.Id,
                Reference = fxTx.Reference,
                Status = FxConversionStatus.Completed,
                FromCurrency = fxTx.FromCurrency,
                ToCurrency = fxTx.ToCurrency,
                SourceAmountDebited = fxTx.SourceAmount,
                NetAmountCredited = fxTx.NetConvertedAmount,
                FeeCharged = fxTx.FeeAmount,
                AppliedRate = fxTx.AppliedRate,
                NewFromWalletBalance = fromWallet.Balance,
                NewToWalletBalance = toWallet.Balance,
                ConvertedAt = fxTx.ConvertedAt
            });
        }

        // ─── CONVERSION HISTORY ───────────────────────────────────────────────────

        public async Task<ServiceResult<PagedFxTransactionsResponse>> GetConversionHistoryAsync(
            Guid accountId, int page, int pageSize)
        {
            var account = await _uow.Accounts.GetByIdAsync(accountId);
            if (account is null)
                return ServiceResult<PagedFxTransactionsResponse>.Fail("Account not found.", 404);

            var transactions = (await _uow.FxTransactions.GetByAccountIdAsync(accountId, page, pageSize)).ToList();

            return ServiceResult<PagedFxTransactionsResponse>.Success(new PagedFxTransactionsResponse
            {
                Page = page,
                PageSize = pageSize,
                Items = transactions.Select(t => new FxTransactionResponse
                {
                    Id = t.Id,
                    Reference = t.Reference,
                    FromCurrency = t.FromCurrency,
                    ToCurrency = t.ToCurrency,
                    SourceAmount = t.SourceAmount,
                    NetConvertedAmount = t.NetConvertedAmount,
                    FeeAmount = t.FeeAmount,
                    AppliedRate = t.AppliedRate,
                    Status = t.Status,
                    ConvertedAt = t.ConvertedAt
                }).ToList()
            });
        }

        // ─── PRIVATE HELPERS ─────────────────────────────────────────────────────

        private static (decimal fee, decimal net) CalculateFee(decimal grossConverted, CurrencyPairConfig? config)
        {
            if (config is null)
                return (0m, grossConverted);

            // Percentage fee applied on gross converted amount
            var percentFee = Math.Round(grossConverted * (config.PercentageFee / 100m), 4);

            // Total fee = flat + percentage
            var totalFee = Math.Round(config.FlatFee + percentFee, 4);
            var net = Math.Max(0m, grossConverted - totalFee);

            return (totalFee, Math.Round(net, 4));
        }

        /// <summary>Release the soft hold placed on the source wallet when a lock expires unused.</summary>
        private async Task ReleaseReservedFundsAsync(FxRateLock rateLock)
        {
            var fromWallet = await _uow.Wallets.GetByAccountAndCurrencyAsync(rateLock.AccountId, rateLock.FromCurrency);
            if (fromWallet is null) return;

            fromWallet.ReservedAmount = Math.Max(0, fromWallet.ReservedAmount - rateLock.SourceAmount);
            fromWallet.AvailableBalance += rateLock.SourceAmount;
            fromWallet.UpdatedAt = DateTime.UtcNow;

            rateLock.IsUsed = true;
            rateLock.UpdatedAt = DateTime.UtcNow;

            await _uow.Wallets.UpdateAsync(fromWallet);
            await _uow.FxRateLocks.UpdateAsync(rateLock);
            await _uow.SaveChangesAsync();
        }

        private static string GenerateReference()
        {
            var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
            var suffix = Random.Shared.Next(10_000_000, 99_999_999);
            return $"FX-{datePart}-{suffix}";
        }
    }
}
