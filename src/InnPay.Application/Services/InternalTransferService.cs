using InnPay.Application.Common;
using InnPay.Application.DTOs.Request;
using InnPay.Application.DTOs.Response;
using InnPay.Application.Interfaces;
using InnPay.Domain.Entities;
using InnPay.Domain.Enums;
using InnPay.Domain.Interfaces;

namespace InnPay.Application.Services;

public class InternalTransferService : IInternalTransferService
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;

    public InternalTransferService(IUnitOfWork uow, IPasswordHasher hasher)
    {
        _uow = uow;
        _hasher = hasher;
    }

    // ── Recipient Lookup ──────────────────────────────────────────────────────

    public async Task<ServiceResult<RecipientLookupResponse>> LookupRecipientAsync(
        string identifier, CancellationToken cancellationToken = default)
    {
        // Try phone number first
        var user = await _uow.Users.GetByPhoneAsync(identifier)
                   ?? await _uow.Users.GetByEmailAsync(identifier);

        if (user is null)
            return ServiceResult<RecipientLookupResponse>.Fail("No InnPay user found for the given identifier.", 404);

        var account = await _uow.Accounts.GetByUserIdAsync(user.Id);
        if (account is null)
            return ServiceResult<RecipientLookupResponse>.Fail("Account not found.", 404);

        return ServiceResult<RecipientLookupResponse>.Success(new RecipientLookupResponse
        {
            AccountId = account.Id,
            FullName = user.FullName,
            AccountNumber = account.AccountNumber,
            AccountType = account.AccountType
        });
    }

    // ── Initiate Transfer ─────────────────────────────────────────────────────

    public async Task<ServiceResult<InternalTransferResponse>> InitiateAsync(
        InternalTransferRequest request, CancellationToken cancellationToken = default)
    {
        // Validate sender account
        var senderAccount = await _uow.Accounts.GetByIdAsync(request.SenderAccountId);
        if (senderAccount is null)
            return ServiceResult<InternalTransferResponse>.Fail("Sender account not found.", 404);

        if (senderAccount.Status != AccountStatus.Active)
            return ServiceResult<InternalTransferResponse>.Fail("Sender account is not active.");

        // Validate transaction PIN
        if (!_hasher.Verify(request.TransactionPin, senderAccount.User?.TransactionPinHash ?? ""))
            return ServiceResult<InternalTransferResponse>.Fail("Invalid transaction PIN.", 401);

        // Resolve recipient
        var recipientLookup = await LookupRecipientAsync(request.RecipientIdentifier, cancellationToken);
        if (!recipientLookup.IsSuccess)
            return ServiceResult<InternalTransferResponse>.Fail(recipientLookup.Error!, 404);

        var receiverAccount = await _uow.Accounts.GetByIdAsync(recipientLookup.Data!.AccountId);
        if (receiverAccount is null)
            return ServiceResult<InternalTransferResponse>.Fail("Recipient account not found.", 404);

        if (senderAccount.Id == receiverAccount.Id)
            return ServiceResult<InternalTransferResponse>.Fail("Cannot transfer to your own account.");

        // Validate wallets
        var senderWallet = await _uow.Wallets.GetByAccountAndCurrencyAsync(request.SenderAccountId, request.Currency);
        if (senderWallet is null || !senderWallet.IsActive)
            return ServiceResult<InternalTransferResponse>.Fail($"No active {request.Currency} wallet found for sender.");

        var receiverWallet = await _uow.Wallets.GetByAccountAndCurrencyAsync(receiverAccount.Id, request.Currency);
        if (receiverWallet is null || !receiverWallet.IsActive)
            return ServiceResult<InternalTransferResponse>.Fail($"Recipient does not have an active {request.Currency} wallet.");

        // Balance check
        if (senderWallet.AvailableBalance < request.Amount)
            return ServiceResult<InternalTransferResponse>.Fail("Insufficient balance.");

        // Daily limit check
        if (senderWallet.DailyTransactionLimit > 0 && request.Amount > senderWallet.DailyTransactionLimit)
            return ServiceResult<InternalTransferResponse>.Fail($"Amount exceeds daily transaction limit of {request.Currency} {senderWallet.DailyTransactionLimit:N2}.");

        var reference = GenerateReference();
        var transfer = new InternalTransfer
        {
            SenderAccountId = request.SenderAccountId,
            ReceiverAccountId = receiverAccount.Id,
            SenderWalletId = senderWallet.Id,
            ReceiverWalletId = receiverWallet.Id,
            Amount = request.Amount,
            Currency = request.Currency,
            Status = TransferStatus.Pending,
            Reference = reference,
            Note = request.Note,
            InitiatorRole = UserRole.Owner   // extracted from JWT in real flow
        };

        await _uow.InternalTransfers.AddAsync(transfer);

        // ── Atomic debit/credit ───────────────────────────────────────────────
        senderWallet.Balance -= request.Amount;
        senderWallet.AvailableBalance -= request.Amount;
        receiverWallet.Balance += request.Amount;
        receiverWallet.AvailableBalance += request.Amount;

        transfer.Status = TransferStatus.Completed;

        await _uow.Wallets.UpdateAsync(senderWallet);
        await _uow.Wallets.UpdateAsync(receiverWallet);
        await _uow.InternalTransfers.UpdateAsync(transfer);

        // Save beneficiary if requested
        if (request.SaveBeneficiary)
        {
            var alreadySaved = await _uow.SavedBeneficiaries.ExistsAsync(request.SenderAccountId, receiverAccount.Id);
            if (!alreadySaved)
            {
                await _uow.SavedBeneficiaries.AddAsync(new SavedBeneficiary
                {
                    OwnerAccountId = request.SenderAccountId,
                    BeneficiaryAccountId = receiverAccount.Id,
                    Nickname = recipientLookup.Data!.FullName
                });
            }
        }

        await _uow.SaveChangesAsync(cancellationToken);

        return ServiceResult<InternalTransferResponse>.Success(new InternalTransferResponse
        {
            TransferId = transfer.Id,
            Reference = transfer.Reference,
            SenderAccountId = transfer.SenderAccountId,
            ReceiverAccountId = transfer.ReceiverAccountId,
            ReceiverName = recipientLookup.Data!.FullName,
            Amount = transfer.Amount,
            Currency = transfer.Currency,
            Status = transfer.Status,
            Note = transfer.Note,
            CreatedAt = transfer.CreatedAt
        }, 201);
    }

    // ── Schedule Transfer ─────────────────────────────────────────────────────

    public async Task<ServiceResult<TransferScheduleResponse>> ScheduleAsync(
        ScheduleTransferRequest request, CancellationToken cancellationToken = default)
    {
        var senderAccount = await _uow.Accounts.GetByIdAsync(request.SenderAccountId);
        if (senderAccount is null)
            return ServiceResult<TransferScheduleResponse>.Fail("Sender account not found.", 404);

        if (!_hasher.Verify(request.TransactionPin, senderAccount.User?.TransactionPinHash ?? ""))
            return ServiceResult<TransferScheduleResponse>.Fail("Invalid transaction PIN.", 401);

        var receiverAccount = await _uow.Accounts.GetByIdAsync(request.ReceiverAccountId);
        if (receiverAccount is null)
            return ServiceResult<TransferScheduleResponse>.Fail("Receiver account not found.", 404);

        var senderWallet = await _uow.Wallets.GetByAccountAndCurrencyAsync(request.SenderAccountId, request.Currency);
        if (senderWallet is null || !senderWallet.IsActive)
            return ServiceResult<TransferScheduleResponse>.Fail($"No active {request.Currency} wallet found for sender.");

        var receiverWallet = await _uow.Wallets.GetByAccountAndCurrencyAsync(request.ReceiverAccountId, request.Currency);
        if (receiverWallet is null || !receiverWallet.IsActive)
            return ServiceResult<TransferScheduleResponse>.Fail($"Recipient does not have an active {request.Currency} wallet.");

        var schedule = new TransferSchedule
        {
            SenderAccountId = request.SenderAccountId,
            SenderWalletId = senderWallet.Id,
            ReceiverAccountId = request.ReceiverAccountId,
            ReceiverWalletId = receiverWallet.Id,
            Amount = request.Amount,
            Currency = request.Currency,
            Frequency = request.Frequency,
            NextRunAt = request.StartDate,
            IsActive = true,
            Note = request.Note
        };

        await _uow.TransferSchedules.AddAsync(schedule);
        await _uow.SaveChangesAsync(cancellationToken);

        var receiverUser = receiverAccount.User;
        return ServiceResult<TransferScheduleResponse>.Success(new TransferScheduleResponse
        {
            ScheduleId = schedule.Id,
            ReceiverAccountId = schedule.ReceiverAccountId,
            ReceiverName = receiverUser?.FullName ?? string.Empty,
            Amount = schedule.Amount,
            Currency = schedule.Currency,
            Frequency = schedule.Frequency,
            NextRunAt = schedule.NextRunAt,
            IsActive = schedule.IsActive
        }, 201);
    }

    public async Task<ServiceResult> CancelScheduleAsync(
        CancelScheduleRequest request, CancellationToken cancellationToken = default)
    {
        var schedule = await _uow.TransferSchedules.GetByIdAsync(request.ScheduleId);
        if (schedule is null || schedule.SenderAccountId != request.AccountId)
            return ServiceResult.Fail("Schedule not found.", 404);

        schedule.IsActive = false;
        await _uow.TransferSchedules.UpdateAsync(schedule);
        await _uow.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success();
    }

    // ── History ───────────────────────────────────────────────────────────────

    public async Task<ServiceResult<IEnumerable<InternalTransferResponse>>> GetHistoryAsync(
        Guid accountId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var transfers = await _uow.InternalTransfers.GetByAccountIdAsync(accountId, page, pageSize);
        var responses = transfers.Select(t => new InternalTransferResponse
        {
            TransferId = t.Id,
            Reference = t.Reference,
            SenderAccountId = t.SenderAccountId,
            ReceiverAccountId = t.ReceiverAccountId,
            ReceiverName = t.ReceiverAccount?.User?.FullName ?? string.Empty,
            Amount = t.Amount,
            Currency = t.Currency,
            Status = t.Status,
            Note = t.Note,
            CreatedAt = t.CreatedAt
        });

        return ServiceResult<IEnumerable<InternalTransferResponse>>.Success(responses);
    }

    private static string GenerateReference()
        => $"INN-TXF-{Guid.NewGuid().ToString("N")[..12].ToUpper()}";
}
