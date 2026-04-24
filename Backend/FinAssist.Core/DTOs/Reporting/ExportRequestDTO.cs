namespace FinAssist.Core.DTOs.Reporting;

public class ExportRequestDTO
{
    /// <summary>PDF ou EXCEL</summary>
    public string Format { get; set; } = "EXCEL";
    public FiltreRapportDTO? Filtres { get; set; }
}
