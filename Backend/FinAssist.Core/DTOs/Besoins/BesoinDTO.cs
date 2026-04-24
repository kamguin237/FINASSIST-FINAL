namespace FinAssist.Core.DTOs.Besoins;

public class BesoinDTO
{
    public int Id { get; set; }
    public string Titre { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Statut { get; set; } = string.Empty;
    public string NiveauImportance { get; set; } = string.Empty;
    public DateTime DateCreation { get; set; }
    public DateTime DateModification { get; set; }
    public int UtilisateurId { get; set; }
    public string UtilisateurNom { get; set; } = string.Empty;
    public int CategorieId { get; set; }
    public string CategorieNom { get; set; } = string.Empty;
    /// <summary>
    /// Vrai si le besoin a atteint la dernière étape du circuit (statut TERMINE).
    /// </summary>
    public bool EstTermine { get; set; }
    public bool DejaValideParMoi { get; set; }
}
