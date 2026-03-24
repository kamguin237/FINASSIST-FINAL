namespace FinAssist.Core.DTOs.Permissions;

public class PermissionDTO
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Fonctionnalite { get; set; }
    public string? Module { get; set; }
    public DateTime DateCreation { get; set; }
    public DateTime DateModification { get; set; }
}
