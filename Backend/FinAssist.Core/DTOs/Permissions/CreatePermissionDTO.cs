namespace FinAssist.Core.DTOs.Permissions;

public class CreatePermissionDTO
{
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Fonctionnalite { get; set; }
    public string? Module { get; set; }
}
