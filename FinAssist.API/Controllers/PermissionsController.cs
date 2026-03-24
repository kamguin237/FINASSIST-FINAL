using FinAssist.API.Attributes;
using FinAssist.Core.DTOs.Permissions;
using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinAssist.API.Controllers;

[ApiController]
[Route("api/permissions")]
[Authorize]
public class PermissionsController(IPermissionsRepository permissionsRepo) : ControllerBase
{
    [HttpGet]
    [RequirePermission("PERMISSION_CONSULTER")]
    public async Task<IActionResult> GetAll()
        => Ok((await permissionsRepo.GetAllAsync()).Select(ToDTO));

    [HttpGet("{id:int}")]
    [RequirePermission("PERMISSION_CONSULTER")]
    public async Task<IActionResult> GetById(int id)
    {
        var p = await permissionsRepo.GetByIdAsync(id);
        if (p is null) return NotFound(new { message = $"Permission {id} introuvable." });
        return Ok(ToDTO(p));
    }

    [HttpPut("{id:int}")]
    [RequirePermission("PERMISSION_MODIFIER")]
    public async Task<IActionResult> Update(int id, [FromBody] CreatePermissionDTO dto)
    {
        var permission = await permissionsRepo.GetByIdAsync(id);
        if (permission is null) return NotFound(new { message = $"Permission {id} introuvable." });
        if (!permission.Code.Equals(dto.Code, StringComparison.OrdinalIgnoreCase) && await permissionsRepo.ExistsAsync(dto.Code))
            return Conflict(new { message = $"Une permission avec le code '{dto.Code}' existe déjà." });
        permission.Code = dto.Code;
        permission.Description = dto.Description;
        permission.Fonctionnalite = dto.Fonctionnalite;
        permission.Module = dto.Module;
        permission.DateModification = DateTime.UtcNow;
        await permissionsRepo.UpdateAsync(permission);
        return Ok(ToDTO(permission));
    }

    private static PermissionDTO ToDTO(Permission p) => new() { Id = p.Id, Code = p.Code, Description = p.Description, Fonctionnalite = p.Fonctionnalite, Module = p.Module, DateCreation = p.DateCreation, DateModification = p.DateModification };
}
