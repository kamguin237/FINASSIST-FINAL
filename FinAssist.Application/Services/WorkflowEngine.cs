using FinAssist.Core.Entities;

namespace FinAssist.Application.Services;

/// <summary>
/// Moteur de transitions dynamiques basé sur les étapes du WorkflowCircuit.
/// Statuts générés : APPROUVE_ROLE{N}, SIGNE_ROLE{N}, REJETE_ROLE{N}, TRANSMIS, TERMINE
/// </summary>
public static class WorkflowEngine
{
    // ── Noms de statuts fixes ────────────────────────────────────────────────
    public const string BROUILLON   = "BROUILLON";
    public const string ENREGISTRE  = "ENREGISTRE";
    public const string SOUMISE     = "SOUMISE";
    public const string EN_ATTENTE  = "EN_ATTENTE";
    public const string TRANSMIS    = "TRANSMIS";
    public const string TERMINE     = "TERMINE";

    public static string StatutApprouve(int ordre)  => $"APPROUVE_ROLE{ordre}";
    public static string StatutSigne(int ordre)     => $"SIGNE_ROLE{ordre}";
    public static string StatutRejete(int ordre)    => $"REJETE_ROLE{ordre}";

    // ── Résolution de l'étape courante ───────────────────────────────────────

    /// <summary>
    /// Retourne l'étape courante du circuit selon le statut du besoin.
    /// Le besoin doit être EN_ATTENTE ou APPROUVE_ROLE{N} pour avoir une étape active.
    /// </summary>
    public static EtapeCircuit? ResoudreEtapeCourante(string statut, IEnumerable<EtapeCircuit> etapes, int? etapeCouranteOrdre = null)
    {
        var liste = etapes.OrderBy(e => e.Ordre).ToList();

        if (statut == EN_ATTENTE)
        {
            // Si on connaît l'ordre de l'étape courante (après transmission), on l'utilise
            if (etapeCouranteOrdre.HasValue)
                return liste.FirstOrDefault(e => e.Ordre == etapeCouranteOrdre.Value)
                    ?? liste.FirstOrDefault();

            return liste.FirstOrDefault();
        }

        // APPROUVE_ROLE{N} → on est toujours sur l'étape N (en attente de signature)
        foreach (var etape in liste)
        {
            if (statut == StatutApprouve(etape.Ordre))
                return etape;
        }

        return null;
    }

    /// <summary>
    /// Calcule le statut résultant après une décision sur une étape.
    /// </summary>
    public static (string nouveauStatut, int? prochaineEtapeOrdre) AppliquerDecision(
        string statutActuel,
        EtapeCircuit etape,
        IList<EtapeCircuit> toutesEtapes,
        string decision)
    {
        if (decision == "REJETE")
            return (StatutRejete(etape.Ordre), null);

        if (decision != "APPROUVE")
            throw new ArgumentException("La décision doit être APPROUVE ou REJETE.");

        // Approbation → si signature requise, on passe à APPROUVE_ROLE{N} (en attente de signature)
        if (statutActuel == EN_ATTENTE)
        {
            if (etape.SignatureRequise)
                return (StatutApprouve(etape.Ordre), etape.Ordre);

            // Pas de signature → on avance directement
            return AvancerApresEtape(etape, toutesEtapes);
        }

        // Signature → statut APPROUVE_ROLE{N}, on avance
        if (statutActuel == StatutApprouve(etape.Ordre))
            return (StatutSigne(etape.Ordre), etape.Ordre); // sera géré par SignatureService

        throw new InvalidOperationException(
            $"Transition invalide depuis le statut '{statutActuel}' avec décision '{decision}'.");
    }

    /// <summary>
    /// Calcule le statut après qu'une signature a été posée sur l'étape courante.
    /// </summary>
    public static (string nouveauStatut, int? prochaineEtapeOrdre) AppliquerSignature(
        EtapeCircuit etape,
        IList<EtapeCircuit> toutesEtapes)
        => AvancerApresEtape(etape, toutesEtapes);

    // ── Vérification d'autorisation ──────────────────────────────────────────

    public static void VerifierRole(EtapeCircuit etape, string roleCode)
    {
        if (!string.Equals(etape.RoleRequis, roleCode, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException(
                $"Cette étape requiert le rôle '{etape.RoleRequis}'. Rôle actuel : '{roleCode}'.");
    }

    // ── Helpers privés ───────────────────────────────────────────────────────

    private static (string nouveauStatut, int? prochaineEtapeOrdre) AvancerApresEtape(
        EtapeCircuit etape,
        IList<EtapeCircuit> toutesEtapes)
    {
        if (etape.EstDerniereEtape)
            return (TERMINE, null);

        var prochaine = toutesEtapes
            .Where(e => e.Ordre > etape.Ordre)
            .OrderBy(e => e.Ordre)
            .FirstOrDefault();

        if (prochaine is null)
            return (TERMINE, null);

        // Transition vers TRANSMIS puis EN_ATTENTE de la prochaine étape
        return (TRANSMIS, prochaine.Ordre);
    }
}
