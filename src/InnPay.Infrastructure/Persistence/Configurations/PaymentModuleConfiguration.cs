using InnPay.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnPay.Infrastructure.Persistence.Configurations;

// ── 6.1 Payment ───────────────────────────────────────────────────────────────

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.HasKey(p => p.Id);
        b.Property(p => p.Amount).HasPrecision(18, 4);
        b.Property(p => p.IdempotencyKey).HasMaxLength(100).IsRequired();
        b.Property(p => p.Reference).HasMaxLength(50).IsRequired();
        b.Property(p => p.ProviderReference).HasMaxLength(200);
        b.Property(p => p.ClientSecret).HasMaxLength(500);
        b.Property(p => p.FailureReason).HasMaxLength(500);

        b.HasIndex(p => p.IdempotencyKey).IsUnique();
        b.HasIndex(p => p.Reference).IsUnique();
        b.HasIndex(p => p.AccountId);

        b.HasOne(p => p.Account).WithMany().HasForeignKey(p => p.AccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(p => p.Wallet).WithMany().HasForeignKey(p => p.WalletId).OnDelete(DeleteBehavior.Restrict);
    }
}

// ── 6.3 Internal Transfer ─────────────────────────────────────────────────────

public class InternalTransferConfiguration : IEntityTypeConfiguration<InternalTransfer>
{
    public void Configure(EntityTypeBuilder<InternalTransfer> b)
    {
        b.HasKey(t => t.Id);
        b.Property(t => t.Amount).HasPrecision(18, 4);
        b.Property(t => t.Reference).HasMaxLength(50).IsRequired();
        b.Property(t => t.Note).HasMaxLength(255);
        b.Property(t => t.FailureReason).HasMaxLength(500);

        b.HasIndex(t => t.Reference).IsUnique();
        b.HasIndex(t => t.SenderAccountId);
        b.HasIndex(t => t.ReceiverAccountId);

        b.HasOne(t => t.SenderAccount).WithMany().HasForeignKey(t => t.SenderAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(t => t.ReceiverAccount).WithMany().HasForeignKey(t => t.ReceiverAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(t => t.SenderWallet).WithMany().HasForeignKey(t => t.SenderWalletId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(t => t.ReceiverWallet).WithMany().HasForeignKey(t => t.ReceiverWalletId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class TransferScheduleConfiguration : IEntityTypeConfiguration<TransferSchedule>
{
    public void Configure(EntityTypeBuilder<TransferSchedule> b)
    {
        b.HasKey(s => s.Id);
        b.Property(s => s.Amount).HasPrecision(18, 4);
        b.Property(s => s.Note).HasMaxLength(255);

        b.HasIndex(s => s.SenderAccountId);
        b.HasIndex(s => new { s.IsActive, s.NextRunAt });

        b.HasOne(s => s.SenderAccount).WithMany().HasForeignKey(s => s.SenderAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(s => s.SenderWallet).WithMany().HasForeignKey(s => s.SenderWalletId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class SavedBeneficiaryConfiguration : IEntityTypeConfiguration<SavedBeneficiary>
{
    public void Configure(EntityTypeBuilder<SavedBeneficiary> b)
    {
        b.HasKey(sb => sb.Id);
        b.Property(sb => sb.Nickname).HasMaxLength(100).IsRequired();

        b.HasIndex(sb => new { sb.OwnerAccountId, sb.BeneficiaryAccountId }).IsUnique();

        b.HasOne(sb => sb.OwnerAccount).WithMany().HasForeignKey(sb => sb.OwnerAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(sb => sb.BeneficiaryAccount).WithMany().HasForeignKey(sb => sb.BeneficiaryAccountId).OnDelete(DeleteBehavior.Restrict);
    }
}

// ── 6.4 External Transfer ─────────────────────────────────────────────────────

public class ExternalTransferConfiguration : IEntityTypeConfiguration<ExternalTransfer>
{
    public void Configure(EntityTypeBuilder<ExternalTransfer> b)
    {
        b.HasKey(t => t.Id);
        b.Property(t => t.Amount).HasPrecision(18, 4);
        b.Property(t => t.Reference).HasMaxLength(50).IsRequired();
        b.Property(t => t.DestinationBankCode).HasMaxLength(20);
        b.Property(t => t.DestinationBankName).HasMaxLength(100);
        b.Property(t => t.DestinationAccountNumber).HasMaxLength(20);
        b.Property(t => t.DestinationAccountName).HasMaxLength(200);
        b.Property(t => t.NibssSessionId).HasMaxLength(50);
        b.Property(t => t.SwiftReference).HasMaxLength(50);
        b.Property(t => t.FailureReason).HasMaxLength(500);

        b.HasIndex(t => t.Reference).IsUnique();
        b.HasIndex(t => t.SenderAccountId);

        b.HasOne(t => t.SenderAccount).WithMany().HasForeignKey(t => t.SenderAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(t => t.SenderWallet).WithMany().HasForeignKey(t => t.SenderWalletId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(t => t.SavedBankAccount).WithMany().HasForeignKey(t => t.SavedBankAccountId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class SavedBankAccountConfiguration : IEntityTypeConfiguration<SavedBankAccount>
{
    public void Configure(EntityTypeBuilder<SavedBankAccount> b)
    {
        b.HasKey(s => s.Id);
        b.Property(s => s.BankCode).HasMaxLength(20).IsRequired();
        b.Property(s => s.BankName).HasMaxLength(100).IsRequired();
        b.Property(s => s.AccountNumber).HasMaxLength(20).IsRequired();
        b.Property(s => s.AccountName).HasMaxLength(200).IsRequired();

        b.HasIndex(s => s.AccountId);
        b.HasOne(s => s.Account).WithMany().HasForeignKey(s => s.AccountId).OnDelete(DeleteBehavior.Cascade);
    }
}

// ── 6.2 Bills ─────────────────────────────────────────────────────────────────

public class BillPaymentConfiguration : IEntityTypeConfiguration<BillPayment>
{
    public void Configure(EntityTypeBuilder<BillPayment> b)
    {
        b.HasKey(bp => bp.Id);
        b.Property(bp => bp.Amount).HasPrecision(18, 4);
        b.Property(bp => bp.Reference).HasMaxLength(50).IsRequired();
        b.Property(bp => bp.BillerCode).HasMaxLength(50).IsRequired();
        b.Property(bp => bp.BillerName).HasMaxLength(100).IsRequired();
        b.Property(bp => bp.CustomerReference).HasMaxLength(50).IsRequired();
        b.Property(bp => bp.CustomerName).HasMaxLength(200);
        b.Property(bp => bp.BillerReference).HasMaxLength(100);
        b.Property(bp => bp.ElectricityToken).HasMaxLength(100);
        b.Property(bp => bp.FailureReason).HasMaxLength(500);

        b.HasIndex(bp => bp.Reference).IsUnique();
        b.HasIndex(bp => bp.AccountId);

        b.HasOne(bp => bp.Account).WithMany().HasForeignKey(bp => bp.AccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(bp => bp.Wallet).WithMany().HasForeignKey(bp => bp.WalletId).OnDelete(DeleteBehavior.Restrict);
    }
}

// ── 6.5 Virtual Accounts & 6.6 Withdrawals ───────────────────────────────────

public class VirtualAccountConfiguration : IEntityTypeConfiguration<VirtualAccount>
{
    public void Configure(EntityTypeBuilder<VirtualAccount> b)
    {
        b.HasKey(v => v.Id);
        b.Property(v => v.AccountNumber).HasMaxLength(20).IsRequired();
        b.Property(v => v.BankName).HasMaxLength(100).IsRequired();
        b.Property(v => v.BankCode).HasMaxLength(20).IsRequired();
        b.Property(v => v.AccountName).HasMaxLength(200).IsRequired();
        b.Property(v => v.Label).HasMaxLength(100);

        b.HasIndex(v => v.AccountNumber).IsUnique();
        b.HasIndex(v => v.AccountId);

        b.HasOne(v => v.Account).WithMany().HasForeignKey(v => v.AccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(v => v.Wallet).WithMany().HasForeignKey(v => v.WalletId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class WithdrawalConfiguration : IEntityTypeConfiguration<Withdrawal>
{
    public void Configure(EntityTypeBuilder<Withdrawal> b)
    {
        b.HasKey(w => w.Id);
        b.Property(w => w.Amount).HasPrecision(18, 4);
        b.Property(w => w.Reference).HasMaxLength(50).IsRequired();
        b.Property(w => w.DestinationBankCode).HasMaxLength(20);
        b.Property(w => w.DestinationBankName).HasMaxLength(100);
        b.Property(w => w.DestinationAccountNumber).HasMaxLength(20);
        b.Property(w => w.DestinationAccountName).HasMaxLength(200);
        b.Property(w => w.ProviderReference).HasMaxLength(100);
        b.Property(w => w.FailureReason).HasMaxLength(500);

        b.HasIndex(w => w.Reference).IsUnique();
        b.HasIndex(w => w.AccountId);

        b.HasOne(w => w.Account).WithMany().HasForeignKey(w => w.AccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(w => w.Wallet).WithMany().HasForeignKey(w => w.WalletId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(w => w.SavedBankAccount).WithMany().HasForeignKey(w => w.SavedBankAccountId).OnDelete(DeleteBehavior.SetNull);
    }
}

// ── 6.7 Virtual Cards ─────────────────────────────────────────────────────────

public class VirtualCardConfiguration : IEntityTypeConfiguration<VirtualCard>
{
    public void Configure(EntityTypeBuilder<VirtualCard> b)
    {
        b.HasKey(c => c.Id);
        b.Property(c => c.PanEncrypted).HasMaxLength(500).IsRequired();
        b.Property(c => c.Last4).HasMaxLength(4).IsRequired();
        b.Property(c => c.Expiry).HasMaxLength(5).IsRequired();
        b.Property(c => c.CvvEncrypted).HasMaxLength(200).IsRequired();
        b.Property(c => c.ProviderCardId).HasMaxLength(100).IsRequired();
        b.Property(c => c.SpendingLimitPerTransaction).HasPrecision(18, 4);
        b.Property(c => c.SpendingLimitMonthly).HasPrecision(18, 4);
        b.Property(c => c.TerminationReason).HasMaxLength(255);

        b.HasIndex(c => c.ProviderCardId).IsUnique();
        b.HasIndex(c => c.AccountId);

        b.HasOne(c => c.Account).WithMany().HasForeignKey(c => c.AccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(c => c.Wallet).WithMany().HasForeignKey(c => c.WalletId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(c => c.Transactions).WithOne(t => t.VirtualCard).HasForeignKey(t => t.VirtualCardId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class CardTransactionConfiguration : IEntityTypeConfiguration<CardTransaction>
{
    public void Configure(EntityTypeBuilder<CardTransaction> b)
    {
        b.HasKey(t => t.Id);
        b.Property(t => t.Amount).HasPrecision(18, 4);
        b.Property(t => t.MerchantName).HasMaxLength(200);
        b.Property(t => t.MerchantCategory).HasMaxLength(100);
        b.Property(t => t.ProviderTransactionId).HasMaxLength(100);
        b.Property(t => t.DeclineReason).HasMaxLength(255);

        b.HasIndex(t => t.VirtualCardId);
        b.HasIndex(t => t.TransactedAt);
    }
}

// ── 6.8 Gift Cards ────────────────────────────────────────────────────────────

public class GiftCardPurchaseConfiguration : IEntityTypeConfiguration<GiftCardPurchase>
{
    public void Configure(EntityTypeBuilder<GiftCardPurchase> b)
    {
        b.HasKey(g => g.Id);
        b.Property(g => g.DenominationUsd).HasPrecision(18, 4);
        b.Property(g => g.AmountCharged).HasPrecision(18, 4);
        b.Property(g => g.Reference).HasMaxLength(50).IsRequired();
        b.Property(g => g.BrandName).HasMaxLength(100).IsRequired();
        b.Property(g => g.CountryCode).HasMaxLength(3).IsRequired();
        b.Property(g => g.RedemptionCodeEncrypted).HasMaxLength(1000);
        b.Property(g => g.RedemptionPin).HasMaxLength(20);
        b.Property(g => g.ReloadlyOrderId).HasMaxLength(100);
        b.Property(g => g.FailureReason).HasMaxLength(500);

        b.HasIndex(g => g.Reference).IsUnique();
        b.HasIndex(g => g.AccountId);

        b.HasOne(g => g.Account).WithMany().HasForeignKey(g => g.AccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(g => g.Wallet).WithMany().HasForeignKey(g => g.WalletId).OnDelete(DeleteBehavior.Restrict);
    }
}

// ── 6.9 Flights ───────────────────────────────────────────────────────────────

public class FlightBookingConfiguration : IEntityTypeConfiguration<FlightBooking>
{
    public void Configure(EntityTypeBuilder<FlightBooking> b)
    {
        b.HasKey(f => f.Id);
        b.Property(f => f.AmountCharged).HasPrecision(18, 4);
        b.Property(f => f.BookingReference).HasMaxLength(50).IsRequired();
        b.Property(f => f.Airline).HasMaxLength(100);
        b.Property(f => f.Origin).HasMaxLength(3);
        b.Property(f => f.Destination).HasMaxLength(3);
        b.Property(f => f.CabinClass).HasMaxLength(50);
        b.Property(f => f.ETicketUrl).HasMaxLength(500);
        b.Property(f => f.AmadeusOrderId).HasMaxLength(100);
        b.Property(f => f.FailureReason).HasMaxLength(500);

        b.HasIndex(f => f.BookingReference).IsUnique();
        b.HasIndex(f => f.AccountId);

        b.HasOne(f => f.Account).WithMany().HasForeignKey(f => f.AccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(f => f.Wallet).WithMany().HasForeignKey(f => f.WalletId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(f => f.Passengers).WithOne(p => p.FlightBooking).HasForeignKey(p => p.FlightBookingId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class FlightPassengerConfiguration : IEntityTypeConfiguration<FlightPassenger>
{
    public void Configure(EntityTypeBuilder<FlightPassenger> b)
    {
        b.HasKey(p => p.Id);
        b.Property(p => p.FirstName).HasMaxLength(100).IsRequired();
        b.Property(p => p.LastName).HasMaxLength(100).IsRequired();
        b.Property(p => p.PassportNumber).HasMaxLength(20).IsRequired();
        b.Property(p => p.Nationality).HasMaxLength(3).IsRequired();
    }
}

// ── 6.10 Bet Funding ──────────────────────────────────────────────────────────

public class BetFundingConfiguration : IEntityTypeConfiguration<BetFunding>
{
    public void Configure(EntityTypeBuilder<BetFunding> b)
    {
        b.HasKey(f => f.Id);
        b.Property(f => f.Amount).HasPrecision(18, 4);
        b.Property(f => f.Reference).HasMaxLength(50).IsRequired();
        b.Property(f => f.BettingUserId).HasMaxLength(100).IsRequired();
        b.Property(f => f.ProviderReference).HasMaxLength(100);
        b.Property(f => f.FailureReason).HasMaxLength(500);

        b.HasIndex(f => f.Reference).IsUnique();
        b.HasIndex(f => f.AccountId);

        b.HasOne(f => f.Account).WithMany().HasForeignKey(f => f.AccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(f => f.Wallet).WithMany().HasForeignKey(f => f.WalletId).OnDelete(DeleteBehavior.Restrict);
    }
}
