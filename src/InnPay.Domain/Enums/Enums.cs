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

