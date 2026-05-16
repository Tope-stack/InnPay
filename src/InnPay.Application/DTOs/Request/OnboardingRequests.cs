using System.ComponentModel.DataAnnotations;

namespace InnPay.Application.DTOs.Request;

// ─── PERSONAL ONBOARDING ──────────────────────────────────────────────────────

public class RegisterPersonalRequest
{
    [Required] public string FullName { get; set; } = string.Empty;
    [Required, Phone] public string PhoneNumber { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public DateTime DateOfBirth { get; set; }
    [Required, MinLength(8)] public string Password { get; set; } = string.Empty;
}

public class VerifyOtpRequest
{
    [Required] public Guid UserId { get; set; }
    [Required, StringLength(6, MinimumLength = 6)] public string Code { get; set; } = string.Empty;
    [Required] public string Purpose { get; set; } = string.Empty;  // "PhoneVerification" etc.
}

public class SetTransactionPinRequest
{
    [Required] public Guid UserId { get; set; }
    [Required, StringLength(6, MinimumLength = 6)] public string Pin { get; set; } = string.Empty;
    [Required, StringLength(6, MinimumLength = 6)] public string ConfirmPin { get; set; } = string.Empty;
}

// ─── BUSINESS ONBOARDING ─────────────────────────────────────────────────────

public class RegisterBusinessRequest
{
    // Owner personal details
    [Required] public string FullName { get; set; } = string.Empty;
    [Required, Phone] public string PhoneNumber { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, MinLength(8)] public string Password { get; set; } = string.Empty;

    // Business details
    [Required] public string BusinessName { get; set; } = string.Empty;
    [Required] public string RcNumber { get; set; } = string.Empty;
    [Required] public string BusinessType { get; set; } = string.Empty;
    [Required] public string Industry { get; set; } = string.Empty;
}

// ─── CORPORATE ONBOARDING ────────────────────────────────────────────────────

public class RegisterCorporateRequest
{
    // Compliance officer / initiator details
    [Required] public string FullName { get; set; } = string.Empty;
    [Required, Phone] public string PhoneNumber { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, MinLength(8)] public string Password { get; set; } = string.Empty;

    // Corporate entity details
    [Required] public string CorporateName { get; set; } = string.Empty;
    [Required] public string IncorporationNumber { get; set; } = string.Empty;
    [Required] public string CountryOfIncorporation { get; set; } = string.Empty;
    [Required] public string TaxId { get; set; } = string.Empty;
}

// ─── RESEND OTP ───────────────────────────────────────────────────────────────

public class ResendOtpRequest
{
    [Required] public Guid UserId { get; set; }
    [Required] public string Purpose { get; set; } = "PhoneVerification";
}
