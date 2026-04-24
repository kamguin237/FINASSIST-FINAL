using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace FinAssist.API.Controllers;

[ApiController]
[Route("api/push")]
[Authorize]
public class PushController(
    IPushSubscriptionRepository repo,
    IConfiguration config) : ControllerBase{
    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Retourne la clé publique VAPID pour le frontend</summary>
    [HttpGet("vapid-public-key")]
    [AllowAnonymous]
    public IActionResult GetPublicKey()
        => Ok(new { publicKey = config["Vapid:PublicKey"] });

    /// <summary>Enregistrer une subscription push</summary>
    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] PushSubscribeDTO dto)
    {
        await repo.SaveAsync(new PushSubscription
        {
            UtilisateurId = CurrentUserId,
            Endpoint      = dto.Endpoint,
            P256dh        = dto.P256dh,
            Auth          = dto.Auth
        });
        return Ok(new { message = "Subscription enregistrée." });
    }

    /// <summary>Supprimer une subscription push</summary>
    [HttpDelete("unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromBody] UnsubscribeDTO dto)
    {
        await repo.DeleteAsync(dto.Endpoint);
        return NoContent();
    }
}

public class PushSubscribeDTO
{
    public string Endpoint { get; set; } = string.Empty;
    public string P256dh   { get; set; } = string.Empty;
    public string Auth     { get; set; } = string.Empty;
}

public class UnsubscribeDTO
{
    public string Endpoint { get; set; } = string.Empty;
}
