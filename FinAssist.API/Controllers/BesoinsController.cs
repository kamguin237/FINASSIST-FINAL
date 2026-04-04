using FinAssist.API.Attributes;
using FinAssist.Core.DTOs.Besoins;
using FinAssist.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinAssist.API.Controllers;

[ApiController]
[Route("api/besoins")]
[Authorize]
public class BesoinsController(IBesoinsService besoinsService) : ControllerBase
{
    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private string CurrentUserRole =>
        User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    [HttpGet]
    [RequirePermission("BESOIN_CONSULTER")]
    public async Task<IActionResult> GetAll()
        => Ok(await besoinsService.GetAllAsync(CurrentUserId, CurrentUserRole));

    [HttpGet("{id:int}")]
    [RequirePermission("BESOIN_CONSULTER")]
    public async Task<IActionResult> GetById(int id)
    {
        try { return Ok(await besoinsService.GetByIdAsync(id, CurrentUserId, CurrentUserRole)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPost]
    [RequirePermission("BESOIN_CREER")]
    public async Task<IActionResult> Create([FromBody] CreateBesoinDTO dto)
    {
        try
        {
            var created = await besoinsService.CreateAsync(dto, CurrentUserId);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (Exception ex) { return StatusCode(500, new { message = ex.Message, detail = ex.InnerException?.Message }); }
    }

    [HttpPut("{id:int}")]
    [RequirePermission("BESOIN_MODIFIER")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateBesoinDTO dto)
    {
        try { return Ok(await besoinsService.UpdateAsync(id, dto, CurrentUserId)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPost("{id:int}/enregistrer")]
    [RequirePermission("BESOIN_MODIFIER")]
    public async Task<IActionResult> Enregistrer(int id)
    {
        try { return Ok(await besoinsService.EnregistrerAsync(id, CurrentUserId)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPost("{id:int}/soumettre")]
    [RequirePermission("BESOIN_MODIFIER")]
    public async Task<IActionResult> Soumettre(int id)
    {
        try { return Ok(await besoinsService.SoumettreAsync(id, CurrentUserId)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpGet("{id:int}/pieces-jointes")]
    [RequirePermission("BESOIN_CONSULTER")]
    public async Task<IActionResult> GetDocuments(int id)
    {
        try { return Ok(await besoinsService.GetDocumentsAsync(id)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpDelete("{id:int}/pieces-jointes/{documentId:int}")]
    [RequirePermission("BESOIN_MODIFIER")]
    public async Task<IActionResult> SupprimerDocument(int id, int documentId)
    {
        try
        {
            await besoinsService.SupprimerDocumentAsync(id, documentId);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpGet("{id:int}/pieces-jointes/{documentId:int}")]
    [RequirePermission("BESOIN_CONSULTER")]
    public async Task<IActionResult> GetDocumentContenu(int id, int documentId)
    {
        try
        {
            var (contenu, nom, type) = await besoinsService.GetDocumentContenuAsync(id, documentId);
            var mimeType = string.IsNullOrEmpty(type) ? "application/pdf" : type;
            Response.Headers.Append("Content-Disposition", $"inline; filename=\"{Uri.EscapeDataString(nom)}\"");
            return File(contenu, mimeType);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
    }

    [HttpPost("{id:int}/pieces-jointes")]
    [RequirePermission("BESOIN_MODIFIER")]
    public async Task<IActionResult> AjouterPieceJointe(int id, IFormFile fichier)
    {
        if (fichier is null || fichier.Length == 0)
            return BadRequest(new { message = "Fichier manquant ou vide." });

        using var ms = new MemoryStream();
        await fichier.CopyToAsync(ms);
        var contenu = ms.ToArray();

        try
        {
            var doc = await besoinsService.AjouterPieceJointeAsync(id, fichier.FileName, fichier.ContentType, contenu);
            return Ok(doc);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpGet("{id:int}/historique")]
    [RequirePermission("BESOIN_CONSULTER")]
    public async Task<IActionResult> GetHistorique(int id)
    {
        try { return Ok(await besoinsService.GetHistoriqueAsync(id)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpDelete("{id:int}")]
    [RequirePermission("BESOIN_SUPPRIMER")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await besoinsService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }
}
