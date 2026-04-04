using FinAssist.API.Attributes;
using FinAssist.Core.DTOs.Besoins;
using FinAssist.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinAssist.API.Controllers;

[ApiController]
[Route("api/categories")]
[Authorize]
public class CategoriesController(IBesoinsService besoinsService) : ControllerBase
{
    [HttpGet]
    [RequirePermission("CATEGORIE_CONSULTER")]
    public async Task<IActionResult> GetAll()
        => Ok(await besoinsService.GetAllCategoriesAsync());

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
