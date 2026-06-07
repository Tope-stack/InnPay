namespace InnPay.Domain.Enums;

public enum AccountType
{
    Personal = 1,
    Business = 2,
    Corporate = 3
}

public enum KycTier
{
    None = 0,
    Tier1 = 1,
    Tier2 = 2,
    BusinessVerified = 3,
    CorporateVerified = 4
}

public enum KycStatus
{
    NotSubmitted = 0,
    Pending = 1,
    UnderReview = 2,
    Approved = 3,
    Rejected = 4
}

public enum WalletCurrency
{
    NGN = 1,
    USD = 2,
    EUR = 3,
    GBP = 4
}

public enum UserRole
{
    // Personal
    Owner = 1,

    // Business
    BusinessAdmin = 10,
    BusinessOperator = 11,
    BusinessViewer = 12,

    // Corporate
    SuperAdmin = 20,
    FinanceAdmin = 21,
    CorporateOperator = 22,
    Auditor = 23,
    CorporateViewer = 24
}

public enum DocumentType
{
    // Personal KYC
    NationalId = 1,
    InternationalPassport = 2,
    DriversLicense = 3,
    Selfie = 4,
    ProofOfAddress = 5,

    // Business KYC
    CacCertificate = 10,
    BusinessUtilityBill = 11,
    DirectorId = 12,

    // Corporate KYC
    CertificateOfIncorporation = 20,
    MemorandumAndArticles = 21,
    BoardResolution = 22,
    BeneficialOwnershipDeclaration = 23,
    UboId = 24
}

public enum OtpPurpose
{
    PhoneVerification = 1,
    TransactionPin = 2,
    PasswordReset = 3
}

public enum AccountStatus
{
    Pending = 0,
    Active = 1,
    Suspended = 2,
    Closed = 3
}

public enum WalletStatus
{
    Active = 1,
    Frozen = 2,
    Closed = 3
}

/// <summary>
/// Model A = sub-wallet inside an account (wallet reference only).
/// Model B = standalone dedicated currency account with its own IBAN / sort code.
/// </summary>
public enum CurrencyAccountModel
{
    WalletModel = 1,       // Model A — Personal / Business default
    StandaloneAccount = 2  // Model B — Corporate / upgraded Business
}

// ── Module 6: Payment Modules ────────────────────────────────────────────────

public enum PaymentProvider
{
    Paystack = 1,       // NGN card & bank transfer
    Flutterwave = 2,    // Multi-currency, Africa-wide
    Stripe = 3          // International USD/EUR/GBP
}

public enum PaymentMethod
{
    BankTransfer = 1,
    Card = 2,
    WalletBalance = 3,
    USSD = 4
}

public enum PaymentStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Reversed = 4
}

public enum TransferStatus
{
    Pending = 0,
    Completed = 1,
    Failed = 2,
    Reversed = 3
}

public enum TransferFrequency
{
    Daily = 1,
    Weekly = 2,
    Monthly = 3
}

public enum BillCategory
{
    Electricity = 1,
    Airtime = 2,
    Data = 3,
    CableTV = 4,
    Water = 5,
    Internet = 6
}

public enum VirtualCardStatus
{
    Active = 1,
    Frozen = 2,
    Terminated = 3
}

public enum TripType
{
    OneWay = 1,
    RoundTrip = 2,
    MultiCity = 3
}

public enum BettingPlatform
{
    Bet9ja = 1,
    Sportybet = 2,
    OneXBet = 3
}

public enum ExternalTransferStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3
}

public enum WithdrawalStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3
}

public enum GiftCardStatus
{
    Purchased = 1,
    Revealed = 2,
    Sent = 3
}

public enum BookingStatus
{
    Pending = 0,
    Confirmed = 1,
    Failed = 2,
    Cancelled = 3
}

public enum FxConversionStatus
{
    Pending = 0,
    Completed = 1,
    Failed = 2,
    Expired = 3    // rate lock window elapsed before confirmation
}

public enum FxRateSource
{
    OpenExchangeRates = 1,
    FixerIo = 2,
    Manual = 3     // admin-set override rate
}

