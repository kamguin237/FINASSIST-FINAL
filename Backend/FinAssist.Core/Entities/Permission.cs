namespace FinAssist.Core.Entities;

public class Permission
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Fonctionnalite { get; set; }
    public string? Module { get; set; }
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public DateTime DateModification { get; set; } = DateTime.UtcNow;
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
    public ICollection<UtilisateurPermission> UtilisateurPermissions { get; set; } = [];
}
