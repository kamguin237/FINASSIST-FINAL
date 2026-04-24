namespace FinAssist.Core.DTOs.Workflow;

public class ValidationDTO
{
    public int Id { get; set; }
    public int Niveau { get; set; }
    public string Decision { get; set; } = string.Empty;
    public string? Motif { get; set; }
    public string? Commentaire { get; set; }
    public DateTime DateDecision { get; set; }
    public int? ValidateurId { get; set; }
    public string ValidateurNom { get; set; } = string.Empty;
    public int BesoinId { get; set; }
}
