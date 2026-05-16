using InnPay.Domain.Enums;

namespace InnPay.Domain.Entities;

public class Account : BaseEntity
{
    public Guid UserId { get; set; }
    public AccountType AccountType { get; set; }
    public KycTier KycTier { get; set; } = KycTier.None;
    public KycStatus KycStatus { get; set; } = KycStatus.NotSubmitted;
    public AccountStatus Status { get; set; } = AccountStatus.Pending;
    public string AccountNumber { get; set; } = string.Empty;  // system-generated reference

    // Business-specific fields
    public string? BusinessName { get; set; }
    public string? RcNumber { get; set; }       // CAC registration number
    public string? BusinessType { get; set; }
    public string? Industry { get; set; }

    // Corporate-specific fields
    public string? CorporateName { get; set; }
    public string? IncorporationNumber { get; set; }
    public string? CountryOfIncorporation { get; set; }
    public string? TaxId { get; set; }
    public bool ComplianceReviewComplete { get; set; } = false;

    // Navigation
    public User User { get; set; } = null!;
    public ICollection<KycDocument> KycDocuments { get; set; } = new List<KycDocument>();
    public ICollection<Wallet> Wallets { get; set; } = new List<Wallet>();
    public ICollection<TeamMember> TeamMembers { get; set; } = new List<TeamMember>();
}
