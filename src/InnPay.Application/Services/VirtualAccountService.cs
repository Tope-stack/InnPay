using InnPay.Application.Common;
using InnPay.Application.DTOs.Request;
using InnPay.Application.DTOs.Response;
using InnPay.Application.Interfaces;
using InnPay.Domain.Entities;
using InnPay.Domain.Enums;
using InnPay.Domain.Interfaces;

namespace InnPay.Application.Services;

public class VirtualAccountService : IVirtualAccountService
{
    private readonly IUnitOfWork _uow;
    private readonly IVirtualAccountProvider _provider;

    public VirtualAccountService(IUnitOfWork uow, IVirtualAccountProvider provider)
    {
        _uow = uow;
        _provider = provider;
    }

    public async Task<ServiceResult<VirtualAccountResponse>> GenerateAsync(
        GenerateVirtualAccountRequest request, CancellationToken cancellationToken = default)
    {
        var account = await _uow.Accounts.GetByIdAsync(request.AccountId);
        if (account is null)
            return ServiceResult<VirtualAccountResponse>.Fail("Account not found.", 404);

        if (account.Status != AccountStatus.Active)
            return ServiceResult<VirtualAccountResponse>.Fail("Account must be active to generate a virtual account.");

        var wallet = await _uow.Wallets.GetByIdAsync(request.WalletId);
        if (wallet is null || wallet.AccountId != request.AccountId || wallet.Currency != WalletCurrency.NGN)
            return ServiceResult<VirtualAccountResponse>.Fail("NGN wallet not found for this account.", 404);

        // Personal/Business: one primary virtual account; Corporate: unlimited (by label)
        if (account.AccountType != AccountType.Corporate && string.IsNullOrEmpty(request.Label))
        {
            var existing = await _uow.VirtualAccounts.GetPrimaryByAccountIdAsync(request.AccountId);
            if (existing is not null)
                return ServiceResult<VirtualAccountResponse>.Success(MapToResponse(existing));
        }

        var reference = $"INN-VA-{Guid.NewGuid().ToString("N")[..10].ToUpper()}";
        var accountName = account.AccountType == AccountType.Personal
            ? account.User?.FullName ?? "InnPay User"
            : account.BusinessName ?? account.CorporateName ?? "InnPay Business";

        var providerResult = await _provider.GenerateAsync(accountName, reference);
        if (!providerResult.IsSuccess)
            return ServiceResult<VirtualAccountResponse>.Fail(providerResult.Error ?? "Failed to generate virtual account.", 503);

        var virtualAccount = new VirtualAccount
        {
            AccountId = request.AccountId,
            WalletId = request.WalletId,
            AccountNumber = providerResult.AccountNumber!,
            BankName = providerResult.BankName!,
            BankCode = providerResult.BankCode!,
            AccountName = providerResult.AccountName!,
            Label = request.Label,
            IsActive = true
        };

        await _uow.VirtualAccounts.AddAsync(virtualAccount);
        await _uow.SaveChangesAsync(cancellationToken);

        return ServiceResult<VirtualAccountResponse>.Success(MapToResponse(virtualAccount), 201);
    }

    public async Task<ServiceResult<IEnumerable<VirtualAccountResponse>>> GetByAccountAsync(
        Guid accountId, CancellationToken cancellationToken = default)
    {
        var accounts = await _uow.VirtualAccounts.GetByAccountIdAsync(accountId);
        return ServiceResult<IEnumerable<VirtualAccountResponse>>.Success(
            accounts.Select(MapToResponse));
    }

    private static VirtualAccountResponse MapToResponse(VirtualAccount v) => new()
    {
        VirtualAccountId = v.Id,
        AccountNumber = v.AccountNumber,
        BankName = v.BankName,
        AccountName = v.AccountName,
        Label = v.Label,
        IsActive = v.IsActive,
        CreatedAt = v.CreatedAt
    };
}
