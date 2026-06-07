using InnPay.Domain.Entities;
using InnPay.Domain.Interfaces;
using InnPay.Infrastructure.Persistence;
using InnPay.Infrastructure.Repositories;

namespace InnPay.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;

    public UnitOfWork(AppDbContext db)
    {
        _db = db;
        Users = new UserRepository(db);
        Accounts = new AccountRepository(db);
        KycDocuments = new KycDocumentRepository(db);
        Wallets = new WalletRepository(db);
        Otps = new OtpRepository(db);
        RefreshTokens = new RefreshTokenRepository(db);
        // Currency module
        FxRates = new FxRateRepository(db);
        FxRateLocks = new FxRateLockRepository(db);
        FxTransactions = new FxTransactionRepository(db);
        CurrencyPairConfigs = new CurrencyPairConfigRepository(db);
        // Payment module (6.1)
        Payments = new PaymentRepository(db);
        // Transfer module (6.3 / 6.4)
        InternalTransfers = new InternalTransferRepository(db);
        TransferSchedules = new TransferScheduleRepository(db);
        SavedBeneficiaries = new SavedBeneficiaryRepository(db);
        ExternalTransfers = new ExternalTransferRepository(db);
        SavedBankAccounts = new SavedBankAccountRepository(db);
        // Bills (6.2)
        BillPayments = new BillPaymentRepository(db);
        // Virtual accounts & withdrawals (6.5, 6.6)
        VirtualAccounts = new VirtualAccountRepository(db);
        Withdrawals = new WithdrawalRepository(db);
        // Cards (6.7)
        VirtualCards = new VirtualCardRepository(db);
        CardTransactions = new CardTransactionRepository(db);
        // Gift cards (6.8)
        GiftCardPurchases = new GiftCardPurchaseRepository(db);
        // Flights (6.9)
        FlightBookings = new FlightBookingRepository(db);
        // Betting (6.10)
        BetFundings = new BetFundingRepository(db);
    }

    public IUserRepository Users { get; }
    public IAccountRepository Accounts { get; }
    public IKycDocumentRepository KycDocuments { get; }
    public IWalletRepository Wallets { get; }
    public IOtpRepository Otps { get; }
    public IRefreshTokenRepository RefreshTokens { get; }

    // Currency module
    public IFxRateRepository FxRates { get; }
    public IFxRateLockRepository FxRateLocks { get; }
    public IFxTransactionRepository FxTransactions { get; }
    public ICurrencyPairConfigRepository CurrencyPairConfigs { get; }

    // Payment module (6.1)
    public IPaymentRepository Payments { get; }

    // Transfer module (6.3 / 6.4)
    public IInternalTransferRepository InternalTransfers { get; }
    public ITransferScheduleRepository TransferSchedules { get; }
    public ISavedBeneficiaryRepository SavedBeneficiaries { get; }
    public IExternalTransferRepository ExternalTransfers { get; }
    public ISavedBankAccountRepository SavedBankAccounts { get; }

    // Bills (6.2)
    public IBillPaymentRepository BillPayments { get; }

    // Virtual accounts & withdrawals (6.5, 6.6)
    public IVirtualAccountRepository VirtualAccounts { get; }
    public IWithdrawalRepository Withdrawals { get; }

    // Cards (6.7)
    public IVirtualCardRepository VirtualCards { get; }
    public ICardTransactionRepository CardTransactions { get; }

    // Gift cards (6.8)
    public IGiftCardPurchaseRepository GiftCardPurchases { get; }

    // Flights (6.9)
    public IFlightBookingRepository FlightBookings { get; }

    // Betting (6.10)
    public IBetFundingRepository BetFundings { get; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _db.SaveChangesAsync(cancellationToken);
}
