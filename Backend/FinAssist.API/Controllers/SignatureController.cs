using FinAssist.API.Attributes;
using FinAssist.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinAssist.API.Controllers;

[ApiController]
[Route("api/signatures")]
[Authorize]
public class SignatureController(ISignatureService signatureService, ILogger<SignatureController> logger) : ControllerBase
{
    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("{documentId:int}/signer")]
    [RequirePermission("BESOIN_SIGNER")]
    public async Task<IActionResult> Signer(int documentId)
    {
        try { return Ok(await signatureService.SignerAsync(documentId, CurrentUserId)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpPost("besoins/{besoinId:int}/signer")]
    [RequirePermission("BESOIN_SIGNER")]
    public async Task<IActionResult> SignerParBesoin(int besoinId, [FromBody] FinAssist.Core.DTOs.Signature.SignerBesoinDTO? dto)
    {
        try { return Ok(await signatureService.SignerParBesoinAsync(besoinId, CurrentUserId, dto)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpGet("besoins/{besoinId:int}")]
    [RequirePermission("BESOIN_CONSULTER")]
    public async Task<IActionResult> GetByBesoin(int besoinId)
    {
        try { return Ok(await signatureService.GetApercuAsync(besoinId)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpGet("{id:int}/document-signe")]
    [RequirePermission("BESOIN_CONSULTER")]
    public async Task<IActionResult> TelechargerDocumentSigne(int id)
    {
        logger.LogInformation("[FINASSIST][SignatureController] GET document-signe start: signatureId={SignatureId}, userId={UserId}", id, CurrentUserId);
        try
        {
            var (pdfBytes, nomFichier) = await signatureService.GenererDocumentSigneAsync(id);
            logger.LogInformation("[FINASSIST][SignatureController] GET document-signe success: signatureId={SignatureId}, bytes={Bytes}, file={File}",
                id, pdfBytes.Length, nomFichier);
            Response.Headers.Append("Content-Disposition", $"attachment; filename={nomFichier}");
            return File(pdfBytes, "application/pdf");
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex, "[FINASSIST][SignatureController] GET document-signe not-found: signatureId={SignatureId}, error={Error}",
                id, ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "[FINASSIST][SignatureController] GET document-signe bad-request: signatureId={SignatureId}, error={Error}, inner={Inner}",
                id, ex.Message, ex.InnerException?.Message);
            return BadRequest(new { message = ex.Message, detail = ex.InnerException?.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[FINASSIST][SignatureController] GET document-signe unhandled: signatureId={SignatureId}", id);
            return StatusCode(500, new { message = "Erreur interne lors de la génération du document signé." });
        }
    }

    [HttpGet("{id:int}/verifier")]
    [RequirePermission("BESOIN_CONSULTER")]
    public async Task<IActionResult> Verifier(int id)
    {
        try { return Ok(await signatureService.VerifierAsync(id)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }
}
