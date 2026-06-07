using InnPay.Domain.Entities;
using InnPay.Domain.Enums;

namespace InnPay.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByPhoneAsync(string phoneNumber);
    Task<bool> EmailExistsAsync(string email);
    Task<bool> PhoneExistsAsync(string phoneNumber);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
}

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(Guid id);
    Task<Account?> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<Account>> GetAllByUserIdAsync(Guid userId);
    Task AddAsync(Account account);
    Task UpdateAsync(Account account);
}

public interface IKycDocumentRepository
{
    Task<IEnumerable<KycDocument>> GetByAccountIdAsync(Guid accountId);
    Task<KycDocument?> GetByIdAsync(Guid id);
    Task AddAsync(KycDocument document);
    Task UpdateAsync(KycDocument document);
}

public interface IWalletRepository
{
    Task<Wallet?> GetByIdAsync(Guid id);
    Task<Wallet?> GetByAccountAndCurrencyAsync(Guid accountId, WalletCurrency currency);
    Task<IEnumerable<Wallet>> GetByAccountIdAsync(Guid accountId);
    Task AddAsync(Wallet wallet);
    Task UpdateAsync(Wallet wallet);
}

public interface IFxRateRepository
{
    /// <summary>Returns the most recently fetched non-expired rate for this pair.</summary>
    Task<FxRate?> GetLatestAsync(WalletCurrency from, WalletCurrency to);

    /// <summary>Returns all latest rates keyed by pair — used by GET /fx/rates.</summary>
    Task<IEnumerable<FxRate>> GetAllLatestAsync();

    Task AddAsync(FxRate rate);

    /// <summary>Bulk insert a fresh set of rates fetched from the provider.</summary>
    Task BulkAddAsync(IEnumerable<FxRate> rates);
}

public interface IFxRateLockRepository
{
    Task<FxRateLock?> GetByIdAsync(Guid lockId);
    Task<FxRateLock?> GetActiveByAccountAsync(Guid accountId, WalletCurrency from, WalletCurrency to);
    Task AddAsync(FxRateLock rateLock);
    Task UpdateAsync(FxRateLock rateLock);
}

public interface IFxTransactionRepository
{
    Task<FxTransaction?> GetByIdAsync(Guid id);
    Task<FxTransaction?> GetByReferenceAsync(string reference);
    Task<IEnumerable<FxTransaction>> GetByAccountIdAsync(Guid accountId, int page = 1, int pageSize = 20);
    Task AddAsync(FxTransaction transaction);
    Task UpdateAsync(FxTransaction transaction);
}

public interface ICurrencyPairConfigRepository
{
    Task<CurrencyPairConfig?> GetAsync(WalletCurrency from, WalletCurrency to);
    Task<IEnumerable<CurrencyPairConfig>> GetAllActiveAsync();
    Task AddAsync(CurrencyPairConfig config);
    Task UpdateAsync(CurrencyPairConfig config);
}


public interface IOtpRepository
{
    Task<OtpCode?> GetActiveOtpAsync(Guid userId, OtpPurpose purpose);
    Task AddAsync(OtpCode otp);
    Task MarkUsedAsync(Guid otpId);
    Task InvalidateAllForUserAsync(Guid userId, OtpPurpose purpose);
}

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task AddAsync(RefreshToken token);
    Task UpdateAsync(RefreshToken token);
    Task RevokeAllForUserAsync(Guid userId);
}

public interface IUnitOfWork
{
    IUserRepository Users { get; }
    IAccountRepository Accounts { get; }
    IKycDocumentRepository KycDocuments { get; }
    IWalletRepository Wallets { get; }
    IOtpRepository Otps { get; }
    IRefreshTokenRepository RefreshTokens { get; }

    // Currency module
    IFxRateRepository FxRates { get; }
    IFxRateLockRepository FxRateLocks { get; }
    IFxTransactionRepository FxTransactions { get; }
    ICurrencyPairConfigRepository CurrencyPairConfigs { get; }

