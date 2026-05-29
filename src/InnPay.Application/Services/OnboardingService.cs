using InnPay.Application.Common;
using InnPay.Application.DTOs.Request;
using InnPay.Application.DTOs.Response;
using InnPay.Application.Interfaces;
using InnPay.Domain.Entities;
using InnPay.Domain.Enums;
using InnPay.Domain.Interfaces;

namespace InnPay.Application.Services;

public class OnboardingService : IOnboardingService
{
    private readonly IUnitOfWork _uow;
    private readonly IOtpService _otpService;
    private readonly IPasswordHasher _passwordHasher;

    public OnboardingService(IUnitOfWork uow, IOtpService otpService, IPasswordHasher passwordHasher)
    {
        _uow = uow;
        _otpService = otpService;
        _passwordHasher = passwordHasher;
    }

    // ─── PERSONAL ────────────────────────────────────────────────────────────

    public async Task<ServiceResult<RegisterResponse>> RegisterPersonalAsync(RegisterPersonalRequest request)
    {
        // Duplicate checks
        if (await _uow.Users.EmailExistsAsync(request.Email))
            return ServiceResult<RegisterResponse>.Fail("Email is already registered.", 409);

        if (await _uow.Users.PhoneExistsAsync(request.PhoneNumber))
            return ServiceResult<RegisterResponse>.Fail("Phone number is already registered.", 409);

        var user = new User
        {
            FullName = request.FullName.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            DateOfBirth = request.DateOfBirth,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Status = AccountStatus.Pending
        };

        await _uow.Users.AddAsync(user);

        var account = new Account
        {
            UserId = user.Id,
            AccountType = AccountType.Personal,
            KycTier = KycTier.None,
            KycStatus = KycStatus.NotSubmitted,
            Status = AccountStatus.Pending,
            AccountNumber = GenerateAccountNumber()
        };

        await _uow.Accounts.AddAsync(account);

        // Provision NGN wallet (active immediately for Tier 0 with 50k/day limit)
        var ngnWallet = new Wallet
        {
            AccountId = account.Id,
            Currency = WalletCurrency.NGN,
            IsActive = true,
            DailyTransactionLimit = 50_000m
        };

        await _uow.Wallets.AddAsync(ngnWallet);
        await _uow.SaveChangesAsync();

        // Send OTP
        await _otpService.GenerateAndSendOtpAsync(user.Id, user.PhoneNumber, user.Email, OtpPurpose.PhoneVerification);

        return ServiceResult<RegisterResponse>.Success(new RegisterResponse
        {
            UserId = user.Id,
            AccountId = account.Id,
            Message = "Registration successful. Please verify your phone number.",
            OtpSent = true
        }, 201);
    }

    // ─── BUSINESS ────────────────────────────────────────────────────────────

    public async Task<ServiceResult<RegisterResponse>> RegisterBusinessAsync(RegisterBusinessRequest request)
    {
        if (await _uow.Users.EmailExistsAsync(request.Email))
            return ServiceResult<RegisterResponse>.Fail("Email is already registered.", 409);

        if (await _uow.Users.PhoneExistsAsync(request.PhoneNumber))
            return ServiceResult<RegisterResponse>.Fail("Phone number is already registered.", 409);

        var user = new User
        {
            FullName = request.FullName.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            Status = AccountStatus.Pending
        };

        await _uow.Users.AddAsync(user);

        var account = new Account
        {
            UserId = user.Id,
            AccountType = AccountType.Business,
            BusinessName = request.BusinessName.Trim(),
            RcNumber = request.RcNumber.Trim(),
            BusinessType = request.BusinessType.Trim(),
            Industry = request.Industry.Trim(),
            KycTier = KycTier.None,
            KycStatus = KycStatus.NotSubmitted,
            // Business accounts are pending until CAC verification
            Status = AccountStatus.Pending,
            AccountNumber = GenerateAccountNumber()
        };

        await _uow.Accounts.AddAsync(account);

        // NGN business wallet — inbound-only until KYC approved
        var ngnWallet = new Wallet
        {
            AccountId = account.Id,
            Currency = WalletCurrency.NGN,
            IsActive = true,
            DailyTransactionLimit = 0m   // outbound blocked until KYC verified
        };

        await _uow.Wallets.AddAsync(ngnWallet);
        await _uow.SaveChangesAsync();

        await _otpService.GenerateAndSendOtpAsync(user.Id, user.PhoneNumber, user.Email, OtpPurpose.PhoneVerification);

        return ServiceResult<RegisterResponse>.Success(new RegisterResponse
        {
            UserId = user.Id,
            AccountId = account.Id,
            Message = "Business registration successful. Please verify your phone number and submit KYC documents.",
            OtpSent = true
        }, 201);
    }

    // ─── CORPORATE ───────────────────────────────────────────────────────────

    public async Task<ServiceResult<RegisterResponse>> RegisterCorporateAsync(RegisterCorporateRequest request)
    {
        if (await _uow.Users.EmailExistsAsync(request.Email))
            return ServiceResult<RegisterResponse>.Fail("Email is already registered.", 409);

        if (await _uow.Users.PhoneExistsAsync(request.PhoneNumber))
            return ServiceResult<RegisterResponse>.Fail("Phone number is already registered.", 409);

        var user = new User
        {
            FullName = request.FullName.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            Status = AccountStatus.Pending
        };

        await _uow.Users.AddAsync(user);

        var account = new Account
        {
            UserId = user.Id,
            AccountType = AccountType.Corporate,
            CorporateName = request.CorporateName.Trim(),
            IncorporationNumber = request.IncorporationNumber.Trim(),
            CountryOfIncorporation = request.CountryOfIncorporation.Trim(),
            TaxId = request.TaxId.Trim(),
            KycTier = KycTier.None,
            KycStatus = KycStatus.NotSubmitted,
            Status = AccountStatus.Pending,   // requires InnPay compliance review (up to 3 days)
            AccountNumber = GenerateAccountNumber()
        };

        await _uow.Accounts.AddAsync(account);
        await _uow.SaveChangesAsync();

        await _otpService.GenerateAndSendOtpAsync(user.Id, user.PhoneNumber, user.Email, OtpPurpose.PhoneVerification);

        return ServiceResult<RegisterResponse>.Success(new RegisterResponse
        {
            UserId = user.Id,
            AccountId = account.Id,
            Message = "Corporate registration initiated. Please verify your phone number and submit required documents. Account activation takes up to 3 business days.",
            OtpSent = true
        }, 201);
    }

