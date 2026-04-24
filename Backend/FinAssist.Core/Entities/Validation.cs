namespace FinAssist.Core.Entities;

public class Validation
{
    public int Id { get; set; }
    public int Niveau { get; set; }
    public DecisionValidation Decision { get; set; } = DecisionValidation.EN_ATTENTE;
    public string? Motif { get; set; }
    public string? Commentaire { get; set; }
    public DateTime DateDecision { get; set; } = DateTime.UtcNow;

    /// <summary>Ordre de l'étape du circuit au moment de la validation</summary>
    public int? EtapeOrdre { get; set; }

    /// <summary>Statut du besoin après cette décision</summary>
    public string? StatutApres { get; set; }

    public int? ValidateurId { get; set; }
    public Utilisateur? Validateur { get; set; }

    public int BesoinId { get; set; }
    public Besoin Besoin { get; set; } = null!;
}
