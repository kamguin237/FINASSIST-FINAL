namespace FinAssist.Core.DTOs.Reporting;

public class EvolutionPointDTO
{
    public string Date { get; set; } = string.Empty;
    public int Recus { get; set; }
    public int Approuves { get; set; }
    public int Rejetes { get; set; }
    public int EnAttentePlus48h { get; set; }
    public double TauxApprobation { get; set; }
}
