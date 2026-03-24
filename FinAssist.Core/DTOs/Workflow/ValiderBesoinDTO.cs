namespace FinAssist.Core.DTOs.Workflow;

public class ValiderBesoinDTO
{
    /// <summary>APPROUVE ou REJETE</summary>
    public string Decision { get; set; } = string.Empty;
    public string? Motif { get; set; }
    public string? Commentaire { get; set; }
}
