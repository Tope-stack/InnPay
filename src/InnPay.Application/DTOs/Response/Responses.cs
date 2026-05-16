using InnPay.Domain.Enums;

namespace InnPay.Application.DTOs.Response;

public class RegisterResponse
{
    public Guid UserId { get; set; }
    public Guid AccountId { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool OtpSent { get; set; }
}

public class AuthResponse
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAt { get; set; }
    public AccountType AccountType { get; set; }
    public KycTier KycTier { get; set; }
    public AccountStatus AccountStatus { get; set; }
}

public class OtpResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class AccountResponse
{
    public Guid AccountId { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public AccountType AccountType { get; set; }
    public KycTier KycTier { get; set; }
    public KycStatus KycStatus { get; set; }
    public AccountStatus Status { get; set; }

    // Business-specific
    public string? BusinessName { get; set; }
    public string? RcNumber { get; set; }

    // Corporate-specific
    public string? CorporateName { get; set; }
    public string? IncorporationNumber { get; set; }

    public List<WalletResponse> Wallets { get; set; } = new();
}

public class WalletResponse
{
    public Guid WalletId { get; set; }
    public WalletCurrency Currency { get; set; }
    public decimal Balance { get; set; }
    public bool IsActive { get; set; }
    public decimal DailyTransactionLimit { get; set; }
}

public class KycDocumentResponse
{
    public Guid DocumentId { get; set; }
    public DocumentType DocumentType { get; set; }
    public KycStatus Status { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime UploadedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
}

public class KycStatusResponse
{
    public Guid AccountId { get; set; }
    public KycTier CurrentTier { get; set; }
    public KycStatus OverallStatus { get; set; }
    public List<KycDocumentResponse> Documents { get; set; } = new();
    public string? NextStepMessage { get; set; }
}
