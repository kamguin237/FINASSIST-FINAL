namespace FinAssist.Core.DTOs.Permissions;

public class PermissionsEffectivesDTO
{
    public IEnumerable<string> PermissionsRole { get; set; } = [];
    public IEnumerable<string> PermissionsDirectes { get; set; } = [];
    public IEnumerable<string> PermissionsEffectives { get; set; } = [];
}
