using InnPay.Application.Common;
using InnPay.Application.DTOs.Request;
using InnPay.Application.DTOs.Response;
using InnPay.Application.Interfaces;
using InnPay.Domain.Entities;
using InnPay.Domain.Enums;
using InnPay.Domain.Interfaces;

namespace InnPay.Application.Services;

public class VirtualCardService : IVirtualCardService
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;
    private readonly ICardIssuerProvider _cardIssuer;

    public VirtualCardService(IUnitOfWork uow, IPasswordHasher hasher, ICardIssuerProvider cardIssuer)
    {
        _uow = uow;
        _hasher = hasher;
        _cardIssuer = cardIssuer;
    }

    public async Task<ServiceResult<VirtualCardResponse>> CreateAsync(
        CreateVirtualCardRequest request, CancellationToken cancellationToken = default)
    {
        var account = await _uow.Accounts.GetByIdAsync(request.AccountId);
        if (account is null)
            return ServiceResult<VirtualCardResponse>.Fail("Account not found.", 404);

        if (!_hasher.Verify(request.TransactionPin, account.User?.TransactionPinHash ?? ""))
            return ServiceResult<VirtualCardResponse>.Fail("Invalid transaction PIN.", 401);

        // KYC gate — must be Tier 2 or higher
        if (account.KycTier < KycTier.Tier2)
            return ServiceResult<VirtualCardResponse>.Fail("KYC Tier 2 is required to create a virtual card.", 403);

        var wallet = await _uow.Wallets.GetByIdAsync(request.WalletId);
        if (wallet is null || wallet.AccountId != request.AccountId || wallet.Currency != WalletCurrency.USD)
            return ServiceResult<VirtualCardResponse>.Fail("A USD wallet is required to create a virtual card.", 404);

        if (!wallet.IsActive)
            return ServiceResult<VirtualCardResponse>.Fail("USD wallet is not active.");

        var accountName = account.User?.FullName ?? "InnPay User";
        var reference = $"INN-CARD-{Guid.NewGuid().ToString("N")[..10].ToUpper()}";

        var providerResult = await _cardIssuer.CreateCardAsync(accountName, reference);
        if (!providerResult.IsSuccess)
            return ServiceResult<VirtualCardResponse>.Fail(providerResult.Error ?? "Card creation failed.", 503);

        var card = new VirtualCard
        {
            AccountId = request.AccountId,
            WalletId = request.WalletId,
            PanEncrypted = providerResult.PanEncrypted!,
            Last4 = providerResult.Last4!,
            Expiry = providerResult.Expiry!,
            CvvEncrypted = providerResult.CvvEncrypted!,
            Status = VirtualCardStatus.Active,
            ProviderCardId = providerResult.ProviderCardId!
        };

        await _uow.VirtualCards.AddAsync(card);
        await _uow.SaveChangesAsync(cancellationToken);

        return ServiceResult<VirtualCardResponse>.Success(MapToResponse(card), 201);
    }

    public async Task<ServiceResult<VirtualCardDetailsResponse>> RevealDetailsAsync(
        RevealCardDetailsRequest request, CancellationToken cancellationToken = default)
    {
        var card = await _uow.VirtualCards.GetByIdAsync(request.CardId);
        if (card is null)
            return ServiceResult<VirtualCardDetailsResponse>.Fail("Card not found.", 404);

        var account = await _uow.Accounts.GetByIdAsync(card.AccountId);
        if (!_hasher.Verify(request.TransactionPin, account?.User?.TransactionPinHash ?? ""))
            return ServiceResult<VirtualCardDetailsResponse>.Fail("Invalid transaction PIN.", 401);

        // Decrypt PAN and CVV — in production use IEncryptionService
        return ServiceResult<VirtualCardDetailsResponse>.Success(new VirtualCardDetailsResponse
        {
            CardId = card.Id,
            Last4 = card.Last4,
            Expiry = card.Expiry,
            Status = card.Status,
            SpendingLimitPerTransaction = card.SpendingLimitPerTransaction,
            SpendingLimitMonthly = card.SpendingLimitMonthly,
            CreatedAt = card.CreatedAt,
            Pan = DecryptField(card.PanEncrypted),   // TODO: replace with IEncryptionService.Decrypt
            Cvv = DecryptField(card.CvvEncrypted)
        });
    }

    public async Task<ServiceResult<VirtualCardResponse>> FreezeAsync(
        CardActionRequest request, CancellationToken cancellationToken = default)
        => await SetCardStatus(request, VirtualCardStatus.Frozen,
            c => _cardIssuer.FreezeCardAsync(c.ProviderCardId), cancellationToken);

    public async Task<ServiceResult<VirtualCardResponse>> UnfreezeAsync(
        CardActionRequest request, CancellationToken cancellationToken = default)
        => await SetCardStatus(request, VirtualCardStatus.Active,
            c => _cardIssuer.UnfreezeCardAsync(c.ProviderCardId), cancellationToken);

    public async Task<ServiceResult> DeleteAsync(
        CardActionRequest request, CancellationToken cancellationToken = default)
    {
        var (card, pinError) = await ValidateCardPin(request.CardId, request.TransactionPin);
        if (pinError is not null) return ServiceResult.Fail(pinError, 401);

        await _cardIssuer.DeleteCardAsync(card!.ProviderCardId);
        card.Status = VirtualCardStatus.Terminated;
        card.TerminationReason = "Deleted by user";
        await _uow.VirtualCards.UpdateAsync(card);
        await _uow.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult<VirtualCardResponse>> SetSpendingLimitAsync(
        SetSpendingLimitRequest request, CancellationToken cancellationToken = default)
    {
        var (card, pinError) = await ValidateCardPin(request.CardId, request.TransactionPin);
        if (pinError is not null) return ServiceResult<VirtualCardResponse>.Fail(pinError, 401);

        await _cardIssuer.SetSpendingLimitAsync(card!.ProviderCardId, request.PerTransactionLimit, request.MonthlyLimit);
        card.SpendingLimitPerTransaction = request.PerTransactionLimit;
        card.SpendingLimitMonthly = request.MonthlyLimit;
        await _uow.VirtualCards.UpdateAsync(card);
        await _uow.SaveChangesAsync(cancellationToken);
        return ServiceResult<VirtualCardResponse>.Success(MapToResponse(card));
    }

    public async Task<ServiceResult<IEnumerable<CardTransactionResponse>>> GetTransactionHistoryAsync(
        Guid cardId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var txns = await _uow.CardTransactions.GetByCardIdAsync(cardId, page, pageSize);
        return ServiceResult<IEnumerable<CardTransactionResponse>>.Success(
            txns.Select(t => new CardTransactionResponse
            {
                TransactionId = t.Id,
                MerchantName = t.MerchantName,
                MerchantCategory = t.MerchantCategory,
                Amount = t.Amount,
                Currency = t.Currency,
                IsDeclined = t.IsDeclined,
                DeclineReason = t.DeclineReason,
                TransactedAt = t.TransactedAt
            }));
    }

    public async Task<ServiceResult> HandleWebhookAsync(
        string providerCardId, CardTransactionWebhookPayload payload, CancellationToken cancellationToken = default)
    {
        var card = await _uow.VirtualCards.GetByProviderCardIdAsync(providerCardId);
        if (card is null) return ServiceResult.Fail("Card not found.", 404);

        var currency = Enum.TryParse<WalletCurrency>(payload.Currency, true, out var c) ? c : WalletCurrency.USD;

        await _uow.CardTransactions.AddAsync(new CardTransaction
        {
            VirtualCardId = card.Id,
            MerchantName = payload.MerchantName,
            MerchantCategory = payload.MerchantCategory,
            Amount = payload.Amount,
            Currency = currency,
            ProviderTransactionId = payload.ProviderTransactionId,
            IsDeclined = payload.IsDeclined,
            DeclineReason = payload.DeclineReason,
            TransactedAt = payload.TransactedAt
        });

        await _uow.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<ServiceResult<VirtualCardResponse>> SetCardStatus(
        CardActionRequest request, VirtualCardStatus targetStatus,
        Func<VirtualCard, Task<bool>> providerAction, CancellationToken cancellationToken)
    {
        var (card, pinError) = await ValidateCardPin(request.CardId, request.TransactionPin);
        if (pinError is not null) return ServiceResult<VirtualCardResponse>.Fail(pinError, 401);

        await providerAction(card!);
        card!.Status = targetStatus;
        await _uow.VirtualCards.UpdateAsync(card);
        await _uow.SaveChangesAsync(cancellationToken);
        return ServiceResult<VirtualCardResponse>.Success(MapToResponse(card));
    }

    private async Task<(VirtualCard? card, string? error)> ValidateCardPin(Guid cardId, string pin)
    {
        var card = await _uow.VirtualCards.GetByIdAsync(cardId);
        if (card is null) return (null, "Card not found.");
        var account = await _uow.Accounts.GetByIdAsync(card.AccountId);
        if (!_hasher.Verify(pin, account?.User?.TransactionPinHash ?? ""))
            return (null, "Invalid transaction PIN.");
        return (card, null);
    }

    private static string DecryptField(string encrypted)
    {
        // TODO: replace with IEncryptionService.Decrypt(encrypted) when wired in
        return encrypted;
    }

    private static VirtualCardResponse MapToResponse(VirtualCard c) => new()
    {
        CardId = c.Id,
        Last4 = c.Last4,
        Expiry = c.Expiry,
        Status = c.Status,
        SpendingLimitPerTransaction = c.SpendingLimitPerTransaction,
        SpendingLimitMonthly = c.SpendingLimitMonthly,
        CreatedAt = c.CreatedAt
    };
}