    // Payment module (6.1)
    IPaymentRepository Payments { get; }

    // Transfer module (6.3 internal, 6.4 external)
    IInternalTransferRepository InternalTransfers { get; }
    ITransferScheduleRepository TransferSchedules { get; }
    ISavedBeneficiaryRepository SavedBeneficiaries { get; }
    IExternalTransferRepository ExternalTransfers { get; }
    ISavedBankAccountRepository SavedBankAccounts { get; }

    // Bills module (6.2)
    IBillPaymentRepository BillPayments { get; }

    // Virtual accounts & withdrawals (6.5, 6.6)
    IVirtualAccountRepository VirtualAccounts { get; }
    IWithdrawalRepository Withdrawals { get; }

    // Virtual card module (6.7)
    IVirtualCardRepository VirtualCards { get; }
    ICardTransactionRepository CardTransactions { get; }

    // Gift card module (6.8)
    IGiftCardPurchaseRepository GiftCardPurchases { get; }

    // Flight module (6.9)
    IFlightBookingRepository FlightBookings { get; }

    // Bet funding module (6.10)
    IBetFundingRepository BetFundings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

// ── Module 6: Repository Interfaces ─────────────────────────────────────────

// 6.1 — Universal Payment Gateway
public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id);
    Task<Payment?> GetByIdempotencyKeyAsync(string idempotencyKey);
    Task<Payment?> GetByReferenceAsync(string reference);
    Task<IEnumerable<Payment>> GetByAccountIdAsync(Guid accountId, int page = 1, int pageSize = 20);
    Task AddAsync(Payment payment);
    Task UpdateAsync(Payment payment);
}

// 6.3 — Internal Transfer
public interface IInternalTransferRepository
{
    Task<InternalTransfer?> GetByIdAsync(Guid id);
    Task<InternalTransfer?> GetByReferenceAsync(string reference);
    Task<IEnumerable<InternalTransfer>> GetBySenderAccountIdAsync(Guid accountId, int page = 1, int pageSize = 20);
    Task<IEnumerable<InternalTransfer>> GetByAccountIdAsync(Guid accountId, int page = 1, int pageSize = 20);
    Task AddAsync(InternalTransfer transfer);
    Task UpdateAsync(InternalTransfer transfer);
}

public interface ITransferScheduleRepository
{
    Task<IEnumerable<TransferSchedule>> GetDueSchedulesAsync(DateTime asOf);
    Task<IEnumerable<TransferSchedule>> GetByAccountIdAsync(Guid accountId);
    Task<TransferSchedule?> GetByIdAsync(Guid id);
    Task AddAsync(TransferSchedule schedule);
    Task UpdateAsync(TransferSchedule schedule);
}

public interface ISavedBeneficiaryRepository
{
    Task<IEnumerable<SavedBeneficiary>> GetByOwnerAccountIdAsync(Guid ownerAccountId);
    Task<SavedBeneficiary?> GetByIdAsync(Guid id);
    Task<bool> ExistsAsync(Guid ownerAccountId, Guid beneficiaryAccountId);
    Task AddAsync(SavedBeneficiary beneficiary);
    Task DeleteAsync(Guid id);
}

// 6.4 — External Transfer
public interface IExternalTransferRepository
{
    Task<ExternalTransfer?> GetByIdAsync(Guid id);
    Task<ExternalTransfer?> GetByReferenceAsync(string reference);
    Task<decimal> GetSevenDayAverageAsync(Guid id);
    Task<IEnumerable<ExternalTransfer>> GetByAccountIdAsync(Guid accountId, int page = 1, int pageSize = 20);
    Task AddAsync(ExternalTransfer transfer);
    Task UpdateAsync(ExternalTransfer transfer);
}

