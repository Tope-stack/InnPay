using InnPay.Application.Common;
using InnPay.Application.DTOs.Request;
using InnPay.Application.DTOs.Response;

namespace InnPay.Application.Interfaces;

public interface IOnboardingService
{
    Task<ServiceResult<RegisterResponse>> RegisterPersonalAsync(RegisterPersonalRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<RegisterResponse>> RegisterBusinessAsync(RegisterBusinessRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<RegisterResponse>> RegisterCorporateAsync(RegisterCorporateRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<OtpResponse>> VerifyPhoneOtpAsync(VerifyOtpRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<OtpResponse>> ResendOtpAsync(ResendOtpRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<OtpResponse>> SetTransactionPinAsync(SetTransactionPinRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<AccountResponse>> GetAccountAsync(Guid accountId, CancellationToken cancellationToken = default);
}

public interface IAuthService
{
    Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> LogoutAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ServiceResult<OtpResponse>> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<OtpResponse>> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
}

public interface IKycService
{
    Task<ServiceResult<KycStatusResponse>> GetKycStatusAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task<ServiceResult<KycDocumentResponse>> UploadDocumentAsync(UploadKycDocumentRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<KycStatusResponse>> SubmitPersonalKycTier1Async(UploadPersonalKycTier1Request request, CancellationToken cancellationToken = default);
    Task<ServiceResult<KycStatusResponse>> SubmitPersonalKycTier2Async(UploadPersonalKycTier2Request request, CancellationToken cancellationToken = default);
    Task<ServiceResult<KycStatusResponse>> SubmitBusinessKycAsync(UploadBusinessKycRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<KycStatusResponse>> SubmitCorporateKycAsync(UploadCorporateKycRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<KycDocumentResponse>> ReviewDocumentAsync(ReviewKycDocumentRequest request, CancellationToken cancellationToken = default);  // admin
}

public interface IOtpService
{
    Task<string> GenerateAndSendOtpAsync(Guid userId, string phoneNumber, string email, Domain.Enums.OtpPurpose purpose, CancellationToken cancellationToken = default);
    Task<bool> ValidateOtpAsync(Guid userId, string code, Domain.Enums.OtpPurpose purpose, CancellationToken cancellationToken = default);
}

public interface IFileStorageService
{
    Task<string> UploadAsync(string base64Content, string fileName, string contentType, string folder, CancellationToken cancellationToken = default);
    Task DeleteAsync(string fileUrl, CancellationToken cancellationToken = default);
}

public interface ITokenService
{
    string GenerateAccessToken(Domain.Entities.User user, Domain.Entities.Account account);
    Domain.Entities.RefreshToken GenerateRefreshToken(Guid userId, string? ipAddress = null);
    Guid? GetUserIdFromToken(string token);
}

/// <summary>
/// Abstraction for sending SMS messages.
/// Infrastructure provides the concrete implementation (Termii, Twilio, etc.).
/// </summary>
public interface ISmsService
{
    Task SendAsync(string phoneNumber, string message);
}

/// <summary>
/// Abstraction for sending transactional emails.
/// Infrastructure provides the concrete implementation (Zoho SMTP, SendGrid, etc.).
/// </summary>
public interface IEmailService
{
    Task SendAsync(string toEmail, string subject, string body);
}
