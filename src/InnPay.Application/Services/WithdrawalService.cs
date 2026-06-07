using InnPay.Application.Common;
using InnPay.Application.DTOs.Request;
using InnPay.Application.DTOs.Response;
using InnPay.Application.Interfaces;
using InnPay.Domain.Entities;
using InnPay.Domain.Enums;
using InnPay.Domain.Interfaces;

namespace InnPay.Application.Services;

public class WithdrawalService : IWithdrawalService
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;

    public WithdrawalService(IUnitOfWork uow, IPasswordHasher hasher)
    {
        _uow = uow;
        _hasher = hasher;
    }

    public async Task<ServiceResult<WithdrawalResponse>> InitiateAsync(
        InitiateWithdrawalRequest request, CancellationToken cancellationToken = default)
    {
        var account = await _uow.Accounts.GetByIdAsync(request.AccountId);
        if (account is null)
            return ServiceResult<WithdrawalResponse>.Fail("Account not found.", 404);

        if (!_hasher.Verify(request.TransactionPin, account.User?.TransactionPinHash ?? ""))
            return ServiceResult<WithdrawalResponse>.Fail("Invalid transaction PIN.", 401);

        var wallet = await _uow.Wallets.GetByIdAsync(request.WalletId);
        if (wallet is null || wallet.AccountId != request.AccountId)
            return ServiceResult<WithdrawalResponse>.Fail("Wallet not found.", 404);

        if (!wallet.IsActive || wallet.Status != WalletStatus.Active)
            return ServiceResult<WithdrawalResponse>.Fail("Wallet is not active.");

        if (wallet.AvailableBalance < request.Amount)
            return ServiceResult<WithdrawalResponse>.Fail("Insufficient balance.");

        // Daily limit enforcement
        if (wallet.DailyTransactionLimit > 0 && request.Amount > wallet.DailyTransactionLimit)
            return ServiceResult<WithdrawalResponse>.Fail($"Amount exceeds daily withdrawal limit.");

        // Resolve destination bank details
        string bankCode, bankName, accountNumber, accountName;

        if (request.SavedBankAccountId.HasValue)
        {
            var saved = await _uow.SavedBankAccounts.GetByIdAsync(request.SavedBankAccountId.Value);
            if (saved is null || saved.AccountId != request.AccountId)
                return ServiceResult<WithdrawalResponse>.Fail("Saved bank account not found.", 404);

            bankCode = saved.BankCode;
            bankName = saved.BankName;
            accountNumber = saved.AccountNumber;
            accountName = saved.AccountName;
        }
        else
        {
            if (string.IsNullOrEmpty(request.BankCode) || string.IsNullOrEmpty(request.AccountNumber))
                return ServiceResult<WithdrawalResponse>.Fail("Provide either a saved bank account ID or bank code + account number.");

            bankCode = request.BankCode!;
            bankName = "Bank";          // resolved by name-enquiry in production
            accountNumber = request.AccountNumber!;
            accountName = "Account Holder";   // resolved by name-enquiry in production
        }

        // ── Fraud scoring ─────────────────────────────────────────────────────
        var sevenDayAvg = await _uow.Withdrawals.GetSevenDayAverageAsync(wallet.Id);
        var fraudScore = sevenDayAvg > 0
            ? (int)Math.Min(100, (request.Amount / sevenDayAvg) * 60)
            : 0;

        var reference = $"INN-WD-{Guid.NewGuid().ToString("N")[..10].ToUpper()}";

        var withdrawal = new Withdrawal
        {
            AccountId = request.AccountId,
            WalletId = request.WalletId,
            SavedBankAccountId = request.SavedBankAccountId,
            DestinationBankCode = bankCode,
            DestinationBankName = bankName,
            DestinationAccountNumber = accountNumber,
            DestinationAccountName = accountName,
            Amount = request.Amount,
            Currency = wallet.Currency,
            Status = WithdrawalStatus.Processing,
            Reference = reference,
            FraudScore = fraudScore,
            IsFlagged = fraudScore > 70
        };

        // Reserve funds
        wallet.AvailableBalance -= request.Amount;
        wallet.ReservedAmount += request.Amount;

        await _uow.Withdrawals.AddAsync(withdrawal);
        await _uow.Wallets.UpdateAsync(wallet);

        // Save bank if requested
        if (request.SaveBankAccount && !request.SavedBankAccountId.HasValue)
        {
            await _uow.SavedBankAccounts.AddAsync(new SavedBankAccount
            {
                AccountId = request.AccountId,
                BankCode = bankCode,
                BankName = bankName,
                AccountNumber = accountNumber,
                AccountName = accountName,
                Currency = wallet.Currency,
                IsVerified = false
            });
        }

        await _uow.SaveChangesAsync(cancellationToken);

        return ServiceResult<WithdrawalResponse>.Success(new WithdrawalResponse
        {
            WithdrawalId = withdrawal.Id,
            Reference = withdrawal.Reference,
            DestinationBankName = withdrawal.DestinationBankName,
            DestinationAccountNumber = withdrawal.DestinationAccountNumber,
            DestinationAccountName = withdrawal.DestinationAccountName,
            Amount = withdrawal.Amount,
            Currency = withdrawal.Currency,
            Status = withdrawal.Status,
            EstimatedDelivery = wallet.Currency == WalletCurrency.NGN ? "Instant" : "1–2 business days",
            CreatedAt = withdrawal.CreatedAt
        }, 201);
    }

    public async Task<ServiceResult<IEnumerable<WithdrawalResponse>>> GetHistoryAsync(
        Guid accountId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var withdrawals = await _uow.Withdrawals.GetByAccountIdAsync(accountId, page, pageSize);
        return ServiceResult<IEnumerable<WithdrawalResponse>>.Success(
            withdrawals.Select(w => new WithdrawalResponse
            {
                WithdrawalId = w.Id,
                Reference = w.Reference,
                DestinationBankName = w.DestinationBankName,
                DestinationAccountNumber = w.DestinationAccountNumber,
                DestinationAccountName = w.DestinationAccountName,
                Amount = w.Amount,
                Currency = w.Currency,
                Status = w.Status,
                EstimatedDelivery = w.Currency == WalletCurrency.NGN ? "Instant" : "1–2 business days",
                CreatedAt = w.CreatedAt
            }));
    }
}
