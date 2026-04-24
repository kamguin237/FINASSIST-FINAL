using System.Security.Claims;
using FinAssist.Core.DTOs.Auth;
using FinAssist.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinAssist.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogService _logService;
    private readonly IUserAgentParser _uaParser;
    private readonly IGeoIpService _geoIpService;

    public AuthController(IAuthService authService, ILogService logService, IUserAgentParser uaParser, IGeoIpService geoIpService)
    {
        _authService = authService;
        _logService = logService;
        _uaParser = uaParser;
        _geoIpService = geoIpService;
    }

    /// <summary>Connexion — retourne un JWT valide 15 minutes.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDTO request)
    {
        try
        {
            var result = await _authService.LoginAsync(request);

            var ip = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                ?? HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Inconnu";
            if (ip.Contains(',')) ip = ip.Split(',')[0].Trim();

            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
            var (os, navigateur) = _uaParser.Parse(userAgent);

            // Priorité au header X-Client-OS si présent
            var clientOs = HttpContext.Request.Headers["X-Client-OS"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(clientOs)) os = clientOs;

            var (pays, ville) = ("Inconnu", "Inconnu");
            try { (pays, ville) = await _geoIpService.GetLocalisationAsync(ip); } catch { }

            await _logService.LoggerAsync(
                action:              "POST /api/auth/login",
                entiteType:          "auth",
                utilisateurId:       result.Utilisateur.Id,
                adresseIp:           ip,
                systemeExploitation: os,
                navigateur:          navigateur,
                pays:                pays,
                ville:               ville);

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>Déconnexion — le frontend doit supprimer le token localement.</summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                       ?? User.FindFirst("sub");

        if (userIdClaim is null || !int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized();

        await _authService.LogoutAsync(userId);
        return NoContent();
    }
}
