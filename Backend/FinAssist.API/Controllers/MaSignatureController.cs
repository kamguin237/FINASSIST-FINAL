using FinAssist.API.Attributes;
using FinAssist.Core.DTOs.Signatures;
using FinAssist.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinAssist.API.Controllers;

[ApiController]
[Route("api/ma-signature")]
[Authorize]
public class MaSignatureController(ISignatureUtilisateurService service) : ControllerBase
{
    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    [RequirePermission("SIGNATURE_PERSO_GERER")]
    public async Task<IActionResult> Get()
    {
        var sig = await service.GetMaSignatureAsync(CurrentUserId);
        return sig is null ? NoContent() : Ok(sig);
    }

    [HttpPost]
    [RequirePermission("SIGNATURE_PERSO_GERER")]
    public async Task<IActionResult> Save([FromBody] SaveSignatureUtilisateurDTO dto)
    {
        try
        {
            var saved = await service.SauvegarderAsync(CurrentUserId, dto);
            return Ok(saved);
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete]
    [RequirePermission("SIGNATURE_PERSO_GERER")]
    public async Task<IActionResult> Delete()
    {
        await service.SupprimerAsync(CurrentUserId);
        return NoContent();
    }
}
