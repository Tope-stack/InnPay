using System.ComponentModel.DataAnnotations;
using InnPay.Domain.Enums;

namespace InnPay.Application.DTOs.Request;

public class UploadKycDocumentRequest
{
    [Required] public Guid AccountId { get; set; }
    [Required] public DocumentType DocumentType { get; set; }

    /// Base64-encoded file content
    [Required] public string FileBase64 { get; set; } = string.Empty;
    [Required] public string OriginalFileName { get; set; } = string.Empty;
    [Required] public string ContentType { get; set; } = string.Empty;   // e.g. "image/jpeg", "application/pdf"
}

public class UploadPersonalKycTier1Request
{
    [Required] public Guid AccountId { get; set; }

    /// One of: NationalId, InternationalPassport, DriversLicense
    [Required] public DocumentType IdDocumentType { get; set; }
    [Required] public string IdFileBase64 { get; set; } = string.Empty;
    [Required] public string IdOriginalFileName { get; set; } = string.Empty;
    [Required] public string IdContentType { get; set; } = string.Empty;
}

public class UploadPersonalKycTier2Request
{
    [Required] public Guid AccountId { get; set; }

    // BVN (Bank Verification Number) — verified in-service, not stored raw
    [Required, StringLength(11, MinimumLength = 11)] public string Bvn { get; set; } = string.Empty;

    // Selfie with ID
    [Required] public string SelfieFileBase64 { get; set; } = string.Empty;
    [Required] public string SelfieFileName { get; set; } = string.Empty;
    [Required] public string SelfieContentType { get; set; } = string.Empty;

    // Proof of address
    [Required] public string ProofOfAddressFileBase64 { get; set; } = string.Empty;
    [Required] public string ProofOfAddressFileName { get; set; } = string.Empty;
    [Required] public string ProofOfAddressContentType { get; set; } = string.Empty;
}

public class UploadBusinessKycRequest
{
    [Required] public Guid AccountId { get; set; }

    // CAC Certificate
    [Required] public string CacCertificateBase64 { get; set; } = string.Empty;
    [Required] public string CacCertificateFileName { get; set; } = string.Empty;
    [Required] public string CacCertificateContentType { get; set; } = string.Empty;

    // Utility bill for business address
    [Required] public string UtilityBillBase64 { get; set; } = string.Empty;
    [Required] public string UtilityBillFileName { get; set; } = string.Empty;
    [Required] public string UtilityBillContentType { get; set; } = string.Empty;

    // Director's ID
    [Required] public DocumentType DirectorIdType { get; set; }
    [Required] public string DirectorIdBase64 { get; set; } = string.Empty;
    [Required] public string DirectorIdFileName { get; set; } = string.Empty;
    [Required] public string DirectorIdContentType { get; set; } = string.Empty;
}

public class UploadCorporateKycRequest
{
    [Required] public Guid AccountId { get; set; }

    [Required] public string CertOfIncorporationBase64 { get; set; } = string.Empty;
    [Required] public string CertOfIncorporationFileName { get; set; } = string.Empty;
    [Required] public string CertOfIncorporationContentType { get; set; } = string.Empty;

    [Required] public string MemorandumBase64 { get; set; } = string.Empty;
    [Required] public string MemorandumFileName { get; set; } = string.Empty;
    [Required] public string MemorandumContentType { get; set; } = string.Empty;

    [Required] public string BoardResolutionBase64 { get; set; } = string.Empty;
    [Required] public string BoardResolutionFileName { get; set; } = string.Empty;
    [Required] public string BoardResolutionContentType { get; set; } = string.Empty;

    [Required] public string BeneficialOwnershipBase64 { get; set; } = string.Empty;
    [Required] public string BeneficialOwnershipFileName { get; set; } = string.Empty;
    [Required] public string BeneficialOwnershipContentType { get; set; } = string.Empty;

    // UBO individual KYC documents (one entry per UBO director)
    [Required] public List<UboKycEntry> UboDocuments { get; set; } = new();
}

public class UboKycEntry
{
    [Required] public string FullName { get; set; } = string.Empty;
    [Required] public string IdFileBase64 { get; set; } = string.Empty;
    [Required] public string IdFileName { get; set; } = string.Empty;
    [Required] public string IdContentType { get; set; } = string.Empty;
}

// Admin-only: review a submitted KYC document
public class ReviewKycDocumentRequest
{
    [Required] public Guid DocumentId { get; set; }
    [Required] public bool IsApproved { get; set; }
    public string? RejectionReason { get; set; }
    [Required] public Guid ReviewedByAdminId { get; set; }
}
