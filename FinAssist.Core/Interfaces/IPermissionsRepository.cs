using FinAssist.Core.Entities;

namespace FinAssist.Core.Interfaces;

public interface IPermissionsRepository
{
    Task<IEnumerable<Permission>> GetAllAsync();
    Task<Permission?> GetByIdAsync(int id);
    Task<bool> ExistsAsync(string code);
    Task<Permission> CreateAsync(Permission permission);
    Task<Permission> UpdateAsync(Permission permission);
    Task DeleteAsync(int id);

    // Attribution rôle ↔ permission
    Task AssignToRoleAsync(int permissionId, int roleId);
    Task RemoveFromRoleAsync(int permissionId, int roleId);
    Task SetRolePermissionsAsync(int roleId, IEnumerable<int> permissionIds);
    Task<IEnumerable<Permission>> GetPermissionsByRoleAsync(int roleId);

    // Attribution utilisateur ↔ permission
    Task AssignToUtilisateurAsync(int permissionId, int utilisateurId);
    Task RemoveFromUtilisateurAsync(int permissionId, int utilisateurId);
    Task SetUtilisateurPermissionsAsync(int utilisateurId, IEnumerable<int> permissionIds);
    Task<IEnumerable<Permission>> GetPermissionsDirectesAsync(int utilisateurId);

    // Permissions effectives (rôle + directes)
    Task<IEnumerable<string>> GetPermissionsEffectivesAsync(int utilisateurId);
}
