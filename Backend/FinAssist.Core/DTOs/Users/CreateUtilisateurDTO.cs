namespace FinAssist.Core.DTOs.Users;

public class CreateUtilisateurDTO
{
    public string Nom { get; set; } = string.Empty;
    public string Prenom { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public List<int>? PermissionsSupplementaires { get; set; }
}
