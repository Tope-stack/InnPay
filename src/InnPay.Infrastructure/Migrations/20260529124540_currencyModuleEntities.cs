using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InnPay.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class currencyModuleEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AvailableBalance",
                table: "Wallets",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "Wallets",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Iban",
                table: "Wallets",
                type: "character varying(34)",
                maxLength: 34,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Model",
                table: "Wallets",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "ReservedAmount",
                table: "Wallets",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "SortCode",
                table: "Wallets",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Wallets",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SwiftCode",
                table: "Wallets",
                type: "character varying(11)",
                maxLength: 11,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UkAccountNumber",
                table: "Wallets",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WalletReference",
                table: "Wallets",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CurrencyPairConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FromCurrency = table.Column<string>(type: "text", nullable: false),
                    ToCurrency = table.Column<string>(type: "text", nullable: false),
                    SpreadBps = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    FlatFee = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    PercentageFee = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    MinSourceAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    MaxSourceAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrencyPairConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FxRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FromCurrency = table.Column<string>(type: "text", nullable: false),
                    ToCurrency = table.Column<string>(type: "text", nullable: false),
                    MidRate = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    CustomerRate = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    SpreadBps = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    FetchedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FxRates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FxRateLocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    FxRateId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromCurrency = table.Column<string>(type: "text", nullable: false),
                    ToCurrency = table.Column<string>(type: "text", nullable: false),
                    SourceAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    LockedCustomerRate = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    EstimatedConvertedAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    EstimatedFeeAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FxRateLocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FxRateLocks_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FxRateLocks_FxRates_FxRateId",
                        column: x => x.FxRateId,
                        principalTable: "FxRates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FxTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    RateLockId = table.Column<Guid>(type: "uuid", nullable: true),
                    FromWalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToWalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromCurrency = table.Column<string>(type: "text", nullable: false),
                    ToCurrency = table.Column<string>(type: "text", nullable: false),
                    SourceAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    AppliedRate = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    GrossConvertedAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    FeeAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    FeeCurrency = table.Column<string>(type: "text", nullable: false),
                    NetConvertedAmount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Reference = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ConvertedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FxTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FxTransactions_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FxTransactions_FxRateLocks_RateLockId",
                        column: x => x.RateLockId,
                        principalTable: "FxRateLocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FxTransactions_Wallets_FromWalletId",
                        column: x => x.FromWalletId,
                        principalTable: "Wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FxTransactions_Wallets_ToWalletId",
                        column: x => x.ToWalletId,
                        principalTable: "Wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_Iban",
                table: "Wallets",
                column: "Iban",
                unique: true,
                filter: "\"Iban\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_WalletReference",
                table: "Wallets",
                column: "WalletReference",
                unique: true,
                filter: "\"WalletReference\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyPairConfigs_FromCurrency_ToCurrency",
                table: "CurrencyPairConfigs",
                columns: new[] { "FromCurrency", "ToCurrency" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FxRateLocks_AccountId",
                table: "FxRateLocks",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_FxRateLocks_FxRateId",
                table: "FxRateLocks",
                column: "FxRateId");

            migrationBuilder.CreateIndex(
                name: "IX_FxRates_FromCurrency_ToCurrency_FetchedAt",
                table: "FxRates",
                columns: new[] { "FromCurrency", "ToCurrency", "FetchedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FxTransactions_AccountId_ConvertedAt",
                table: "FxTransactions",
                columns: new[] { "AccountId", "ConvertedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FxTransactions_FromWalletId",
                table: "FxTransactions",
                column: "FromWalletId");

            migrationBuilder.CreateIndex(
                name: "IX_FxTransactions_RateLockId",
                table: "FxTransactions",
                column: "RateLockId");

            migrationBuilder.CreateIndex(
                name: "IX_FxTransactions_Reference",
                table: "FxTransactions",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FxTransactions_ToWalletId",
                table: "FxTransactions",
                column: "ToWalletId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CurrencyPairConfigs");

            migrationBuilder.DropTable(
                name: "FxTransactions");

            migrationBuilder.DropTable(
                name: "FxRateLocks");

            migrationBuilder.DropTable(
                name: "FxRates");

            migrationBuilder.DropIndex(
                name: "IX_Wallets_Iban",
                table: "Wallets");

            migrationBuilder.DropIndex(
                name: "IX_Wallets_WalletReference",
                table: "Wallets");

            migrationBuilder.DropColumn(
                name: "AvailableBalance",
                table: "Wallets");

            migrationBuilder.DropColumn(
                name: "BankName",
                table: "Wallets");

            migrationBuilder.DropColumn(
                name: "Iban",
                table: "Wallets");

            migrationBuilder.DropColumn(
                name: "Model",
                table: "Wallets");

            migrationBuilder.DropColumn(
                name: "ReservedAmount",
                table: "Wallets");

            migrationBuilder.DropColumn(
                name: "SortCode",
                table: "Wallets");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Wallets");

            migrationBuilder.DropColumn(
                name: "SwiftCode",
                table: "Wallets");

            migrationBuilder.DropColumn(
                name: "UkAccountNumber",
                table: "Wallets");

            migrationBuilder.DropColumn(
                name: "WalletReference",
                table: "Wallets");
        }
    }
}
