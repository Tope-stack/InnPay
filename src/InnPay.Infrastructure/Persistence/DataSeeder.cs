using InnPay.Application.Interfaces;
using InnPay.Domain.Entities;
using InnPay.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InnPay.Infrastructure.Persistence;

/// <summary>
/// Seeds deterministic test users for Development and Staging environments.
/// Safe to call on every startup — all checks are idempotent (skip if already exists).
///
/// Seeded credentials
/// ──────────────────────────────────────────────────────────────────────
/// Personal   email: personal@innpay.test   password: Test@1234   pin: 123456
/// Business   email: business@innpay.test   password: Test@1234   pin: 123456
/// Corporate  email: corporate@innpay.test  password: Test@1234   pin: 123456
/// ──────────────────────────────────────────────────────────────────────
/// </summary>
public static class DataSeeder
{
    // ── Seed identities (fixed GUIDs so re-runs are stable) ───────────────────

    // Personal
    private static readonly Guid PersonalUserId    = new("11111111-0001-0001-0001-000000000001");
    private static readonly Guid PersonalAccountId = new("11111111-0001-0001-0001-000000000002");

    // Business
    private static readonly Guid BusinessUserId    = new("22222222-0002-0002-0002-000000000001");
    private static readonly Guid BusinessAccountId = new("22222222-0002-0002-0002-000000000002");

    // Corporate
    private static readonly Guid CorporateUserId    = new("33333333-0003-0003-0003-000000000001");
    private static readonly Guid CorporateAccountId = new("33333333-0003-0003-0003-000000000002");

    private const string SeedPassword       = "Test@1234";
    private const string SeedTransactionPin = "123456";