public interface ISavedBankAccountRepository
{
    Task<IEnumerable<SavedBankAccount>> GetByAccountIdAsync(Guid accountId);
    Task<SavedBankAccount?> GetByIdAsync(Guid id);
    Task AddAsync(SavedBankAccount bankAccount);
    Task DeleteAsync(Guid id);
}

// 6.2 — Bills
public interface IBillPaymentRepository
{
    Task<BillPayment?> GetByIdAsync(Guid id);
    Task<BillPayment?> GetByReferenceAsync(string reference);
    Task<IEnumerable<BillPayment>> GetByAccountIdAsync(Guid accountId, int page = 1, int pageSize = 20);
    Task AddAsync(BillPayment billPayment);
    Task UpdateAsync(BillPayment billPayment);
}

// 6.5 — Virtual Accounts
public interface IVirtualAccountRepository
{
    Task<VirtualAccount?> GetByIdAsync(Guid id);
    Task<VirtualAccount?> GetByAccountNumberAsync(string accountNumber);
    Task<VirtualAccount?> GetPrimaryByAccountIdAsync(Guid accountId);
    Task<IEnumerable<VirtualAccount>> GetByAccountIdAsync(Guid accountId);
    Task AddAsync(VirtualAccount virtualAccount);
    Task UpdateAsync(VirtualAccount virtualAccount);
}

// 6.6 — Withdrawals
public interface IWithdrawalRepository
{
    Task<Withdrawal?> GetByIdAsync(Guid id);
    Task<Withdrawal?> GetByReferenceAsync(string reference);
    Task<IEnumerable<Withdrawal>> GetByAccountIdAsync(Guid accountId, int page = 1, int pageSize = 20);
    Task<decimal> GetSevenDayAverageAsync(Guid walletId);
    Task AddAsync(Withdrawal withdrawal);
    Task UpdateAsync(Withdrawal withdrawal);
}

// 6.7 — Virtual Card
public interface IVirtualCardRepository
{
    Task<VirtualCard?> GetByIdAsync(Guid id);
    Task<VirtualCard?> GetByProviderCardIdAsync(string providerCardId);
    Task<IEnumerable<VirtualCard>> GetByAccountIdAsync(Guid accountId);
    Task AddAsync(VirtualCard card);
    Task UpdateAsync(VirtualCard card);
}

public interface ICardTransactionRepository
{
    Task<IEnumerable<CardTransaction>> GetByCardIdAsync(Guid cardId, int page = 1, int pageSize = 20);
    Task AddAsync(CardTransaction transaction);
}

// 6.8 — Gift Cards
public interface IGiftCardPurchaseRepository
{
    Task<GiftCardPurchase?> GetByIdAsync(Guid id);
    Task<GiftCardPurchase?> GetByReferenceAsync(string reference);
    Task<IEnumerable<GiftCardPurchase>> GetByAccountIdAsync(Guid accountId, int page = 1, int pageSize = 20);
    Task AddAsync(GiftCardPurchase purchase);
    Task UpdateAsync(GiftCardPurchase purchase);
}

// 6.9 — Flights
public interface IFlightBookingRepository
{
    Task<FlightBooking?> GetByIdAsync(Guid id);
    Task<FlightBooking?> GetByReferenceAsync(string reference);
    Task<IEnumerable<FlightBooking>> GetByAccountIdAsync(Guid accountId, int page = 1, int pageSize = 20);
    Task AddAsync(FlightBooking booking);
    Task UpdateAsync(FlightBooking booking);
}

// 6.10 — Bet Funding
public interface IBetFundingRepository
{
    Task<BetFunding?> GetByIdAsync(Guid id);
    Task<BetFunding?> GetByReferenceAsync(string reference);
    Task<IEnumerable<BetFunding>> GetByAccountIdAsync(Guid accountId, int page = 1, int pageSize = 20);
    Task AddAsync(BetFunding funding);
    Task UpdateAsync(BetFunding funding);
}
