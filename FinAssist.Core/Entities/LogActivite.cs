namespace FinAssist.Core.Entities;

public class LogActivite
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntiteType { get; set; } = string.Empty;
    public int? EntiteId { get; set; }
    public string? AncienneValeur { get; set; }
    public string? NouvelleValeur { get; set; }
    public int? UtilisateurId { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;

    // Navigation
    public Utilisateur? Utilisateur { get; set; }
}
