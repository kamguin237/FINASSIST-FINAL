namespace FinAssist.Core.DTOs.Besoins;

public class DocumentDTO
{
    public int Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Checksum { get; set; } = string.Empty;
    public DateTime DateCreation { get; set; }
}