    // ─── OTP VERIFICATION ────────────────────────────────────────────────────

    public async Task<ServiceResult<OtpResponse>> VerifyPhoneOtpAsync(VerifyOtpRequest request)
    {
        if (!Enum.TryParse<OtpPurpose>(request.Purpose, true, out var purpose))
            return ServiceResult<OtpResponse>.Fail("Invalid OTP purpose.");

        var isValid = await _otpService.ValidateOtpAsync(request.UserId, request.Code, purpose);
        if (!isValid)
            return ServiceResult<OtpResponse>.Fail("Invalid or expired OTP code.", 422);

        var user = await _uow.Users.GetByIdAsync(request.UserId);
        if (user is null)
            return ServiceResult<OtpResponse>.Fail("User not found.", 404);

        if (purpose == OtpPurpose.PhoneVerification)
        {
            user.IsPhoneVerified = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _uow.Users.UpdateAsync(user);
            await _uow.SaveChangesAsync();
        }

        return ServiceResult<OtpResponse>.Success(new OtpResponse
        {
            Success = true,
            Message = "OTP verified successfully."
        });
    }

    public async Task<ServiceResult<OtpResponse>> ResendOtpAsync(ResendOtpRequest request)
    {
        var user = await _uow.Users.GetByIdAsync(request.UserId);
        if (user is null)
            return ServiceResult<OtpResponse>.Fail("User not found.", 404);

        if (!Enum.TryParse<OtpPurpose>(request.Purpose, true, out var purpose))
            return ServiceResult<OtpResponse>.Fail("Invalid OTP purpose.");

        // Invalidate existing OTPs for this purpose
        await _uow.Otps.InvalidateAllForUserAsync(user.Id, purpose);
        await _otpService.GenerateAndSendOtpAsync(user.Id, user.PhoneNumber, user.Email, purpose);

        return ServiceResult<OtpResponse>.Success(new OtpResponse
        {
            Success = true,
            Message = "OTP resent successfully."
        });
    }

    // ─── TRANSACTION PIN ─────────────────────────────────────────────────────

    public async Task<ServiceResult<OtpResponse>> SetTransactionPinAsync(SetTransactionPinRequest request)
    {
        if (request.Pin != request.ConfirmPin)
            return ServiceResult<OtpResponse>.Fail("PINs do not match.");

        var user = await _uow.Users.GetByIdAsync(request.UserId);
        if (user is null)
            return ServiceResult<OtpResponse>.Fail("User not found.", 404);

        if (!user.IsPhoneVerified)
            return ServiceResult<OtpResponse>.Fail("Phone number must be verified before setting a PIN.", 422);

        user.TransactionPinHash = _passwordHasher.Hash(request.Pin);
        user.Status = AccountStatus.Active;    // user is now fully active
        user.UpdatedAt = DateTime.UtcNow;

        await _uow.Users.UpdateAsync(user);

        // Also activate the account now that user setup is complete
        var account = await _uow.Accounts.GetByUserIdAsync(user.Id);
        if (account is not null && account.AccountType == AccountType.Personal)
        {
            account.Status = AccountStatus.Active;
            account.UpdatedAt = DateTime.UtcNow;
            await _uow.Accounts.UpdateAsync(account);
        }

        await _uow.SaveChangesAsync();

        return ServiceResult<OtpResponse>.Success(new OtpResponse
        {
            Success = true,
            Message = "Transaction PIN set successfully."
        });
    }

    // ─── GET ACCOUNT ─────────────────────────────────────────────────────────

    public async Task<ServiceResult<AccountResponse>> GetAccountAsync(Guid accountId)
    {
        var account = await _uow.Accounts.GetByIdAsync(accountId);
        if (account is null)
            return ServiceResult<AccountResponse>.Fail("Account not found.", 404);

        var wallets = await _uow.Wallets.GetByAccountIdAsync(accountId);

        return ServiceResult<AccountResponse>.Success(new AccountResponse
        {
            AccountId = account.Id,
            AccountNumber = account.AccountNumber,
            AccountType = account.AccountType,
            KycTier = account.KycTier,
            KycStatus = account.KycStatus,
            Status = account.Status,
            BusinessName = account.BusinessName,
            RcNumber = account.RcNumber,
            CorporateName = account.CorporateName,
            IncorporationNumber = account.IncorporationNumber,
            Wallets = wallets.Select(w => new WalletResponse
            {
                WalletId = w.Id,
                Currency = w.Currency,
                Balance = w.Balance,
                IsActive = w.IsActive,
                DailyTransactionLimit = w.DailyTransactionLimit
            }).ToList()
        });
    }

    // ─── HELPERS ─────────────────────────────────────────────────────────────

    private static string GenerateAccountNumber()
    {
        // Format: INN + 10 random digits
        var random = new Random();
        var digits = string.Concat(Enumerable.Range(0, 10).Select(_ => random.Next(0, 10).ToString()));
        return $"INN{digits}";
    }
}
