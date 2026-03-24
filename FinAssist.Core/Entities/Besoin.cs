namespace FinAssist.Core.Entities;

public class Besoin
{
    public int Id { get; set; }
    public string Titre { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    /// <summary>Statut dynamique du besoin (ex: BROUILLON, EN_ATTENTE, APPROUVE_ROLE1, SIGNE_ROLE2...)</summary>
    public string Statut { get; set; } = "BROUILLON";

    /// <summary>Ordre de l'étape courante dans le circuit (null si pas encore en validation)</summary>
    public int? EtapeCouranteOrdre { get; set; }
    public string NiveauImportance { get; set; } = string.Empty;
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public DateTime DateModification { get; set; } = DateTime.UtcNow;

    public int UtilisateurId { get; set; }
    public Utilisateur Utilisateur { get; set; } = null!;

    public int CategorieId { get; set; }
    public Categorie Categorie { get; set; } = null!;

    public ICollection<Document> Documents { get; set; } = [];
    public ICollection<Historique> Historiques { get; set; } = [];
}
