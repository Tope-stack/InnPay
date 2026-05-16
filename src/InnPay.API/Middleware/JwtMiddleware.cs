using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace InnPay.API.Middleware;

/// <summary>
/// Custom JWT validation middleware — validates the Bearer token on every
/// request that doesn't carry an [AllowAnonymous] marker attribute.
/// Attaches claims to HttpContext.User on success.
/// </summary>
public class JwtMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _config;

    public JwtMiddleware(RequestDelegate next, IConfiguration config)
    {
        _next = next;
        _config = config;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var token = ExtractToken(context);

        if (!string.IsNullOrEmpty(token))
        {
            var principal = ValidateToken(token);
            if (principal is not null)
                context.User = principal;
        }

        await _next(context);
    }

    private static string? ExtractToken(HttpContext context)
    {
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (authHeader is not null && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return authHeader["Bearer ".Length..].Trim();

        return null;
    }

    private System.Security.Claims.ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var key = _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT key not configured.");
            var handler = new JwtSecurityTokenHandler();

            var parameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                ValidateIssuer   = true,
                ValidIssuer      = _config["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience    = _config["Jwt:Audience"],
                ValidateLifetime = true,
                ClockSkew        = TimeSpan.Zero  // strict expiry
            };

            return handler.ValidateToken(token, parameters, out _);
        }
        catch
        {
            return null;   // invalid token — let [Authorize] endpoints reject the request
        }
    }
}
