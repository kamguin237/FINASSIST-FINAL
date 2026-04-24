using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinAssist.API.Controllers;

[ApiController]
[Route("api/signatures/qr")]
public class QrSignatureController(
    IQrSessionRepository sessionRepo,
    ISignatureUtilisateurRepository sigRepo,
    IUsersService usersService,
    Microsoft.Extensions.Configuration.IConfiguration config) : ControllerBase
{
    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ── PC : créer une session QR ─────────────────────────────────────────────
    [HttpPost("session")]
    [Authorize]
    public async Task<IActionResult> CreateSession()
    {
        var token = Guid.NewGuid().ToString("N"); // token unique 32 chars hex
        var session = new QrSignatureSession
        {
            Token        = token,
            UtilisateurId = CurrentUserId,
            CreeLe       = DateTime.UtcNow,
            Expiration   = DateTime.UtcNow.AddMinutes(10),
            Completed    = false
        };
        await sessionRepo.CreateAsync(session);

        var baseUrl = config["App:BaseUrl"] 
            ?? Request.Scheme + "://" + Request.Host;
        return Ok(new
        {
            token,
            urlMobile = $"{baseUrl}/sign-mobile/{token}",
            expiration = session.Expiration
        });
    }

    // ── PC : polling — vérifier si la signature a été soumise ─────────────────
    [HttpGet("session/{token}/status")]
    [Authorize]
    public async Task<IActionResult> GetStatus(string token)
    {
        var session = await sessionRepo.GetByTokenAsync(token);
        if (session is null) return NotFound(new { message = "Session introuvable." });
        if (session.UtilisateurId != CurrentUserId) return Forbid();
        if (session.Expiration < DateTime.UtcNow && !session.Completed)
            return Ok(new { completed = false, expired = true });

        return Ok(new { completed = session.Completed, expired = false });
    }

    // ── Mobile : récupérer les infos de la session (page publique) ────────────
    [HttpGet("session/{token}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSession(string token)
    {
        var session = await sessionRepo.GetByTokenAsync(token);
        if (session is null) return NotFound(new { message = "Session introuvable ou expirée." });
        if (session.Expiration < DateTime.UtcNow)
            return BadRequest(new { message = "Ce QR Code a expiré. Veuillez en générer un nouveau." });
        if (session.Completed)
            return BadRequest(new { message = "Cette session a déjà été utilisée." });

        return Ok(new
        {
            nom    = session.Utilisateur.Nom,
            prenom = session.Utilisateur.Prenom,
            role   = session.Utilisateur.Role?.Code ?? string.Empty,
            expiration = session.Expiration
        });
    }

    // ── Mobile : soumettre la signature dessinée ──────────────────────────────
    [HttpPost("session/{token}/submit")]
    [AllowAnonymous]
    public async Task<IActionResult> Submit(string token, [FromBody] SubmitSignatureDTO dto)
    {
        var session = await sessionRepo.GetByTokenAsync(token);
        if (session is null) return NotFound(new { message = "Session introuvable." });
        if (session.Expiration < DateTime.UtcNow)
            return BadRequest(new { message = "Session expirée." });
        if (session.Completed)
            return BadRequest(new { message = "Session déjà utilisée." });
        if (string.IsNullOrWhiteSpace(dto.ImageBase64))
            return BadRequest(new { message = "Signature vide." });

        // Sauvegarder comme signature personnelle de l'utilisateur
        var sig = new SignatureUtilisateur
        {
            UtilisateurId    = session.UtilisateurId,
            Type             = "qrcode",
            ImageBase64      = dto.ImageBase64,
            DateCreation     = DateTime.UtcNow,
            DateModification = DateTime.UtcNow
        };
        await sigRepo.SaveAsync(sig);

        // Marquer la session comme complète
        session.Completed    = true;
        session.SignatureBase64 = dto.ImageBase64;
        session.CompletedAt  = DateTime.UtcNow;
        await sessionRepo.UpdateAsync(session);

        return Ok(new { message = "Signature enregistrée avec succès." });
    }
}

public class SubmitSignatureDTO
{
    public string ImageBase64 { get; set; } = string.Empty;
}
