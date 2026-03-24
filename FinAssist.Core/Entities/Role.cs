namespace FinAssist.Core.Entities;

public class Role
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public DateTime DateModification { get; set; } = DateTime.UtcNow;
    public ICollection<Utilisateur> Utilisateurs { get; set; } = [];
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
