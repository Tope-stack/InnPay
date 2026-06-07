using InnPay.Application.Common;
using InnPay.Application.DTOs.Request;
using InnPay.Application.DTOs.Response;
using InnPay.Application.Interfaces;
using InnPay.Domain.Entities;
using InnPay.Domain.Enums;
using InnPay.Domain.Interfaces;

namespace InnPay.Application.Services;

public class ExternalTransferService : IExternalTransferService
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;

    // TODO: inject real NIBSS / SWIFT provider when bank partner is onboarded
    // For now the transfer is recorded and marked Processing — provider stub is in Infrastructure

    public ExternalTransferService(IUnitOfWork uow, IPasswordHasher hasher)
    {
        _uow = uow;
        _hasher = hasher;
    }

    public Task<ServiceResult<IEnumerable<BankResponse>>> GetBankListAsync(
        WalletCurrency currency, CancellationToken cancellationToken = default)
    {
        // Stub — returns a representative list of Nigerian banks (NGN) or placeholder for FX
        var banks = currency == WalletCurrency.NGN
            ? NigerianBanks()
            : InternationalBanks(currency);

        return Task.FromResult(ServiceResult<IEnumerable<BankResponse>>.Success(banks));
    }

    public async Task<ServiceResult<BankAccountValidationResponse>> ValidateBankAccountAsync(
        ValidateBankAccountRequest request, CancellationToken cancellationToken = default)
    {
        // TODO: call NIP account-name-enquiry (NIBSS) or SWIFT BIC lookup in production
        // Returning a mock validation to demonstrate the flow
        await Task.Delay(100, cancellationToken); // simulate network call

        return ServiceResult<BankAccountValidationResponse>.Success(new BankAccountValidationResponse
        {
            AccountNumber = request.AccountNumber,
            AccountName = "Account Holder Name",   // replaced by real NIP response in prod
            BankName = ResolveBankName(request.BankCode)
        });
    }

    public async Task<ServiceResult<ExternalTransferResponse>> InitiateAsync(
        ExternalTransferRequest request, CancellationToken cancellationToken = default)
    {
        var senderAccount = await _uow.Accounts.GetByIdAsync(request.SenderAccountId);
        if (senderAccount is null)
            return ServiceResult<ExternalTransferResponse>.Fail("Account not found.", 404);

        if (!_hasher.Verify(request.TransactionPin, senderAccount.User?.TransactionPinHash ?? ""))
            return ServiceResult<ExternalTransferResponse>.Fail("Invalid transaction PIN.", 401);

        var wallet = await _uow.Wallets.GetByIdAsync(request.SenderWalletId);
        if (wallet is null || wallet.AccountId != request.SenderAccountId)
            return ServiceResult<ExternalTransferResponse>.Fail("Wallet not found.", 404);

        if (!wallet.IsActive || wallet.Status != WalletStatus.Active)
            return ServiceResult<ExternalTransferResponse>.Fail("Wallet is not active.");

        if (wallet.AvailableBalance < request.Amount)
            return ServiceResult<ExternalTransferResponse>.Fail("Insufficient balance.");

        // ── Fraud scoring (rule-based v1) ─────────────────────────────────────
        var sevenDayAvg = await _uow.ExternalTransfers.GetSevenDayAverageAsync(wallet.Id);
        // Reuse withdrawal repository's method — same logic
        var fraudScore = ComputeFraudScore(request.Amount, sevenDayAvg);

        // Validate destination bank (name enquiry)
        var validation = await ValidateBankAccountAsync(
            new ValidateBankAccountRequest { BankCode = request.BankCode, AccountNumber = request.AccountNumber, Currency = request.Currency },
            cancellationToken);

        if (!validation.IsSuccess)
            return ServiceResult<ExternalTransferResponse>.Fail("Could not validate destination account.");

        var reference = $"INN-EXT-{Guid.NewGuid().ToString("N")[..12].ToUpper()}";

        var transfer = new ExternalTransfer
        {
            SenderAccountId = request.SenderAccountId,
            SenderWalletId = request.SenderWalletId,
            DestinationBankCode = request.BankCode,
            DestinationBankName = ResolveBankName(request.BankCode),
            DestinationAccountNumber = request.AccountNumber,
            DestinationAccountName = validation.Data!.AccountName,
            Amount = request.Amount,
            Currency = request.Currency,
            Status = ExternalTransferStatus.Processing,
            Reference = reference,
            NibssSessionId = request.Currency == WalletCurrency.NGN ? GenerateNibssSessionId() : null,
            SwiftReference = request.Currency != WalletCurrency.NGN ? GenerateSwiftReference() : null,
            FraudScore = fraudScore,
            IsFlagged = fraudScore > 70
        };

        // Debit sender wallet immediately; credit happens when provider confirms
        wallet.AvailableBalance -= request.Amount;
        wallet.ReservedAmount += request.Amount;

        await _uow.ExternalTransfers.AddAsync(transfer);
        await _uow.Wallets.UpdateAsync(wallet);

        // Save bank account if requested
        if (request.SaveBankAccount)
        {
            await _uow.SavedBankAccounts.AddAsync(new SavedBankAccount
            {
                AccountId = request.SenderAccountId,
                BankCode = request.BankCode,
                BankName = ResolveBankName(request.BankCode),
                AccountNumber = request.AccountNumber,
                AccountName = validation.Data.AccountName,
                Currency = request.Currency,
                IsVerified = true
            });
        }

        await _uow.SaveChangesAsync(cancellationToken);

        return ServiceResult<ExternalTransferResponse>.Success(new ExternalTransferResponse
        {
            TransferId = transfer.Id,
            Reference = transfer.Reference,
            DestinationBankName = transfer.DestinationBankName,
            DestinationAccountNumber = transfer.DestinationAccountNumber,
            DestinationAccountName = transfer.DestinationAccountName,
            Amount = transfer.Amount,
            Currency = transfer.Currency,
            Status = transfer.Status,
            NibssSessionId = transfer.NibssSessionId,
            SwiftReference = transfer.SwiftReference,
            CreatedAt = transfer.CreatedAt
        }, 201);
    }

    public async Task<ServiceResult<IEnumerable<ExternalTransferResponse>>> GetHistoryAsync(
        Guid accountId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var transfers = await _uow.ExternalTransfers.GetByAccountIdAsync(accountId, page, pageSize);
        return ServiceResult<IEnumerable<ExternalTransferResponse>>.Success(
            transfers.Select(MapToResponse));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static int ComputeFraudScore(decimal amount, decimal sevenDayAvg)
    {
        if (sevenDayAvg <= 0) return 0;
        var ratio = amount / sevenDayAvg;
        return ratio >= 0.8m ? (int)Math.Min(100, ratio * 60) : 0;
    }

    private static string GenerateNibssSessionId()
        => $"NIBSS{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";

    private static string GenerateSwiftReference()
        => $"SWIFT{Guid.NewGuid().ToString("N")[..10].ToUpper()}";

    private static string ResolveBankName(string bankCode) => bankCode switch
    {
        "044" => "Access Bank", "023" => "Citibank", "063" => "Diamond Bank",
        "050" => "EcoBank", "070" => "Fidelity Bank", "011" => "First Bank",
        "214" => "First City Monument Bank", "058" => "Guaranty Trust Bank",
        "030" => "Heritage Bank", "301" => "Jaiz Bank", "082" => "Keystone Bank",
        "526" => "Parallex Bank", "076" => "Polaris Bank", "101" => "ProvidusBank",
        "221" => "Stanbic IBTC Bank", "068" => "Standard Chartered Bank",
        "232" => "Sterling Bank", "100" => "Suntrust Bank", "032" => "Union Bank",
        "033" => "United Bank for Africa", "215" => "Unity Bank",
        "035" => "Wema Bank", "057" => "Zenith Bank",
        _ => "Unknown Bank"
    };

    private static IEnumerable<BankResponse> NigerianBanks() =>
    [
        new() { BankCode = "044", BankName = "Access Bank", SupportsInstant = true, Currency = WalletCurrency.NGN },
        new() { BankCode = "011", BankName = "First Bank", SupportsInstant = true, Currency = WalletCurrency.NGN },
        new() { BankCode = "058", BankName = "Guaranty Trust Bank", SupportsInstant = true, Currency = WalletCurrency.NGN },
        new() { BankCode = "033", BankName = "United Bank for Africa", SupportsInstant = true, Currency = WalletCurrency.NGN },
        new() { BankCode = "057", BankName = "Zenith Bank", SupportsInstant = true, Currency = WalletCurrency.NGN },
        new() { BankCode = "070", BankName = "Fidelity Bank", SupportsInstant = true, Currency = WalletCurrency.NGN },
        new() { BankCode = "214", BankName = "First City Monument Bank", SupportsInstant = true, Currency = WalletCurrency.NGN },
        new() { BankCode = "050", BankName = "EcoBank", SupportsInstant = true, Currency = WalletCurrency.NGN },
        new() { BankCode = "232", BankName = "Sterling Bank", SupportsInstant = true, Currency = WalletCurrency.NGN },
        new() { BankCode = "035", BankName = "Wema Bank", SupportsInstant = true, Currency = WalletCurrency.NGN }
    ];

    private static IEnumerable<BankResponse> InternationalBanks(WalletCurrency currency) =>
    [
        new() { BankCode = "BNKUS33", BankName = "JP Morgan Chase (USD)", SupportsInstant = false, Currency = currency },
        new() { BankCode = "DEUTDEDB", BankName = "Deutsche Bank (EUR)", SupportsInstant = false, Currency = currency },
        new() { BankCode = "BARCGB22", BankName = "Barclays (GBP)", SupportsInstant = false, Currency = currency }
    ];

    private static ExternalTransferResponse MapToResponse(ExternalTransfer t) => new()
    {
        TransferId = t.Id,
        Reference = t.Reference,
        DestinationBankName = t.DestinationBankName,
        DestinationAccountNumber = t.DestinationAccountNumber,
        DestinationAccountName = t.DestinationAccountName,
        Amount = t.Amount,
        Currency = t.Currency,
        Status = t.Status,
        NibssSessionId = t.NibssSessionId,
        SwiftReference = t.SwiftReference,
        CreatedAt = t.CreatedAt
    };
}
