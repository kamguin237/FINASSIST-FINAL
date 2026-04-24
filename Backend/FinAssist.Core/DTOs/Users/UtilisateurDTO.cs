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
    public string? EmailWarning { get; set; }
    /// <summary>True si l'utilisateur a déjà effectué des actions (besoins, validations, signatures…). Ne peut être que désactivé, pas supprimé.</summary>
    public bool HasActions { get; set; }
}
