namespace FinAssist.Core.Interfaces;

public interface IPermissionService
{
    Task<bool> HasPermissionAsync(int utilisateurId, string permissionCode);
    Task<IEnumerable<string>> GetPermissionsEffectivesAsync(int utilisateurId);
    void InvalidateCache(int utilisateurId);
}
