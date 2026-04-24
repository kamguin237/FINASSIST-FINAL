namespace FinAssist.Core.Entities;

public class Besoin
{
    public int Id { get; set; }
    public string Titre { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Statut { get; set; } = "BROUILLON";
    public int? EtapeCouranteOrdre { get; set; }
    public string NiveauImportance { get; set; } = string.Empty;
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public DateTime DateModification { get; set; } = DateTime.UtcNow;

    // ── Deadline tracking ─────────────────────────────────────────────────────
    public DateTime? DateEntreeEnAttente { get; set; }
    public bool Rappel1Envoye { get; set; } = false;
    public bool Rappel2Envoye { get; set; } = false;
    public bool EmailRappelEnvoye { get; set; } = false;
    public bool RejeteAutomatiquement { get; set; } = false;

    public int UtilisateurId { get; set; }
    public Utilisateur Utilisateur { get; set; } = null!;

    public int CategorieId { get; set; }
    public Categorie Categorie { get; set; } = null!;

    public ICollection<Document> Documents { get; set; } = [];
    public ICollection<Historique> Historiques { get; set; } = [];
}
