namespace FinAssist.Core.Entities;

public class Historique
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime DateAction { get; set; } = DateTime.UtcNow;

    public int BesoinId { get; set; }
    public Besoin Besoin { get; set; } = null!;
}
