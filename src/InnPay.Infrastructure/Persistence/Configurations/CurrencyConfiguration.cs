using InnPay.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnPay.Infrastructure.Persistence.Configurations
{
    public class WalletExtendedConfiguration : IEntityTypeConfiguration<Wallet>
    {
        public void Configure(EntityTypeBuilder<Wallet> builder)
        {
            builder.HasKey(w => w.Id);

            // Unique: one wallet per account per currency
            builder.HasIndex(w => new { w.AccountId, w.Currency }).IsUnique();
            builder.HasIndex(w => w.WalletReference).IsUnique().HasFilter("\"WalletReference\" IS NOT NULL");
            builder.HasIndex(w => w.Iban).IsUnique().HasFilter("\"Iban\" IS NOT NULL");

            builder.Property(w => w.Currency).HasConversion<string>();
            builder.Property(w => w.Model).HasConversion<string>();
            builder.Property(w => w.Status).HasConversion<string>();

            // Decimal precision
            builder.Property(w => w.Balance).HasPrecision(18, 4);
            builder.Property(w => w.AvailableBalance).HasPrecision(18, 4);
            builder.Property(w => w.ReservedAmount).HasPrecision(18, 4);
            builder.Property(w => w.DailyTransactionLimit).HasPrecision(18, 4);

            // Model A
            builder.Property(w => w.WalletReference).HasMaxLength(30);

            // Model B
            builder.Property(w => w.Iban).HasMaxLength(34);           // max IBAN length
            builder.Property(w => w.SortCode).HasMaxLength(8);
            builder.Property(w => w.UkAccountNumber).HasMaxLength(8);
            builder.Property(w => w.SwiftCode).HasMaxLength(11);
            builder.Property(w => w.BankName).HasMaxLength(150);

            // Relationships
            builder.HasMany(w => w.FxTransactionsFrom)
                   .WithOne(t => t.FromWallet)
                   .HasForeignKey(t => t.FromWalletId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(w => w.FxTransactionsTo)
                   .WithOne(t => t.ToWallet)
                   .HasForeignKey(t => t.ToWalletId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class FxRateConfiguration : IEntityTypeConfiguration<FxRate>
    {
        public void Configure(EntityTypeBuilder<FxRate> builder)
        {
            builder.HasKey(r => r.Id);

            // Composite index: fast lookup of latest rate per pair
            builder.HasIndex(r => new { r.FromCurrency, r.ToCurrency, r.FetchedAt });

            builder.Property(r => r.FromCurrency).HasConversion<string>();
            builder.Property(r => r.ToCurrency).HasConversion<string>();
            builder.Property(r => r.Source).HasConversion<string>();

            builder.Property(r => r.MidRate).HasPrecision(18, 8);
            builder.Property(r => r.CustomerRate).HasPrecision(18, 8);
            builder.Property(r => r.SpreadBps).HasPrecision(10, 4);
        }
    }

    public class FxRateLockConfiguration : IEntityTypeConfiguration<FxRateLock>
    {
        public void Configure(EntityTypeBuilder<FxRateLock> builder)
        {
            builder.HasKey(l => l.Id);

            builder.Property(l => l.FromCurrency).HasConversion<string>();
            builder.Property(l => l.ToCurrency).HasConversion<string>();
            builder.Property(l => l.SourceAmount).HasPrecision(18, 4);
            builder.Property(l => l.LockedCustomerRate).HasPrecision(18, 8);
            builder.Property(l => l.EstimatedConvertedAmount).HasPrecision(18, 4);
            builder.Property(l => l.EstimatedFeeAmount).HasPrecision(18, 4);

            // Ignore computed property
            builder.Ignore(l => l.IsExpired);

            builder.HasOne(l => l.Account)
                   .WithMany()
                   .HasForeignKey(l => l.AccountId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(l => l.FxRate)
                   .WithMany()
                   .HasForeignKey(l => l.FxRateId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class FxTransactionConfiguration : IEntityTypeConfiguration<FxTransaction>
    {
        public void Configure(EntityTypeBuilder<FxTransaction> builder)
        {
            builder.HasKey(t => t.Id);

            builder.HasIndex(t => t.Reference).IsUnique();
            builder.HasIndex(t => new { t.AccountId, t.ConvertedAt });   // fast history queries

            builder.Property(t => t.Reference).IsRequired().HasMaxLength(30);
            builder.Property(t => t.FromCurrency).HasConversion<string>();
            builder.Property(t => t.ToCurrency).HasConversion<string>();
            builder.Property(t => t.FeeCurrency).HasConversion<string>();
            builder.Property(t => t.Status).HasConversion<string>();
            builder.Property(t => t.FailureReason).HasMaxLength(500);

            builder.Property(t => t.SourceAmount).HasPrecision(18, 4);
            builder.Property(t => t.AppliedRate).HasPrecision(18, 8);
            builder.Property(t => t.GrossConvertedAmount).HasPrecision(18, 4);
            builder.Property(t => t.FeeAmount).HasPrecision(18, 4);
            builder.Property(t => t.NetConvertedAmount).HasPrecision(18, 4);

            builder.HasOne(t => t.Account)
                   .WithMany()
                   .HasForeignKey(t => t.AccountId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.RateLock)
                   .WithMany()
                   .HasForeignKey(t => t.RateLockId)
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }

    public class CurrencyPairConfigConfiguration : IEntityTypeConfiguration<CurrencyPairConfig>
    {
        public void Configure(EntityTypeBuilder<CurrencyPairConfig> builder)
        {
            builder.HasKey(c => c.Id);

            // Unique pair — only one config per direction
            builder.HasIndex(c => new { c.FromCurrency, c.ToCurrency }).IsUnique();

            builder.Property(c => c.FromCurrency).HasConversion<string>();
            builder.Property(c => c.ToCurrency).HasConversion<string>();

            builder.Property(c => c.SpreadBps).HasPrecision(10, 4);
            builder.Property(c => c.FlatFee).HasPrecision(18, 4);
            builder.Property(c => c.PercentageFee).HasPrecision(10, 4);
            builder.Property(c => c.MinSourceAmount).HasPrecision(18, 4);
            builder.Property(c => c.MaxSourceAmount).HasPrecision(18, 4);

            // Computed property — not mapped
            builder.Ignore(c => c.PairLabel);
        }
    }
}
