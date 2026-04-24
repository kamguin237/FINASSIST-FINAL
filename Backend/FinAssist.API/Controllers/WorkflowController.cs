using FinAssist.API.Attributes;
using FinAssist.Core.DTOs.Workflow;
using FinAssist.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinAssist.API.Controllers;

[ApiController]
[Route("api/workflow")]
[Authorize]
public class WorkflowController(IWorkflowService workflowService) : ControllerBase
{
    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private string CurrentUserRole =>
        User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    private string CurrentUserNom =>
        User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

    [HttpPost("{id:int}/valider")]
    [RequirePermission("BESOIN_VALIDER")]
    public async Task<IActionResult> Valider(int id, [FromBody] ValiderBesoinDTO dto)
    {
        try { return Ok(await workflowService.ValiderAsync(id, dto, CurrentUserId, CurrentUserRole, CurrentUserNom)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("{id:int}/transmettre")]
    [RequirePermission("BESOIN_VALIDER")]
    public async Task<IActionResult> Transmettre(int id)
    {
        try { return Ok(await workflowService.TransmettreAsync(id, CurrentUserId, CurrentUserNom)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpGet("circuits")]
    [RequirePermission("WORKFLOW_CONSULTER")]
    public async Task<IActionResult> GetCircuits()
        => Ok(await workflowService.GetAllCircuitsAsync());

    [HttpGet("circuits/{id:int}")]
    [RequirePermission("WORKFLOW_CONSULTER")]
    public async Task<IActionResult> GetCircuit(int id)
    {
        try { return Ok(await workflowService.GetCircuitByIdAsync(id)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPost("circuits")]
    [RequirePermission("WORKFLOW_CREER")]
    public async Task<IActionResult> CreateCircuit([FromBody] CreateWorkflowCircuitDTO dto)
    {
        try
        {
            var created = await workflowService.CreateCircuitAsync(dto, CurrentUserNom);
            return CreatedAtAction(nameof(GetCircuit), new { id = created.Id }, created);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("circuits/{id:int}")]
    [RequirePermission("WORKFLOW_MODIFIER")]
    public async Task<IActionResult> UpdateCircuit(int id, [FromBody] UpdateWorkflowCircuitDTO dto)
    {
        try { return Ok(await workflowService.UpdateCircuitAsync(id, dto)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpDelete("circuits/{id:int}")]
    [RequirePermission("WORKFLOW_SUPPRIMER")]
    public async Task<IActionResult> DeleteCircuit(int id)
    {
        try
        {
            await workflowService.DeleteCircuitAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }
}
