using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InnPay.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addPaymentsEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BetFundings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    WalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    Platform = table.Column<int>(type: "integer", nullable: false),
                    BettingUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Reference = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProviderReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsFlagged = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BetFundings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BetFundings_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BetFundings_Wallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "Wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BillPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    WalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    BillerCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BillerName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CustomerReference = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    BillerReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ElectricityToken = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Reference = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillPayments_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BillPayments_Wallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "Wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FlightBookings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    WalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingReference = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Airline = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Origin = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Destination = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    DepartureAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReturnAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PassengerCount = table.Column<int>(type: "integer", nullable: false),
                    CabinClass = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TripType = table.Column<int>(type: "integer", nullable: false),
                    AmountCharged = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ETicketUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AmadeusOrderId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlightBookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlightBookings_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FlightBookings_Wallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "Wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GiftCardPurchases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    WalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    BrandName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CountryCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    DenominationUsd = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    AmountCharged = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    WalletCurrency = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RedemptionCodeEncrypted = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    RedemptionPin = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Reference = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReloadlyOrderId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiftCardPurchases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GiftCardPurchases_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GiftCardPurchases_Wallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "Wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InternalTransfers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiverAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderWalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiverWalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Reference = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Note = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    InitiatorRole = table.Column<int>(type: "integer", nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InternalTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InternalTransfers_Accounts_ReceiverAccountId",
                        column: x => x.ReceiverAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InternalTransfers_Accounts_SenderAccountId",
                        column: x => x.SenderAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InternalTransfers_Wallets_ReceiverWalletId",
                        column: x => x.ReceiverWalletId,
                        principalTable: "Wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InternalTransfers_Wallets_SenderWalletId",
                        column: x => x.SenderWalletId,
                        principalTable: "Wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    WalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<int>(type: "integer", nullable: false),
                    Provider = table.Column<int>(type: "integer", nullable: false),
                    Method = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Reference = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProviderReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ClientSecret = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_Wallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "Wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SavedBankAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    BankCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BankName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AccountNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AccountName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Currency = table.Column<int>(type: "integer", nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedBankAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedBankAccounts_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SavedBeneficiaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    BeneficiaryAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nickname = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedBeneficiaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedBeneficiaries_Accounts_BeneficiaryAccountId",
                        column: x => x.BeneficiaryAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SavedBeneficiaries_Accounts_OwnerAccountId",
                        column: x => x.OwnerAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TransferSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderWalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiverAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiverWalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<int>(type: "integer", nullable: false),
                    Frequency = table.Column<int>(type: "integer", nullable: false),
                    NextRunAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Note = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransferSchedules_Accounts_SenderAccountId",
                        column: x => x.SenderAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransferSchedules_Wallets_SenderWalletId",
                        column: x => x.SenderWalletId,
                        principalTable: "Wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VirtualAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    WalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BankName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BankCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AccountName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VirtualAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VirtualAccounts_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VirtualAccounts_Wallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "Wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VirtualCards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    WalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    PanEncrypted = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Last4 = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    Expiry = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    CvvEncrypted = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ProviderCardId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SpendingLimitPerTransaction = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    SpendingLimitMonthly = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    TerminationReason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VirtualCards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VirtualCards_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VirtualCards_Wallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "Wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FlightPassengers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FlightBookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PassportNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Nationality = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlightPassengers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlightPassengers_FlightBookings_FlightBookingId",
                        column: x => x.FlightBookingId,
                        principalTable: "FlightBookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExternalTransfers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderWalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    SavedBankAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DestinationBankCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DestinationBankName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DestinationAccountNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DestinationAccountName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Reference = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NibssSessionId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SwiftReference = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    FraudScore = table.Column<int>(type: "integer", nullable: false),
                    IsFlagged = table.Column<bool>(type: "boolean", nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalTransfers_Accounts_SenderAccountId",
                        column: x => x.SenderAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExternalTransfers_SavedBankAccounts_SavedBankAccountId",
                        column: x => x.SavedBankAccountId,
                        principalTable: "SavedBankAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ExternalTransfers_Wallets_SenderWalletId",
                        column: x => x.SenderWalletId,
                        principalTable: "Wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Withdrawals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    WalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    SavedBankAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DestinationBankCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DestinationBankName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DestinationAccountNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DestinationAccountName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Reference = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProviderReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsFlagged = table.Column<bool>(type: "boolean", nullable: false),
                    FraudScore = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Withdrawals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Withdrawals_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Withdrawals_SavedBankAccounts_SavedBankAccountId",
                        column: x => x.SavedBankAccountId,
                        principalTable: "SavedBankAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Withdrawals_Wallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "Wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CardTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VirtualCardId = table.Column<Guid>(type: "uuid", nullable: false),
                    MerchantName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MerchantCategory = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<int>(type: "integer", nullable: false),
                    ProviderTransactionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsDeclined = table.Column<bool>(type: "boolean", nullable: false),
                    DeclineReason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    TransactedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CardTransactions_VirtualCards_VirtualCardId",
                        column: x => x.VirtualCardId,
                        principalTable: "VirtualCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BetFundings_AccountId",
                table: "BetFundings",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BetFundings_Reference",
                table: "BetFundings",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BetFundings_WalletId",
                table: "BetFundings",
                column: "WalletId");

            migrationBuilder.CreateIndex(
                name: "IX_BillPayments_AccountId",
                table: "BillPayments",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BillPayments_Reference",
                table: "BillPayments",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillPayments_WalletId",
                table: "BillPayments",
                column: "WalletId");

            migrationBuilder.CreateIndex(
                name: "IX_CardTransactions_TransactedAt",
                table: "CardTransactions",
                column: "TransactedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CardTransactions_VirtualCardId",
                table: "CardTransactions",
                column: "VirtualCardId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalTransfers_Reference",
                table: "ExternalTransfers",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExternalTransfers_SavedBankAccountId",
                table: "ExternalTransfers",
                column: "SavedBankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalTransfers_SenderAccountId",
                table: "ExternalTransfers",
                column: "SenderAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalTransfers_SenderWalletId",
                table: "ExternalTransfers",
                column: "SenderWalletId");

            migrationBuilder.CreateIndex(
                name: "IX_FlightBookings_AccountId",
                table: "FlightBookings",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_FlightBookings_BookingReference",
                table: "FlightBookings",
                column: "BookingReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FlightBookings_WalletId",
                table: "FlightBookings",
                column: "WalletId");

            migrationBuilder.CreateIndex(
                name: "IX_FlightPassengers_FlightBookingId",
                table: "FlightPassengers",
                column: "FlightBookingId");

            migrationBuilder.CreateIndex(
                name: "IX_GiftCardPurchases_AccountId",
                table: "GiftCardPurchases",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_GiftCardPurchases_Reference",
                table: "GiftCardPurchases",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GiftCardPurchases_WalletId",
                table: "GiftCardPurchases",
                column: "WalletId");

            migrationBuilder.CreateIndex(
                name: "IX_InternalTransfers_ReceiverAccountId",
                table: "InternalTransfers",
                column: "ReceiverAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_InternalTransfers_ReceiverWalletId",
                table: "InternalTransfers",
                column: "ReceiverWalletId");

            migrationBuilder.CreateIndex(
                name: "IX_InternalTransfers_Reference",
                table: "InternalTransfers",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InternalTransfers_SenderAccountId",
                table: "InternalTransfers",
                column: "SenderAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_InternalTransfers_SenderWalletId",
                table: "InternalTransfers",
                column: "SenderWalletId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_AccountId",
                table: "Payments",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_IdempotencyKey",
                table: "Payments",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Reference",
                table: "Payments",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_WalletId",
                table: "Payments",
                column: "WalletId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedBankAccounts_AccountId",
                table: "SavedBankAccounts",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedBeneficiaries_BeneficiaryAccountId",
                table: "SavedBeneficiaries",
                column: "BeneficiaryAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedBeneficiaries_OwnerAccountId_BeneficiaryAccountId",
                table: "SavedBeneficiaries",
                columns: new[] { "OwnerAccountId", "BeneficiaryAccountId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransferSchedules_IsActive_NextRunAt",
                table: "TransferSchedules",
                columns: new[] { "IsActive", "NextRunAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TransferSchedules_SenderAccountId",
                table: "TransferSchedules",
                column: "SenderAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferSchedules_SenderWalletId",
                table: "TransferSchedules",
                column: "SenderWalletId");

            migrationBuilder.CreateIndex(
                name: "IX_VirtualAccounts_AccountId",
                table: "VirtualAccounts",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_VirtualAccounts_AccountNumber",
                table: "VirtualAccounts",
                column: "AccountNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VirtualAccounts_WalletId",
                table: "VirtualAccounts",
                column: "WalletId");

            migrationBuilder.CreateIndex(
                name: "IX_VirtualCards_AccountId",
                table: "VirtualCards",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_VirtualCards_ProviderCardId",
                table: "VirtualCards",
                column: "ProviderCardId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VirtualCards_WalletId",
                table: "VirtualCards",
                column: "WalletId");

            migrationBuilder.CreateIndex(
                name: "IX_Withdrawals_AccountId",
                table: "Withdrawals",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Withdrawals_Reference",
                table: "Withdrawals",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Withdrawals_SavedBankAccountId",
                table: "Withdrawals",
                column: "SavedBankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Withdrawals_WalletId",
                table: "Withdrawals",
                column: "WalletId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BetFundings");

            migrationBuilder.DropTable(
                name: "BillPayments");

            migrationBuilder.DropTable(
                name: "CardTransactions");

            migrationBuilder.DropTable(
                name: "ExternalTransfers");

            migrationBuilder.DropTable(
                name: "FlightPassengers");

            migrationBuilder.DropTable(
                name: "GiftCardPurchases");

            migrationBuilder.DropTable(
                name: "InternalTransfers");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "SavedBeneficiaries");

            migrationBuilder.DropTable(
                name: "TransferSchedules");

            migrationBuilder.DropTable(
                name: "VirtualAccounts");

            migrationBuilder.DropTable(
                name: "Withdrawals");

            migrationBuilder.DropTable(
                name: "VirtualCards");

            migrationBuilder.DropTable(
                name: "FlightBookings");

            migrationBuilder.DropTable(
                name: "SavedBankAccounts");
        }
    }
}
