using InnPay.Application.Interfaces;
using InnPay.Application.Services;
using InnPay.Domain.Interfaces;
using InnPay.Infrastructure.Persistence;
using InnPay.Infrastructure.Repositories;
using InnPay.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

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
        return services;
    }

    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<ISmsService, ConsoleSmsService>();  // swap with TermiiSmsService in prod
        return services;
    }

    public static IServiceCollection AddCustomAuth(this IServiceCollection services, IConfiguration config)
    {
        // Register JWT bearer authentication using the built-in ASP.NET Core
        // JWT handler — but our custom JwtMiddleware handles the validation, so
        // here we just configure the scheme used by [Authorize].
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