    public static async Task SeedAsync(
        AppDbContext db,
        IPasswordHasher hasher,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Running data seeder...");

        await SeedPersonalUserAsync(db, hasher, cancellationToken);
        await SeedBusinessUserAsync(db, hasher, cancellationToken);
        await SeedCorporateUserAsync(db, hasher, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Data seeder complete.");
    }

    // ─── PERSONAL ─────────────────────────────────────────────────────────────

    private static async Task SeedPersonalUserAsync(
        AppDbContext db,
        IPasswordHasher hasher,
        CancellationToken ct)
    {
        if (await db.Users.AnyAsync(u => u.Id == PersonalUserId, ct))
            return;

        var user = new User
        {
            Id                  = PersonalUserId,
            FullName            = "Ada Okonkwo",
            Email               = "personal@innpay.test",
            PhoneNumber         = "+2348100000001",
            DateOfBirth         = new DateTime(1995, 4, 12, 0, 0, 0, DateTimeKind.Utc),
            PasswordHash        = hasher.Hash(SeedPassword),
            TransactionPinHash  = hasher.Hash(SeedTransactionPin),
            IsPhoneVerified     = true,
            IsEmailVerified     = true,
            Status              = AccountStatus.Active,
            CreatedAt           = DateTime.UtcNow
        };

        var account = new Account
        {
            Id            = PersonalAccountId,
            UserId        = PersonalUserId,
            AccountType   = AccountType.Personal,
            AccountNumber = "INN1000000001",
            KycTier       = KycTier.Tier2,
            KycStatus     = KycStatus.Approved,
            Status        = AccountStatus.Active,
            CreatedAt     = DateTime.UtcNow
        };

        var ngnWallet = BuildWallet(PersonalAccountId, WalletCurrency.NGN,
            balance: 250_000m, dailyLimit: 5_000_000m);

        var usdWallet = BuildWallet(PersonalAccountId, WalletCurrency.USD,
            balance: 150m, dailyLimit: 5_000m);

        await db.Users.AddAsync(user, ct);
        await db.Accounts.AddAsync(account, ct);
        await db.Wallets.AddRangeAsync(ngnWallet, usdWallet);
    }

    // ─── BUSINESS ─────────────────────────────────────────────────────────────

    private static async Task SeedBusinessUserAsync(
        AppDbContext db,
        IPasswordHasher hasher,
        CancellationToken ct)
    {
        if (await db.Users.AnyAsync(u => u.Id == BusinessUserId, ct))
            return;

        var user = new User
        {
            Id                  = BusinessUserId,
            FullName            = "Emeka Nwosu",
            Email               = "business@innpay.test",
            PhoneNumber         = "+2348100000002",
            DateOfBirth         = new DateTime(1988, 9, 3, 0, 0, 0, DateTimeKind.Utc),
            PasswordHash        = hasher.Hash(SeedPassword),
            TransactionPinHash  = hasher.Hash(SeedTransactionPin),
            IsPhoneVerified     = true,
            IsEmailVerified     = true,
            Status              = AccountStatus.Active,
            CreatedAt           = DateTime.UtcNow
        };

        var account = new Account
        {
            Id             = BusinessAccountId,
            UserId         = BusinessUserId,
            AccountType    = AccountType.Business,
            AccountNumber  = "INN2000000001",
            BusinessName   = "Nwosu Ventures Ltd",
            RcNumber       = "RC-1234567",
            BusinessType   = "Limited Liability Company",
            Industry       = "E-Commerce",
            KycTier        = KycTier.BusinessVerified,
            KycStatus      = KycStatus.Approved,
            Status         = AccountStatus.Active,
            CreatedAt      = DateTime.UtcNow
        };

        var ngnWallet = BuildWallet(BusinessAccountId, WalletCurrency.NGN,
            balance: 1_500_000m, dailyLimit: 10_000_000m);

        var gbpWallet = BuildWallet(BusinessAccountId, WalletCurrency.GBP,
            balance: 500m, dailyLimit: 20_000m);

        await db.Users.AddAsync(user, ct);
        await db.Accounts.AddAsync(account, ct);
        await db.Wallets.AddRangeAsync(ngnWallet, gbpWallet);
    }

    // ─── CORPORATE ────────────────────────────────────────────────────────────

    private static async Task SeedCorporateUserAsync(
        AppDbContext db,
        IPasswordHasher hasher,
        CancellationToken ct)
    {
        if (await db.Users.AnyAsync(u => u.Id == CorporateUserId, ct))
            return;

        var user = new User
        {
            Id                  = CorporateUserId,
            FullName            = "Chidinma Obi",
            Email               = "corporate@innpay.test",
            PhoneNumber         = "+2348100000003",
            DateOfBirth         = new DateTime(1980, 1, 20, 0, 0, 0, DateTimeKind.Utc),
            PasswordHash        = hasher.Hash(SeedPassword),
            TransactionPinHash  = hasher.Hash(SeedTransactionPin),
            IsPhoneVerified     = true,
            IsEmailVerified     = true,
            Status              = AccountStatus.Active,
            CreatedAt           = DateTime.UtcNow
        };

        var account = new Account
        {
            Id                       = CorporateAccountId,
            UserId                   = CorporateUserId,
            AccountType              = AccountType.Corporate,
            AccountNumber            = "INN3000000001",
            CorporateName            = "Obi Global Holdings PLC",
            IncorporationNumber      = "INC-9876543",
            CountryOfIncorporation   = "Nigeria",
            TaxId                    = "TIN-00112233",
            KycTier                  = KycTier.CorporateVerified,
            KycStatus                = KycStatus.Approved,
            Status                   = AccountStatus.Active,
            ComplianceReviewComplete = true,
            CreatedAt                = DateTime.UtcNow
        };

        // Corporate gets Model B standalone accounts
        var ngnWallet = BuildWallet(CorporateAccountId, WalletCurrency.NGN,
            balance: 50_000_000m, dailyLimit: 500_000_000m,
            model: CurrencyAccountModel.StandaloneAccount,
            iban: null, sortCode: null, ukAccountNumber: null,
            walletRef: "INN-NG-3000000001");

        var usdWallet = BuildWallet(CorporateAccountId, WalletCurrency.USD,
            balance: 25_000m, dailyLimit: 500_000m,
            model: CurrencyAccountModel.StandaloneAccount,
            iban: "GB29NWBK60161331926819", sortCode: null, ukAccountNumber: null,
            walletRef: "INN-USD-3000000001", swiftCode: "NNBNGBLA", bankName: "InnPay Partner Bank");

        var eurWallet = BuildWallet(CorporateAccountId, WalletCurrency.EUR,
            balance: 18_000m, dailyLimit: 200_000m,
            model: CurrencyAccountModel.StandaloneAccount,
            iban: "DE89370400440532013000", sortCode: null, ukAccountNumber: null,
            walletRef: "INN-EUR-3000000001", swiftCode: "DEUTDEDB", bankName: "InnPay DE Partner Bank");

        var gbpWallet = BuildWallet(CorporateAccountId, WalletCurrency.GBP,
            balance: 12_000m, dailyLimit: 100_000m,
            model: CurrencyAccountModel.StandaloneAccount,
            iban: "GB82WEST12345698765432", sortCode: "12-34-56", ukAccountNumber: "98765432",
            walletRef: "INN-GBP-3000000001", swiftCode: "BARCGB22", bankName: "InnPay UK Partner Bank");

        await db.Users.AddAsync(user, ct);
        await db.Accounts.AddAsync(account, ct);
        await db.Wallets.AddRangeAsync(ngnWallet, usdWallet, eurWallet, gbpWallet);
    }

    // ─── HELPERS ──────────────────────────────────────────────────────────────

    private static Wallet BuildWallet(
        Guid accountId,
        WalletCurrency currency,
        decimal balance,
        decimal dailyLimit,
        CurrencyAccountModel model = CurrencyAccountModel.WalletModel,
        string? iban = null,
        string? sortCode = null,
        string? ukAccountNumber = null,
        string? walletRef = null,
        string? swiftCode = null,
        string? bankName = null) => new()
    {
        AccountId            = accountId,
        Currency             = currency,
        Model                = model,
        Balance              = balance,
        AvailableBalance     = balance,
        ReservedAmount       = 0m,
        IsActive             = true,
        Status               = WalletStatus.Active,
        DailyTransactionLimit = dailyLimit,
        WalletReference      = walletRef,
        Iban                 = iban,
        SortCode             = sortCode,
        UkAccountNumber      = ukAccountNumber,
        SwiftCode            = swiftCode,
        BankName             = bankName,
        CreatedAt            = DateTime.UtcNow
    };
}
