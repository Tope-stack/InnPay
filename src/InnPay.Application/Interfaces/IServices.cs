using InnPay.Application.Common;
using InnPay.Application.DTOs.Request;
using InnPay.Application.DTOs.Response;

namespace InnPay.Application.Interfaces;

public interface IOnboardingService
{
    Task<ServiceResult<RegisterResponse>> RegisterPersonalAsync(RegisterPersonalRequest request);
    Task<ServiceResult<RegisterResponse>> RegisterBusinessAsync(RegisterBusinessRequest request);
    Task<ServiceResult<RegisterResponse>> RegisterCorporateAsync(RegisterCorporateRequest request);
    Task<ServiceResult<OtpResponse>> VerifyPhoneOtpAsync(VerifyOtpRequest request);
    Task<ServiceResult<OtpResponse>> ResendOtpAsync(ResendOtpRequest request);
    Task<ServiceResult<OtpResponse>> SetTransactionPinAsync(SetTransactionPinRequest request);
    Task<ServiceResult<AccountResponse>> GetAccountAsync(Guid accountId);
}

public interface IAuthService
{
    Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest request);
    Task<ServiceResult<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request);
    Task<ServiceResult> LogoutAsync(Guid userId);
    Task<ServiceResult<OtpResponse>> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<ServiceResult<OtpResponse>> ResetPasswordAsync(ResetPasswordRequest request);
}

public interface IKycService
{
    Task<ServiceResult<KycStatusResponse>> GetKycStatusAsync(Guid accountId);
    Task<ServiceResult<KycDocumentResponse>> UploadDocumentAsync(UploadKycDocumentRequest request);
    Task<ServiceResult<KycStatusResponse>> SubmitPersonalKycTier1Async(UploadPersonalKycTier1Request request);
    Task<ServiceResult<KycStatusResponse>> SubmitPersonalKycTier2Async(UploadPersonalKycTier2Request request);
    Task<ServiceResult<KycStatusResponse>> SubmitBusinessKycAsync(UploadBusinessKycRequest request);
    Task<ServiceResult<KycStatusResponse>> SubmitCorporateKycAsync(UploadCorporateKycRequest request);
    Task<ServiceResult<KycDocumentResponse>> ReviewDocumentAsync(ReviewKycDocumentRequest request);  // admin
}

public interface IOtpService
{
    Task<string> GenerateAndSendOtpAsync(Guid userId, string phoneNumber, Domain.Enums.OtpPurpose purpose);
    Task<bool> ValidateOtpAsync(Guid userId, string code, Domain.Enums.OtpPurpose purpose);
}

public interface IFileStorageService
{
    Task<string> UploadAsync(string base64Content, string fileName, string contentType, string folder);
    Task DeleteAsync(string fileUrl);
}

public interface ITokenService
{
    string GenerateAccessToken(Domain.Entities.User user, Domain.Entities.Account account);
    Domain.Entities.RefreshToken GenerateRefreshToken(Guid userId, string? ipAddress = null);
    Guid? GetUserIdFromToken(string token);
}
