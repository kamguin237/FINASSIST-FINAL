using FinAssist.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace FinAssist.Application.Services;

public class PermissionService(IPermissionsRepository permissionsRepo, IMemoryCache cache) : IPermissionService
{
    private static string CacheKey(int userId) => $"permissions_{userId}";

    public async Task<IEnumerable<string>> GetPermissionsEffectivesAsync(int utilisateurId)
    {
        var key = CacheKey(utilisateurId);
        if (cache.TryGetValue(key, out IEnumerable<string>? cached) && cached is not null)
            return cached;

        var permissions = (await permissionsRepo.GetPermissionsEffectivesAsync(utilisateurId)).ToList();
        cache.Set(key, permissions, TimeSpan.FromMinutes(10));
        return permissions;
    }

    public async Task<bool> HasPermissionAsync(int utilisateurId, string permissionCode)
    {
        var permissions = await GetPermissionsEffectivesAsync(utilisateurId);
        return permissions.Contains(permissionCode, StringComparer.OrdinalIgnoreCase);
    }

    public void InvalidateCache(int utilisateurId)
        => cache.Remove(CacheKey(utilisateurId));
}
