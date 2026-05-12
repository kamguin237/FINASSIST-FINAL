namespace FinAssist.Core.DTOs.Logs;

public class FiltreLogsDTO
{
    public string? Action { get; set; }
    public string? EntiteType { get; set; }
    public int? UtilisateurId { get; set; }
    public DateTime? DateDebut { get; set; }
    public DateTime? DateFin { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
