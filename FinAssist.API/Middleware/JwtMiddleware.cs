using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace FinAssist.API.Middleware;

/// <summary>
/// Middleware optionnel — la validation JWT est déjà gérée par UseAuthentication().
/// Ce middleware enrichit le contexte si nécessaire (ex: logging, claims custom).
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
        var token = context.Request.Headers.Authorization
            .FirstOrDefault()?.Split(" ").Last();

        if (token is not null)
            AttachUserToContext(context, token);

        await _next(context);
    }

    private void AttachUserToContext(HttpContext context, string token)
    {
        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Secret"]!));
            new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _config["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _config["Jwt:Audience"],
                ClockSkew = TimeSpan.Zero
            }, out _);
        }
        catch
        {
            // Token invalide — la requête continuera sans utilisateur authentifié
        }
    }
}
