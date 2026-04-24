namespace FinAssist.Core.DTOs.Reporting;

public class RapportDTO
{
    public string Type { get; set; } = string.Empty;
    public DateTime DateGeneration { get; set; } = DateTime.UtcNow;
    public object Contenu { get; set; } = new();
    public int GenerateurId { get; set; }
    public Dictionary<string, string> Parametres { get; set; } = [];
}
