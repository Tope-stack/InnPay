# InnPay API — .NET 8 Backend

Clean Architecture implementation of the InnPay onboarding and KYC system.

## Project Structure

```
InnPay/
├── src/
│   ├── InnPay.Domain/              # Entities, enums, repository interfaces
│   │   ├── Entities/               # User, Account, Wallet, KycDocument, etc.
│   │   ├── Enums/                  # AccountType, KycTier, WalletCurrency, etc.
│   │   └── Interfaces/             # IUserRepository, IUnitOfWork, etc.
│   │
│   ├── InnPay.Application/         # Business logic (no framework dependencies)
│   │   ├── Common/                 # ServiceResult<T> wrapper
│   │   ├── DTOs/Request/           # RegisterPersonalRequest, UploadKycDocumentRequest, etc.
│   │   ├── DTOs/Response/          # AuthResponse, AccountResponse, KycStatusResponse, etc.
│   │   ├── Interfaces/             # IOnboardingService, IAuthService, IKycService, etc.
│   │   └── Services/               # OnboardingService, AuthService, KycService
│   │
│   ├── InnPay.Infrastructure/      # EF Core, PostgreSQL, JWT, file storage
│   │   ├── Persistence/            # AppDbContext, UnitOfWork, EF Configurations
│   │   ├── Repositories/           # Concrete repository implementations
│   │   └── Services/               # PasswordHasher, TokenService, OtpService, FileStorage
│   │
│   └── InnPay.API/                 # ASP.NET Core Web API
│       ├── Controllers/            # OnboardingController, AuthController, KycController
│       ├── Middleware/             # ExceptionMiddleware, JwtMiddleware
│       └── Extensions/            # ServiceExtensions (DI registration)
```

## Prerequisites

- .NET 8 SDK
- PostgreSQL 14+

## Setup

1. **Clone and configure**
   ```bash
   git clone <repo>
   cd InnPay
   ```

2. **Update connection string** in `src/InnPay.API/appsettings.Development.json`:
   ```json
   "DefaultConnection": "Host=localhost;Port=5432;Database=innpay_dev;Username=postgres;Password=yourpassword"
   ```

3. **Update JWT secret** in appsettings (use a strong 256-bit key in production):
   ```json
   "Jwt": { "Key": "your-super-secret-key-minimum-32-characters" }
   ```

4. **Run migrations**
   ```bash
   dotnet tool install --global dotnet-ef
   dotnet ef migrations add InitialCreate \
     --project src/InnPay.Infrastructure \
     --startup-project src/InnPay.API \
     --output-dir Persistence/Migrations
   dotnet ef database update \
     --project src/InnPay.Infrastructure \
     --startup-project src/InnPay.API
   ```

5. **Run the API**
   ```bash
   dotnet run --project src/InnPay.API
   ```

6. **Open Swagger UI**: `http://localhost:5000/swagger`

## API Endpoints

### Onboarding (Public)
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/v1/onboarding/personal/register` | Register personal account |
| POST | `/api/v1/onboarding/business/register` | Register business account |
| POST | `/api/v1/onboarding/corporate/register` | Initiate corporate registration |
| POST | `/api/v1/onboarding/verify-otp` | Verify phone OTP |
| POST | `/api/v1/onboarding/resend-otp` | Resend OTP |
| POST | `/api/v1/onboarding/set-pin` | Set 6-digit transaction PIN |
| GET  | `/api/v1/onboarding/account/{id}` | Get account details (auth required) |

### Auth (Public)
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/v1/auth/login` | Login → JWT + refresh token |
| POST | `/api/v1/auth/refresh` | Rotate refresh token |
| POST | `/api/v1/auth/logout` | Revoke all tokens (auth required) |
| POST | `/api/v1/auth/forgot-password` | Request password reset OTP |
| POST | `/api/v1/auth/reset-password` | Reset password with OTP |

### KYC (Auth required)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET  | `/api/v1/kyc/{accountId}/status` | Get KYC status + documents |
| POST | `/api/v1/kyc/personal/tier1` | Submit personal Tier 1 (gov ID) |
| POST | `/api/v1/kyc/personal/tier2` | Submit personal Tier 2 (BVN + selfie + PoA) |
| POST | `/api/v1/kyc/business` | Submit business KYC (CAC + utility + director ID) |
| POST | `/api/v1/kyc/corporate` | Submit corporate KYC (all docs + UBOs) |
| POST | `/api/v1/kyc/review` | [Admin] Approve/reject a KYC document |

## Onboarding Flow

```
Personal:   Register → OTP → Set PIN → [optional: KYC Tier 1] → [KYC Tier 2 for FX wallets]
Business:   Register → OTP → Set PIN → Submit Business KYC → Admin review → Activated
Corporate:  Register → OTP → Submit Corporate KYC → Compliance review (3 days) → Activated
```

## KYC Tier Upgrade Behaviour

When an admin approves the final document for an account, the system automatically:
- **Personal Tier 1**: Upgrades NGN daily limit to ₦500k
- **Personal Tier 2**: Upgrades NGN limit to ₦5M + activates USD/EUR/GBP wallets
- **Business Verified**: Sets NGN limit to ₦10M + activates FX wallets + activates account
- **Corporate Verified**: Custom limits + activates all wallets + activates account

## Production Checklist

- [ ] Replace `ConsoleSmsService` with Termii or Twilio integration
- [ ] Replace `LocalFileStorageService` with AWS S3 or Azure Blob Storage
- [ ] Replace BVN stub in `KycService.VerifyBvnExternalAsync` with Mono/Okra/NIBSS
- [ ] Set strong JWT secret via environment variables / Azure Key Vault
- [ ] Use `dotnet ef database update` explicitly in CI/CD (remove auto-migrate in prod)
- [ ] Add rate limiting middleware (Microsoft.AspNetCore.RateLimiting)
- [ ] Add structured logging (Serilog + Seq or ELK)
