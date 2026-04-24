namespace FinAssist.Core.DTOs.Users;

public class UpdateUtilisateurDTO
{
    public string? Nom { get; set; }
    public string? Prenom { get; set; }
    public string? Email { get; set; }
    public int? RoleId { get; set; }
    public bool? Actif { get; set; }
}
