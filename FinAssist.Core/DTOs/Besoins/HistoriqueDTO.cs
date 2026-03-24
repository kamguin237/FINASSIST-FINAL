namespace FinAssist.Core.DTOs.Besoins;

public class HistoriqueDTO
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime DateAction { get; set; }
}
