using FinAssist.API.Attributes;
using FinAssist.Core.DTOs.Permissions;
using FinAssist.Core.DTOs.Roles;
using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinAssist.API.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize]
public class RolesController(IRolesRepository rolesRepo, IPermissionsRepository permissionsRepo) : ControllerBase
{
    [HttpGet]
    [RequirePermission("ROLE_CONSULTER")]
    public async Task<IActionResult> GetAll()
        => Ok((await rolesRepo.GetAllAsync()).Select(ToDTO));

    [HttpGet("{id:int}")]
    [RequirePermission("ROLE_CONSULTER")]
    public async Task<IActionResult> GetById(int id)
    {
        var role = await rolesRepo.GetByIdAsync(id);
        if (role is null) return NotFound(new { message = $"Rôle {id} introuvable." });
        return Ok(ToDTO(role));
    }

    [HttpPost]
    [RequirePermission("ROLE_CREER")]
    public async Task<IActionResult> Create([FromBody] CreateRoleDTO dto)
    {
        if (await rolesRepo.ExistsAsync(dto.Code))
            return Conflict(new { message = $"Un rôle avec le code '{dto.Code}' existe déjà." });
        var role = await rolesRepo.CreateAsync(new Role { Code = dto.Code, Description = dto.Description });
        return CreatedAtAction(nameof(GetById), new { id = role.Id }, ToDTO(role));
    }

    [HttpPut("{id:int}")]
    [RequirePermission("ROLE_MODIFIER")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateRoleDTO dto)
    {
        var role = await rolesRepo.GetByIdAsync(id);
        if (role is null) return NotFound(new { message = $"Rôle {id} introuvable." });
        if (!role.Code.Equals(dto.Code, StringComparison.OrdinalIgnoreCase) && await rolesRepo.ExistsAsync(dto.Code))
            return Conflict(new { message = $"Un rôle avec le code '{dto.Code}' existe déjà." });
        role.Code = dto.Code;
        role.Description = dto.Description;
        role.DateModification = DateTime.UtcNow;
        await rolesRepo.UpdateAsync(role);
        return Ok(ToDTO(role));
    }

    [HttpDelete("{id:int}")]
    [RequirePermission("ROLE_SUPPRIMER")]
    public async Task<IActionResult> Delete(int id)
    {
        var role = await rolesRepo.GetByIdAsync(id);
        if (role is null) return NotFound(new { message = $"Rôle {id} introuvable." });
        await rolesRepo.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("{id:int}/permissions")]
    [RequirePermission("ROLE_CONSULTER")]
    public async Task<IActionResult> GetPermissions(int id)
    {
        var role = await rolesRepo.GetByIdAsync(id);
        if (role is null) return NotFound(new { message = $"Rôle {id} introuvable." });
        return Ok((await permissionsRepo.GetPermissionsByRoleAsync(id)).Select(ToPermissionDTO));
    }

    [HttpPost("{id:int}/permissions")]
    [RequirePermission("ROLE_MODIFIER")]
    public async Task<IActionResult> AddPermissions(int id, [FromBody] AssignerPermissionsDTO dto)
    {
        var role = await rolesRepo.GetByIdAsync(id);
        if (role is null) return NotFound(new { message = $"Rôle {id} introuvable." });
        foreach (var pid in dto.PermissionIds)
            await permissionsRepo.AssignToRoleAsync(pid, id);
        return Ok(new { message = "Permissions attribuées." });
    }

    [HttpPut("{id:int}/permissions")]
    [RequirePermission("ROLE_MODIFIER")]
    public async Task<IActionResult> SetPermissions(int id, [FromBody] AssignerPermissionsDTO dto)
    {
        var role = await rolesRepo.GetByIdAsync(id);
        if (role is null) return NotFound(new { message = $"Rôle {id} introuvable." });
        await permissionsRepo.SetRolePermissionsAsync(id, dto.PermissionIds);
        return Ok(new { message = "Permissions mises à jour." });
    }

    [HttpDelete("{id:int}/permissions/{permissionId:int}")]
    [RequirePermission("ROLE_MODIFIER")]
    public async Task<IActionResult> RemovePermission(int id, int permissionId)
    {
        var role = await rolesRepo.GetByIdAsync(id);
        if (role is null) return NotFound(new { message = $"Rôle {id} introuvable." });
        await permissionsRepo.RemoveFromRoleAsync(permissionId, id);
        return NoContent();
    }

    private static RoleDTO ToDTO(Role r) => new() { Id = r.Id, Code = r.Code, Description = r.Description, DateCreation = r.DateCreation, DateModification = r.DateModification };
    private static PermissionDTO ToPermissionDTO(Permission p) => new() { Id = p.Id, Code = p.Code, Description = p.Description, Fonctionnalite = p.Fonctionnalite, Module = p.Module, DateCreation = p.DateCreation, DateModification = p.DateModification };
}
