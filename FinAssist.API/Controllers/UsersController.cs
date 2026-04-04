using FinAssist.API.Attributes;
using FinAssist.Core.DTOs.Permissions;
using FinAssist.Core.DTOs.Roles;
using FinAssist.Core.DTOs.Users;
using FinAssist.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinAssist.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController(IUsersService usersService, IPermissionsRepository permissionsRepo, IPermissionService permissionService) : ControllerBase
{
    [HttpGet]
    [RequirePermission("USER_CONSULTER")]
    public async Task<IActionResult> GetAll()
        => Ok(await usersService.GetAllAsync());

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var idClaim = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (!int.TryParse(idClaim, out var id)) return Unauthorized();
        try { return Ok(await usersService.GetByIdAsync(id)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPut("me/password")]
    public async Task<IActionResult> ChangePassword([FromBody] FinAssist.Core.DTOs.Users.ChangePasswordDTO dto)
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idClaim, out var id)) return Unauthorized();
        if (string.IsNullOrWhiteSpace(dto.NouveauMotDePasse) || dto.NouveauMotDePasse.Length < 8)
            return BadRequest(new { message = "Le nouveau mot de passe doit contenir au moins 8 caractères." });
        try
        {
            await usersService.ChangePasswordAsync(id, dto.AncienMotDePasse, dto.NouveauMotDePasse);
            return Ok(new { message = "Mot de passe modifié avec succès." });
        }
        catch (UnauthorizedAccessException ex) { return BadRequest(new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpGet("{id:int}")]
    [RequirePermission("USER_CONSULTER")]
    public async Task<IActionResult> GetById(int id)
    {
        try { return Ok(await usersService.GetByIdAsync(id)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPost]
    [RequirePermission("USER_CREER")]
    public async Task<IActionResult> Create([FromBody] CreateUtilisateurDTO dto)
    {
        try
        {
            var created = await usersService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpPut("{id:int}")]
    [RequirePermission("USER_MODIFIER")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUtilisateurDTO dto)
    {
        try { return Ok(await usersService.UpdateAsync(id, dto)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpDelete("{id:int}")]
    [RequirePermission("USER_SUPPRIMER")]
    public async Task<IActionResult> Deactivate(int id)
    {
        try { await usersService.DeactivateAsync(id); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpPatch("{id:int}/activer")]
    [RequirePermission("USER_MODIFIER")]
    public async Task<IActionResult> Activate(int id)
    {
        try { await usersService.ActivateAsync(id); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpDelete("{id:int}/supprimer")]
    [RequirePermission("USER_SUPPRIMER")]
    public async Task<IActionResult> Delete(int id)
    {
        try { await usersService.DeleteAsync(id); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPut("{id:int}/role")]
    [RequirePermission("USER_MODIFIER")]
    public async Task<IActionResult> ChangeRole(int id, [FromBody] ChangeRoleDTO dto)
    {
        try { return Ok(await usersService.ChangeRoleAsync(id, dto.RoleId)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpGet("{id:int}/logs")]
    [RequirePermission("USER_CONSULTER")]
    public async Task<IActionResult> GetLogs(int id)
    {
        try { return Ok(await usersService.GetLogsAsync(id)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpGet("{id:int}/permissions")]
    [RequirePermission("USER_CONSULTER")]
    public async Task<IActionResult> GetPermissions(int id)
    {
        try { _ = await usersService.GetByIdAsync(id); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }

        var directes = (await permissionsRepo.GetPermissionsDirectesAsync(id)).Select(p => p.Code).ToList();
        var effectives = (await permissionsRepo.GetPermissionsEffectivesAsync(id)).ToList();
        var role = effectives.Except(directes).ToList();

        return Ok(new PermissionsEffectivesDTO
        {
            PermissionsRole = role,
            PermissionsDirectes = directes,
            PermissionsEffectives = effectives
        });
    }

    [HttpPost("{id:int}/permissions")]
    [RequirePermission("USER_MODIFIER")]
    public async Task<IActionResult> AddPermissions(int id, [FromBody] AssignerPermissionsDTO dto)
    {
        try { _ = await usersService.GetByIdAsync(id); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }

        foreach (var pid in dto.PermissionIds)
            await permissionsRepo.AssignToUtilisateurAsync(pid, id);

        permissionService.InvalidateCache(id);
        return Ok(new { message = "Permissions attribuées." });
    }

    [HttpPut("{id:int}/permissions")]
    [RequirePermission("USER_MODIFIER")]
    public async Task<IActionResult> SetPermissions(int id, [FromBody] AssignerPermissionsDTO dto)
    {
        try { _ = await usersService.GetByIdAsync(id); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }

        await permissionsRepo.SetUtilisateurPermissionsAsync(id, dto.PermissionIds);
        permissionService.InvalidateCache(id);
        return Ok(new { message = "Permissions mises à jour." });
    }

    [HttpDelete("{id:int}/permissions/{permissionId:int}")]
    [RequirePermission("USER_MODIFIER")]
    public async Task<IActionResult> RemovePermission(int id, int permissionId)
    {
        try { _ = await usersService.GetByIdAsync(id); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }

        await permissionsRepo.RemoveFromUtilisateurAsync(permissionId, id);
        permissionService.InvalidateCache(id);
        return NoContent();
    }
}
