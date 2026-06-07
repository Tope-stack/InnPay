using InnPay.Application.Interfaces;
using InnPay.Application.Services;
using InnPay.Domain.Interfaces;
using InnPay.Infrastructure.Persistence;
using InnPay.Infrastructure.Repositories;
using InnPay.Infrastructure.Services;
using InnPay.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InnPay.API.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                config.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsAssembly("InnPay.Infrastructure")));

        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IOnboardingService, OnboardingService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IKycService, KycService>();
        services.AddScoped<IOtpService, OtpService>();
        // Currency module
        services.AddScoped<IWalletService, WalletService>();
        services.AddScoped<IFxRateService, FxRateService>();
        services.AddScoped<IFxConversionService, FxConversionService>();
        services.AddScoped<ICurrencyPairConfigService, CurrencyPairConfigService>();
        // Payment module (6.1 – 6.10)
        services.AddScoped<IPaymentGatewayService, PaymentGatewayService>();
        services.AddScoped<IBillsService, BillsService>();
        services.AddScoped<IInternalTransferService, InternalTransferService>();
        services.AddScoped<IExternalTransferService, ExternalTransferService>();
        services.AddScoped<IVirtualAccountService, VirtualAccountService>();
        services.AddScoped<IWithdrawalService, WithdrawalService>();
        services.AddScoped<IVirtualCardService, VirtualCardService>();
        services.AddScoped<IGiftCardService, GiftCardService>();
        services.AddScoped<IFlightService, FlightService>();
        services.AddScoped<IBetFundingService, BetFundingService>();
        return services;
    }

    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<ISmsService, ConsoleSmsService>();  // swap with TermiiSmsService in prod

        // FX provider — registered as typed HttpClient
        services.AddHttpClient<IFxProviderService, OpenExchangeRatesFxProvider>();

        // Background jobs
        services.AddHostedService<FxRateRefreshJob>();
        services.AddHostedService<TransferScheduleJob>();

        // Payment module providers — mock implementations (swap for real SDK in production)
        // TODO: replace each Mock* with real implementation when API credentials are available
        services.AddScoped<IPaystackProvider, MockPaystackProvider>();
        services.AddScoped<IFlutterwaveProvider, MockFlutterwaveProvider>();
        services.AddScoped<IStripeProvider, MockStripeProvider>();
        services.AddScoped<IBillsProvider, MockBillsProvider>();
        services.AddScoped<IVirtualAccountProvider, MockVirtualAccountProvider>();
        services.AddScoped<ICardIssuerProvider, MockCardIssuerProvider>();
        services.AddScoped<IGiftCardProvider, MockGiftCardProvider>();
        services.AddScoped<IFlightProvider, MockFlightProvider>();
        services.AddScoped<IBettingProvider, MockBettingProvider>();

        services.Configure<ZohoSmtpSettings>(configuration.GetSection("ZohoSmtp"));
        services.AddScoped<IEmailService, ZohoEmailService>();

        return services;
    }

    public static IServiceCollection AddCustomAuth(this IServiceCollection services, IConfiguration config)
    {
        // Register JWT bearer authentication using the built-in ASP.NET Core handler.
        // Token validation is performed here; no custom middleware is needed.
        services.AddAuthentication("Bearer")
            .AddJwtBearer("Bearer", options =>
            {
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                        System.Text.Encoding.UTF8.GetBytes(config["Jwt:Key"]!)),
                    ValidateIssuer   = true,
                    ValidIssuer      = config["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience    = config["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew        = TimeSpan.Zero
                };
            });

        services.AddAuthorization();
        return services;
    }
}
