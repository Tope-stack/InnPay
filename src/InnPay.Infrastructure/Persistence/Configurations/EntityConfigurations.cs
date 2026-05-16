using InnPay.Domain.Entities;
using InnPay.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnPay.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.PhoneNumber).IsUnique();

        builder.Property(u => u.FullName).IsRequired().HasMaxLength(150);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(200);
        builder.Property(u => u.PhoneNumber).IsRequired().HasMaxLength(20);
        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.Status).HasConversion<string>();

        // Soft-delete filter
        builder.HasQueryFilter(u => !u.IsDeleted);

        builder.HasMany(u => u.Accounts)
               .WithOne(a => a.User)
               .HasForeignKey(a => a.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.OtpCodes)
               .WithOne(o => o.User)
               .HasForeignKey(o => o.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.RefreshTokens)
               .WithOne(r => r.User)
               .HasForeignKey(r => r.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.HasKey(a => a.Id);
        builder.HasIndex(a => a.AccountNumber).IsUnique();

        builder.Property(a => a.AccountType).HasConversion<string>();
        builder.Property(a => a.KycTier).HasConversion<string>();
        builder.Property(a => a.KycStatus).HasConversion<string>();
        builder.Property(a => a.Status).HasConversion<string>();

        builder.Property(a => a.BusinessName).HasMaxLength(200);
        builder.Property(a => a.RcNumber).HasMaxLength(50);
        builder.Property(a => a.CorporateName).HasMaxLength(250);
        builder.Property(a => a.IncorporationNumber).HasMaxLength(100);
        builder.Property(a => a.TaxId).HasMaxLength(50);

        builder.HasQueryFilter(a => !a.IsDeleted);

        builder.HasMany(a => a.KycDocuments)
               .WithOne(d => d.Account)
               .HasForeignKey(d => d.AccountId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.Wallets)
               .WithOne(w => w.Account)
               .HasForeignKey(w => w.AccountId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.TeamMembers)
               .WithOne(t => t.Account)
               .HasForeignKey(t => t.AccountId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class KycDocumentConfiguration : IEntityTypeConfiguration<KycDocument>
{
    public void Configure(EntityTypeBuilder<KycDocument> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.DocumentType).HasConversion<string>();
        builder.Property(d => d.Status).HasConversion<string>();
        builder.Property(d => d.FileUrl).IsRequired().HasMaxLength(500);
        builder.Property(d => d.OriginalFileName).HasMaxLength(250);
        builder.Property(d => d.RejectionReason).HasMaxLength(500);
    }
}

public class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.HasKey(w => w.Id);

        // Unique: one wallet per account per currency
        builder.HasIndex(w => new { w.AccountId, w.Currency }).IsUnique();

        builder.Property(w => w.Currency).HasConversion<string>();
        builder.Property(w => w.Balance).HasPrecision(18, 4);
        builder.Property(w => w.DailyTransactionLimit).HasPrecision(18, 4);
    }
}

public class OtpConfiguration : IEntityTypeConfiguration<OtpCode>
{
    public void Configure(EntityTypeBuilder<OtpCode> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Code).IsRequired().HasMaxLength(10);
        builder.Property(o => o.Purpose).HasConversion<string>();
        builder.Property(o => o.PhoneNumber).HasMaxLength(20);

        // Index for fast lookup by user + purpose
        builder.HasIndex(o => new { o.UserId, o.Purpose, o.IsUsed });
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(r => r.Id);
        builder.HasIndex(r => r.Token).IsUnique();
        builder.Property(r => r.Token).IsRequired().HasMaxLength(512);
        builder.Property(r => r.CreatedByIp).HasMaxLength(50);
    }
}

public class TeamMemberConfiguration : IEntityTypeConfiguration<TeamMember>
{
    public void Configure(EntityTypeBuilder<TeamMember> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Role).HasConversion<string>();

        // One user per account (no duplicate team members)
        builder.HasIndex(t => new { t.AccountId, t.UserId }).IsUnique();

        builder.HasOne(t => t.User)
               .WithMany()
               .HasForeignKey(t => t.UserId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
