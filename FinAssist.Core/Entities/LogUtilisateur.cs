namespace FinAssist.Core.Entities;

public class LogUtilisateur
{
    public int Id { get; set; }
    public int UtilisateurId { get; set; }
    public Utilisateur Utilisateur { get; set; } = null!;
    public string Action { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime DateAction { get; set; } = DateTime.UtcNow;
}
