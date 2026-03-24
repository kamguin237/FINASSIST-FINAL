namespace FinAssist.Core.DTOs.Besoins;

public class CreateBesoinDTO
{
    public string Titre { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string NiveauImportance { get; set; } = string.Empty;
    public int CategorieId { get; set; }
}
