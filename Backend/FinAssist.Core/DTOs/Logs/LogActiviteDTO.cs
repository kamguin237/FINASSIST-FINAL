namespace FinAssist.Core.DTOs.Logs;

public class LogActiviteDTO
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntiteType { get; set; } = string.Empty;
    public int? EntiteId { get; set; }
    public string? AncienneValeur { get; set; }
    public string? NouvelleValeur { get; set; }
    public int? UtilisateurId { get; set; }
    public DateTime Date { get; set; }
    public string? AdresseIp { get; set; }
    public string? SystemeExploitation { get; set; }
    public string? Navigateur { get; set; }
    public string? Pays { get; set; }
    public string? Ville { get; set; }
}
