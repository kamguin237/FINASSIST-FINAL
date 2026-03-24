namespace FinAssist.Core.Entities;

public class Document
{
    public int Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public byte[] Contenu { get; set; } = [];
    public string Checksum { get; set; } = string.Empty;
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;

    public int BesoinId { get; set; }
    public Besoin Besoin { get; set; } = null!;
}
