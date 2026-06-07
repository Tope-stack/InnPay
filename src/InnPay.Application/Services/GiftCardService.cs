using InnPay.Application.Common;
using InnPay.Application.DTOs.Request;
using InnPay.Application.DTOs.Response;
using InnPay.Application.Interfaces;
using InnPay.Domain.Entities;
using InnPay.Domain.Enums;
using InnPay.Domain.Interfaces;

namespace InnPay.Application.Services;

public class GiftCardService : IGiftCardService
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;
    private readonly IGiftCardProvider _provider;

    public GiftCardService(IUnitOfWork uow, IPasswordHasher hasher, IGiftCardProvider provider)
    {
        _uow = uow;
        _hasher = hasher;
        _provider = provider;
    }

    public async Task<ServiceResult<IEnumerable<GiftCardCatalogueItemResponse>>> GetCatalogueAsync(
        string? countryCode, CancellationToken cancellationToken = default)
    {
        var items = await _provider.GetCatalogueAsync(countryCode);
        return ServiceResult<IEnumerable<GiftCardCatalogueItemResponse>>.Success(
            items.Select(i => new GiftCardCatalogueItemResponse
            {
                BrandName = i.BrandName,
                CountryCode = i.CountryCode,
                LogoUrl = i.LogoUrl,
                AvailableDenominationsUsd = i.Denominations.ToList()
            }));
    }

    public async Task<ServiceResult<GiftCardPurchaseResponse>> PurchaseAsync(
        PurchaseGiftCardRequest request, CancellationToken cancellationToken = default)
    {
        var account = await _uow.Accounts.GetByIdAsync(request.AccountId);
        if (account is null)
            return ServiceResult<GiftCardPurchaseResponse>.Fail("Account not found.", 404);

        if (!_hasher.Verify(request.TransactionPin, account.User?.TransactionPinHash ?? ""))
            return ServiceResult<GiftCardPurchaseResponse>.Fail("Invalid transaction PIN.", 401);

        var wallet = await _uow.Wallets.GetByIdAsync(request.WalletId);
        if (wallet is null || wallet.AccountId != request.AccountId)
            return ServiceResult<GiftCardPurchaseResponse>.Fail("Wallet not found.", 404);

        // Compute NGN equivalent (simplified — FX conversion used in production)
        var amountCharged = request.DenominationUsd * 1600m; // placeholder rate
        if (wallet.Currency != WalletCurrency.NGN)
            amountCharged = request.DenominationUsd; // USD wallet pays USD face value

        if (wallet.AvailableBalance < amountCharged)
            return ServiceResult<GiftCardPurchaseResponse>.Fail("Insufficient balance.");

        var reference = $"INN-GC-{Guid.NewGuid().ToString("N")[..10].ToUpper()}";

        // Debit wallet
        wallet.Balance -= amountCharged;
        wallet.AvailableBalance -= amountCharged;
        await _uow.Wallets.UpdateAsync(wallet);

        var providerResult = await _provider.PurchaseAsync(
            request.BrandName, request.CountryCode, request.DenominationUsd, reference);

        var purchase = new GiftCardPurchase
        {
            AccountId = request.AccountId,
            WalletId = request.WalletId,
            BrandName = request.BrandName,
            CountryCode = request.CountryCode,
            DenominationUsd = request.DenominationUsd,
            AmountCharged = amountCharged,
            WalletCurrency = wallet.Currency,
            Reference = reference
        };

        if (providerResult.IsSuccess)
        {
            purchase.Status = GiftCardStatus.Purchased;
            purchase.RedemptionCodeEncrypted = providerResult.RedemptionCodeEncrypted!;
            purchase.RedemptionPin = providerResult.RedemptionPin;
            purchase.ReloadlyOrderId = providerResult.OrderId;
        }
        else
        {
            // Refund on failure
            purchase.Status = GiftCardStatus.Purchased;  // set to purchased but mark failure reason
            purchase.FailureReason = providerResult.Error;
            wallet.Balance += amountCharged;
            wallet.AvailableBalance += amountCharged;
            await _uow.Wallets.UpdateAsync(wallet);
        }

        await _uow.GiftCardPurchases.AddAsync(purchase);
        await _uow.SaveChangesAsync(cancellationToken);

        if (!providerResult.IsSuccess)
            return ServiceResult<GiftCardPurchaseResponse>.Fail(providerResult.Error ?? "Gift card purchase failed.", 422);

        return ServiceResult<GiftCardPurchaseResponse>.Success(MapToResponse(purchase), 201);
    }

    public async Task<ServiceResult<GiftCardRevealResponse>> RevealAsync(
        RevealGiftCardRequest request, CancellationToken cancellationToken = default)
    {
        var purchase = await _uow.GiftCardPurchases.GetByIdAsync(request.PurchaseId);
        if (purchase is null)
            return ServiceResult<GiftCardRevealResponse>.Fail("Purchase not found.", 404);

        var account = await _uow.Accounts.GetByIdAsync(purchase.AccountId);
        if (!_hasher.Verify(request.TransactionPin, account?.User?.TransactionPinHash ?? ""))
            return ServiceResult<GiftCardRevealResponse>.Fail("Invalid transaction PIN.", 401);

        if (purchase.Status == GiftCardStatus.Purchased)
        {
            purchase.Status = GiftCardStatus.Revealed;
            await _uow.GiftCardPurchases.UpdateAsync(purchase);
            await _uow.SaveChangesAsync(cancellationToken);
        }

        return ServiceResult<GiftCardRevealResponse>.Success(new GiftCardRevealResponse
        {
            PurchaseId = purchase.Id,
            Reference = purchase.Reference,
            BrandName = purchase.BrandName,
            DenominationUsd = purchase.DenominationUsd,
            AmountCharged = purchase.AmountCharged,
            WalletCurrency = purchase.WalletCurrency,
            Status = purchase.Status,
            CreatedAt = purchase.CreatedAt,
            RedemptionCode = DecryptCode(purchase.RedemptionCodeEncrypted),
            RedemptionPin = purchase.RedemptionPin
        });
    }

    public async Task<ServiceResult<IEnumerable<GiftCardPurchaseResponse>>> GetPurchasesAsync(
        Guid accountId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var purchases = await _uow.GiftCardPurchases.GetByAccountIdAsync(accountId, page, pageSize);
        return ServiceResult<IEnumerable<GiftCardPurchaseResponse>>.Success(purchases.Select(MapToResponse));
    }

    private static string DecryptCode(string encrypted)
    {
        // TODO: replace with IEncryptionService.Decrypt when wired in
        return encrypted;
    }

    private static GiftCardPurchaseResponse MapToResponse(GiftCardPurchase p) => new()
    {
        PurchaseId = p.Id,
        Reference = p.Reference,
        BrandName = p.BrandName,
        DenominationUsd = p.DenominationUsd,
        AmountCharged = p.AmountCharged,
        WalletCurrency = p.WalletCurrency,
        Status = p.Status,
        CreatedAt = p.CreatedAt
    };
}
