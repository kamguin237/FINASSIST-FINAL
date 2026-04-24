namespace FinAssist.Core.DTOs.Besoins;

public class UpdateBesoinDTO
{
    public string? Titre { get; set; }
    public string? Description { get; set; }
    public string? NiveauImportance { get; set; }
    public int? CategorieId { get; set; }
}
