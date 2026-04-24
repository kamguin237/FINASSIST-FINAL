using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;
using FinAssist.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinAssist.Infrastructure.Repositories;

public class PermissionsRepository(AppDbContext db) : IPermissionsRepository
{
    public async Task<IEnumerable<Permission>> GetAllAsync()
        => await db.Permissions.OrderBy(p => p.Module).ThenBy(p => p.Code).ToListAsync();

    public Task<Permission?> GetByIdAsync(int id)
        => db.Permissions.FirstOrDefaultAsync(p => p.Id == id);

    public Task<bool> ExistsAsync(string code)
        => db.Permissions.AnyAsync(p => p.Code.ToLower() == code.ToLower());

    public async Task<Permission> CreateAsync(Permission permission)
    {
        db.Permissions.Add(permission);
        await db.SaveChangesAsync();
        return permission;
    }

    public async Task<Permission> UpdateAsync(Permission permission)
    {
        db.Permissions.Update(permission);
        await db.SaveChangesAsync();
        return permission;
    }

    public async Task DeleteAsync(int id)
        => await db.Permissions.Where(p => p.Id == id).ExecuteDeleteAsync();

    // ── Rôle ↔ Permission ────────────────────────────────────────────────────

    public async Task AssignToRoleAsync(int permissionId, int roleId)
    {
        if (!await db.RolePermissions.AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId))
        {
            db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permissionId });
            await db.SaveChangesAsync();
        }
    }

    public async Task RemoveFromRoleAsync(int permissionId, int roleId)
        => await db.RolePermissions
            .Where(rp => rp.RoleId == roleId && rp.PermissionId == permissionId)
            .ExecuteDeleteAsync();

    public async Task SetRolePermissionsAsync(int roleId, IEnumerable<int> permissionIds)
    {
        await db.RolePermissions.Where(rp => rp.RoleId == roleId).ExecuteDeleteAsync();
        var ids = permissionIds.Distinct().ToList();
        if (ids.Count > 0)
        {
            db.RolePermissions.AddRange(ids.Select(pid => new RolePermission { RoleId = roleId, PermissionId = pid }));
            await db.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Permission>> GetPermissionsByRoleAsync(int roleId)
        => await db.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.Permission)
            .OrderBy(p => p.Module).ThenBy(p => p.Code)
            .ToListAsync();

    // ── Utilisateur ↔ Permission ─────────────────────────────────────────────

    public async Task AssignToUtilisateurAsync(int permissionId, int utilisateurId)
    {
        if (!await db.UtilisateurPermissions.AnyAsync(up => up.UtilisateurId == utilisateurId && up.PermissionId == permissionId))
        {
            db.UtilisateurPermissions.Add(new UtilisateurPermission { UtilisateurId = utilisateurId, PermissionId = permissionId });
            await db.SaveChangesAsync();
        }
    }

    public async Task RemoveFromUtilisateurAsync(int permissionId, int utilisateurId)
        => await db.UtilisateurPermissions
            .Where(up => up.UtilisateurId == utilisateurId && up.PermissionId == permissionId)
            .ExecuteDeleteAsync();

    public async Task SetUtilisateurPermissionsAsync(int utilisateurId, IEnumerable<int> permissionIds)
    {
        await db.UtilisateurPermissions.Where(up => up.UtilisateurId == utilisateurId).ExecuteDeleteAsync();
        var ids = permissionIds.Distinct().ToList();
        if (ids.Count > 0)
        {
            db.UtilisateurPermissions.AddRange(ids.Select(pid => new UtilisateurPermission { UtilisateurId = utilisateurId, PermissionId = pid }));
            await db.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Permission>> GetPermissionsDirectesAsync(int utilisateurId)
        => await db.UtilisateurPermissions
            .Where(up => up.UtilisateurId == utilisateurId)
            .Select(up => up.Permission)
            .OrderBy(p => p.Module).ThenBy(p => p.Code)
            .ToListAsync();

    public async Task<IEnumerable<string>> GetPermissionsEffectivesAsync(int utilisateurId)
    {
        var user = await db.Utilisateurs
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == utilisateurId);

        if (user is null) return [];

        var permissionsRole = await db.RolePermissions
            .Where(rp => rp.RoleId == user.RoleId)
            .Select(rp => rp.Permission.Code)
            .ToListAsync();

        var permissionsDirectes = await db.UtilisateurPermissions
            .Where(up => up.UtilisateurId == utilisateurId)
            .Select(up => up.Permission.Code)
            .ToListAsync();

        return permissionsRole.Union(permissionsDirectes).Distinct().ToList();
    }
}
