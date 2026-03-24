using FinAssist.API.Attributes;
using FinAssist.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinAssist.API.Controllers;

[ApiController]
[Route("api/signatures")]
[Authorize]
public class SignatureController(ISignatureService signatureService) : ControllerBase
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
    public async Task<IActionResult> SignerParBesoin(int besoinId)
    {
        try { return Ok(await signatureService.SignerParBesoinAsync(besoinId, CurrentUserId)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpGet("{id:int}/verifier")]
    [RequirePermission("BESOIN_CONSULTER")]
    public async Task<IActionResult> Verifier(int id)
    {
        try { return Ok(await signatureService.VerifierAsync(id)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }
}
