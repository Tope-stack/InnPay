using InnPay.Application.Common;
using InnPay.Application.DTOs.Request;
using InnPay.Application.DTOs.Response;
using InnPay.Application.Interfaces;
using InnPay.Domain.Enums;
using InnPay.Domain.Interfaces;

namespace InnPay.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _uow;
    private readonly ITokenService _tokenService;
    private readonly IOtpService _otpService;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(
        IUnitOfWork uow,
        ITokenService tokenService,
        IOtpService otpService,
        IPasswordHasher passwordHasher)
    {
        _uow = uow;
        _tokenService = tokenService;
        _otpService = otpService;
        _passwordHasher = passwordHasher;
    }

    public async Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _uow.Users.GetByEmailAsync(request.Email.Trim().ToLowerInvariant());
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            return ServiceResult<AuthResponse>.Fail("Invalid email or password.", 401);

        if (!user.IsPhoneVerified)
            return ServiceResult<AuthResponse>.Fail("Please verify your phone number before logging in.", 403);

        if (user.Status == AccountStatus.Suspended)
            return ServiceResult<AuthResponse>.Fail("Your account has been suspended. Contact support.", 403);

        var account = await _uow.Accounts.GetByUserIdAsync(user.Id);
        if (account is null)
            return ServiceResult<AuthResponse>.Fail("Account not found.", 404);

        var accessToken = _tokenService.GenerateAccessToken(user, account);
        var refreshToken = _tokenService.GenerateRefreshToken(user.Id);

        await _uow.RefreshTokens.AddAsync(refreshToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return ServiceResult<AuthResponse>.Success(new AuthResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(15),
            AccountType = account.AccountType,
            KycTier = account.KycTier,
            AccountStatus = account.Status
        });
    }

    public async Task<ServiceResult<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var storedToken = await _uow.RefreshTokens.GetByTokenAsync(request.RefreshToken);

        if (storedToken is null || storedToken.IsRevoked || storedToken.ExpiresAt < DateTime.UtcNow)
            return ServiceResult<AuthResponse>.Fail("Invalid or expired refresh token.", 401);

        // Rotate: revoke the old token
        storedToken.IsRevoked = true;
        await _uow.RefreshTokens.UpdateAsync(storedToken);

        var user = await _uow.Users.GetByIdAsync(storedToken.UserId);
        if (user is null)
            return ServiceResult<AuthResponse>.Fail("User not found.", 404);

        var account = await _uow.Accounts.GetByUserIdAsync(user.Id);
        if (account is null)
            return ServiceResult<AuthResponse>.Fail("Account not found.", 404);

        var newAccessToken = _tokenService.GenerateAccessToken(user, account);
        var newRefreshToken = _tokenService.GenerateRefreshToken(user.Id);
        storedToken.ReplacedByToken = newRefreshToken.Token;

        await _uow.RefreshTokens.AddAsync(newRefreshToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return ServiceResult<AuthResponse>.Success(new AuthResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken.Token,
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(15),
            AccountType = account.AccountType,
            KycTier = account.KycTier,
            AccountStatus = account.Status
        });
    }

    public async Task<ServiceResult> LogoutAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _uow.RefreshTokens.RevokeAllForUserAsync(userId);
        await _uow.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult<OtpResponse>> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _uow.Users.GetByEmailAsync(request.Email.Trim().ToLowerInvariant());

        // Always return success to prevent email enumeration
        if (user is null)
            return ServiceResult<OtpResponse>.Success(new OtpResponse
            {
                Success = true,
                Message = "If your email is registered, an OTP has been sent to your phone."
            });

        await _uow.Otps.InvalidateAllForUserAsync(user.Id, OtpPurpose.PasswordReset);
        await _otpService.GenerateAndSendOtpAsync(user.Id, user.PhoneNumber, user.Email, OtpPurpose.PasswordReset, cancellationToken);

        return ServiceResult<OtpResponse>.Success(new OtpResponse
        {
            Success = true,
            Message = "If your email is registered, an OTP has been sent to your phone."
        });
    }

    public async Task<ServiceResult<OtpResponse>> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _uow.Users.GetByIdAsync(request.UserId);
        if (user is null)
            return ServiceResult<OtpResponse>.Fail("User not found.", 404);

        var isValid = await _otpService.ValidateOtpAsync(user.Id, request.OtpCode, OtpPurpose.PasswordReset, cancellationToken);
        if (!isValid)
            return ServiceResult<OtpResponse>.Fail("Invalid or expired OTP.", 422);

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await _uow.Users.UpdateAsync(user);

        // Revoke all refresh tokens on password reset
        await _uow.RefreshTokens.RevokeAllForUserAsync(user.Id);
        await _uow.SaveChangesAsync(cancellationToken);

        return ServiceResult<OtpResponse>.Success(new OtpResponse
        {
            Success = true,
            Message = "Password reset successfully. Please log in again."
        });
    }
}
