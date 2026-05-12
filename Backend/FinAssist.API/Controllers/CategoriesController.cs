using FinAssist.API.Attributes;
using FinAssist.Core.DTOs.Besoins;
using FinAssist.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinAssist.API.Controllers;

[ApiController]
[Route("api/categories")]
[Authorize]
public class CategoriesController(IBesoinsService besoinsService) : ControllerBase
{
    [HttpGet]
    [RequirePermission("BESOIN_CONSULTER")]
    public async Task<IActionResult> GetAll()
        => Ok(await besoinsService.GetAllCategoriesAsync());

    /// <summary>
    /// Retourne uniquement les catégories dont le circuit de validation
    /// ne contient PAS le rôle de l'utilisateur connecté.
    /// Utilisé dans le formulaire de création de besoin.
    /// </summary>
    [HttpGet("disponibles")]
    [RequirePermission("BESOIN_CREER")]
    public async Task<IActionResult> GetDisponibles()
    {
        var roleCode = User.FindFirstValue(ClaimTypes.Role)
                    ?? User.FindFirstValue("role")
                    ?? string.Empty;

        var toutes = await besoinsService.GetAllCategoriesAsync();

        // Pour filtrer avec les étapes, on a besoin des détails du circuit
        var disponibles = new List<CategorieDTO>();
        foreach (var cat in toutes)
        {
            if (cat.WorkflowCircuitId == null)
            {
                // Pas de circuit → catégorie disponible
                disponibles.Add(cat);
                continue;
            }

            var detail = await besoinsService.GetCategorieByIdAsync(cat.Id);
            var rolesCircuit = detail.Circuit?.Etapes
                .Select(e => e.RoleRequis.Trim().ToUpperInvariant())
                .ToHashSet() ?? [];

            if (!rolesCircuit.Contains(roleCode.Trim().ToUpperInvariant()))
                disponibles.Add(cat);
        }

        return Ok(disponibles);
    }

    [HttpGet("{id:int}")]
    [RequirePermission("BESOIN_CONSULTER")]
    public async Task<IActionResult> GetById(int id)
    {
        try { return Ok(await besoinsService.GetCategorieByIdAsync(id)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPost]
    [RequirePermission("CATEGORIE_CREER")]
    public async Task<IActionResult> Create([FromBody] CreateCategorieDTO dto)
    {
        try
        {
            var created = await besoinsService.CreateCategorieAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpPut("{id:int}")]
    [RequirePermission("CATEGORIE_MODIFIER")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCategorieDTO dto)
    {
        try { return Ok(await besoinsService.UpdateCategorieAsync(id, dto)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpDelete("{id:int}")]
    [RequirePermission("CATEGORIE_SUPPRIMER")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await besoinsService.DeleteCategorieAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }
}
