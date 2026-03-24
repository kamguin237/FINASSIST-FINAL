namespace FinAssist.Core.Entities;

public class Utilisateur
{
    public int Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Prenom { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string MotDePasse { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public DateTime DateModification { get; set; } = DateTime.UtcNow;
    public bool Actif { get; set; } = true;
    public ICollection<UtilisateurPermission> UtilisateurPermissions { get; set; } = [];
    public ICollection<LogUtilisateur> Logs { get; set; } = [];
}
