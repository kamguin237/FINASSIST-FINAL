namespace FinAssist.Core.DTOs.Users;

public class UtilisateurDTO
{
    public int Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Prenom { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime DateCreation { get; set; }
    public bool Actif { get; set; }
}
