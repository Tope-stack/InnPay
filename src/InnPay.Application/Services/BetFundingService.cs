using InnPay.Application.Common;
using InnPay.Application.DTOs.Request;
using InnPay.Application.DTOs.Response;
using InnPay.Application.Interfaces;
using InnPay.Domain.Entities;
using InnPay.Domain.Enums;
using InnPay.Domain.Interfaces;

namespace InnPay.Application.Services;

public class BetFundingService : IBetFundingService
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;
    private readonly IBettingProvider _bettingProvider;

    // CBN daily limits per platform (NGN) — configurable via admin panel in future
    private static readonly Dictionary<BettingPlatform, decimal> PlatformLimits = new()
    {
        { BettingPlatform.Bet9ja,    200_000m },
        { BettingPlatform.Sportybet, 200_000m },
        { BettingPlatform.OneXBet,   200_000m }
    };

    public BetFundingService(IUnitOfWork uow, IPasswordHasher hasher, IBettingProvider bettingProvider)
    {
        _uow = uow;
        _hasher = hasher;
        _bettingProvider = bettingProvider;
    }

    public Task<ServiceResult<IEnumerable<BettingPlatformResponse>>> GetPlatformsAsync(
        CancellationToken cancellationToken = default)
    {
        var platforms = new List<BettingPlatformResponse>
        {
            new() { Platform = BettingPlatform.Bet9ja, Name = "Bet9ja", MinAmount = 100m, MaxAmount = 200_000m },
            new() { Platform = BettingPlatform.Sportybet, Name = "SportyBet", MinAmount = 100m, MaxAmount = 200_000m },
            new() { Platform = BettingPlatform.OneXBet, Name = "1xBet", MinAmount = 100m, MaxAmount = 200_000m }
        };

        return Task.FromResult(ServiceResult<IEnumerable<BettingPlatformResponse>>.Success(platforms));
    }

    public async Task<ServiceResult<BetFundingResponse>> FundAsync(
        FundBettingAccountRequest request, CancellationToken cancellationToken = default)
    {
        var account = await _uow.Accounts.GetByIdAsync(request.AccountId);
        if (account is null)
            return ServiceResult<BetFundingResponse>.Fail("Account not found.", 404);

        if (!_hasher.Verify(request.TransactionPin, account.User?.TransactionPinHash ?? ""))
            return ServiceResult<BetFundingResponse>.Fail("Invalid transaction PIN.", 401);

        var wallet = await _uow.Wallets.GetByIdAsync(request.WalletId);
        if (wallet is null || wallet.AccountId != request.AccountId)
            return ServiceResult<BetFundingResponse>.Fail("Wallet not found.", 404);

        if (!wallet.IsActive)
            return ServiceResult<BetFundingResponse>.Fail("Wallet is not active.");

        if (wallet.AvailableBalance < request.Amount)
            return ServiceResult<BetFundingResponse>.Fail("Insufficient balance.");

        // CBN limit enforcement
        var platformLimit = PlatformLimits.GetValueOrDefault(request.Platform, 200_000m);
        if (request.Amount > platformLimit)
            return ServiceResult<BetFundingResponse>.Fail($"Amount exceeds the daily limit of ₦{platformLimit:N0} for this platform.", 422);

        // Flag if amount exceeds CBN regulatory threshold (₦100,000)
        var isFlagged = request.Amount > 100_000m;

        var reference = $"INN-BET-{Guid.NewGuid().ToString("N")[..10].ToUpper()}";

        // Debit wallet
        wallet.Balance -= request.Amount;
        wallet.AvailableBalance -= request.Amount;
        await _uow.Wallets.UpdateAsync(wallet);

        var funding = new BetFunding
        {
            AccountId = request.AccountId,
            WalletId = request.WalletId,
            Platform = request.Platform,
            BettingUserId = request.BettingUserId,
            Amount = request.Amount,
            Status = PaymentStatus.Processing,
            Reference = reference,
            IsFlagged = isFlagged
        };

        await _uow.BetFundings.AddAsync(funding);

        // Call provider
        var providerResult = await _bettingProvider.FundAsync(
            request.Platform, request.BettingUserId, request.Amount, reference);

        if (providerResult.IsSuccess)
        {
            funding.Status = PaymentStatus.Completed;
            funding.ProviderReference = providerResult.ProviderReference;
        }
        else
        {
            funding.Status = PaymentStatus.Failed;
            funding.FailureReason = providerResult.Error;
            // Refund
            wallet.Balance += request.Amount;
            wallet.AvailableBalance += request.Amount;
            await _uow.Wallets.UpdateAsync(wallet);
        }

        await _uow.BetFundings.UpdateAsync(funding);
        await _uow.SaveChangesAsync(cancellationToken);

        if (!providerResult.IsSuccess)
            return ServiceResult<BetFundingResponse>.Fail(providerResult.Error ?? "Funding failed.", 422);

        return ServiceResult<BetFundingResponse>.Success(new BetFundingResponse
        {
            FundingId = funding.Id,
            Reference = funding.Reference,
            Platform = funding.Platform,
            BettingUserId = funding.BettingUserId,
            Amount = funding.Amount,
            Status = funding.Status,
            ProviderReference = funding.ProviderReference,
            CreatedAt = funding.CreatedAt
        }, 201);
    }

    public async Task<ServiceResult<IEnumerable<BetFundingResponse>>> GetHistoryAsync(
        Guid accountId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var fundings = await _uow.BetFundings.GetByAccountIdAsync(accountId, page, pageSize);
        return ServiceResult<IEnumerable<BetFundingResponse>>.Success(
            fundings.Select(f => new BetFundingResponse
            {
                FundingId = f.Id,
                Reference = f.Reference,
                Platform = f.Platform,
                BettingUserId = f.BettingUserId,
                Amount = f.Amount,
                Status = f.Status,
                ProviderReference = f.ProviderReference,
                CreatedAt = f.CreatedAt
            }));
    }
}
