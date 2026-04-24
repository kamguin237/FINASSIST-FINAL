using FinAssist.Core.Entities;

namespace FinAssist.Application.Services;

/// <summary>
/// Moteur de transitions dynamiques basé sur les étapes du WorkflowCircuit.
/// Statuts générés : EN_ATTENTE_{ROLE}, APPROUVE_PAR_{ROLE}, SIGNE_PAR_{ROLE}, REJETE_PAR_{ROLE}
/// </summary>
public static class WorkflowEngine
{
    // ── Statuts fixes ────────────────────────────────────────────────────
    public const string BROUILLON  = "BROUILLON";
    public const string ENREGISTRE = "ENREGISTRE";
    public const string SOUMISE    = "SOUMISE";
    public const string EN_ATTENTE = "EN_ATTENTE"; // compatibilité
    public const string TRANSMIS   = "TRANSMIS";
    public const string TERMINE    = "TERMINE";

    // ── Génération dynamique des statuts (basée sur le rôle) ─────────────
    public static string StatutEnAttente(string roleRequis)
        => $"EN_ATTENTE_{roleRequis.Trim().ToUpperInvariant()}";

    public static string StatutApprouve(string roleRequis)
        => $"APPROUVE_PAR_{roleRequis.Trim().ToUpperInvariant()}";

    public static string StatutSigne(string roleRequis)
        => $"SIGNE_PAR_{roleRequis.Trim().ToUpperInvariant()}";

    public static string StatutRejete(string roleRequis)
        => $"REJETE_PAR_{roleRequis.Trim().ToUpperInvariant()}";

    // ── Helpers de détection ─────────────────────────────────────────────
    public static bool EstEnAttente(string statut)
        => statut == EN_ATTENTE || statut.StartsWith("EN_ATTENTE_");

    public static bool EstApprouve(string statut)
        => statut.StartsWith("APPROUVE_PAR_");

    public static bool EstRejete(string statut)
        => statut.StartsWith("REJETE_PAR_");

    public static bool EstSigne(string statut)
        => statut.StartsWith("SIGNE_PAR_");

    // ── Résolution de l'étape courante ───────────────────────────────────
    public static EtapeCircuit? ResoudreEtapeCourante(
        string statut,
        IEnumerable<EtapeCircuit> etapes,
        int? etapeCouranteOrdre = null)
    {
        var liste = etapes.OrderBy(e => e.Ordre).ToList();

        // EN_ATTENTE_{ROLE} → trouver l'étape dont RoleRequis correspond
        if (statut == EN_ATTENTE || statut.StartsWith("EN_ATTENTE_"))
        {
            // Priorité : etapeCouranteOrdre si disponible
            if (etapeCouranteOrdre.HasValue)
                return liste.FirstOrDefault(e => e.Ordre == etapeCouranteOrdre.Value)
                    ?? liste.FirstOrDefault();

            // Extraire le rôle depuis le statut : EN_ATTENTE_RESPONSABLE → RESPONSABLE
            if (statut.StartsWith("EN_ATTENTE_") && statut != EN_ATTENTE)
            {
                var roleStatut = statut.Replace("EN_ATTENTE_", "");
                return liste.FirstOrDefault(e =>
                    e.RoleRequis?.Trim().ToUpperInvariant() == roleStatut)
                    ?? liste.FirstOrDefault();
            }

            return liste.FirstOrDefault();
        }

        // APPROUVE_PAR_{ROLE} → étape en attente de signature
        if (statut.StartsWith("APPROUVE_PAR_"))
        {
            var roleStatut = statut.Replace("APPROUVE_PAR_", "");
            return liste.FirstOrDefault(e =>
                e.RoleRequis?.Trim().ToUpperInvariant() == roleStatut);
        }

        return null;
    }

    // ── Application d'une décision ───────────────────────────────────────
    public static (string nouveauStatut, int? prochaineEtapeOrdre) AppliquerDecision(
        string statutActuel,
        EtapeCircuit etape,
        IList<EtapeCircuit> toutesEtapes,
        string decision)
    {
        // Rôle requis de l'étape = source de vérité pour le statut généré
        var roleRequis = etape.RoleRequis?.Trim() ?? $"ROLE{etape.Ordre}";

        if (decision == "REJETE")
            return (StatutRejete(roleRequis), null);

        if (decision != "APPROUVE")
            throw new ArgumentException("La décision doit être APPROUVE ou REJETE.");

        if (EstEnAttente(statutActuel))
        {
            if (etape.SignatureRequise)
                return (StatutApprouve(roleRequis), etape.Ordre);

            return AvancerApresEtape(etape, toutesEtapes);
        }

        // APPROUVE_PAR_{ROLE} → signature posée → on avance
        if (statutActuel == StatutApprouve(roleRequis))
            return (StatutSigne(roleRequis), etape.Ordre);

        throw new InvalidOperationException(
            $"Transition invalide depuis '{statutActuel}' avec décision '{decision}'.");
    }

    // ── Application d'une signature ──────────────────────────────────────
    public static (string nouveauStatut, int? prochaineEtapeOrdre) AppliquerSignature(
        EtapeCircuit etape,
        IList<EtapeCircuit> toutesEtapes)
        => AvancerApresEtape(etape, toutesEtapes);

    // ── Vérification d'autorisation ──────────────────────────────────────
    public static void VerifierRole(EtapeCircuit etape, string roleCode, HashSet<string>? permissions = null)
    {
        var roleMatch = string.Equals(etape.RoleRequis, roleCode, StringComparison.OrdinalIgnoreCase);
        if (roleMatch) return;

        // Fallback : vérifier si l'utilisateur a une permission directe correspondant à l'étape
        if (permissions is not null && permissions.Count > 0)
        {
            var permAttendue = $"BESOIN_VALIDER_{etape.RoleRequis?.Trim().ToUpperInvariant()}";
            if (permissions.Contains(permAttendue)) return;
        }

        throw new UnauthorizedAccessException(
            $"Cette étape requiert le rôle '{etape.RoleRequis}'. Rôle actuel : '{roleCode}'.");
    }

    // ── Helper privé ─────────────────────────────────────────────────────
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

        // Utiliser le rôle de la PROCHAINE étape pour générer le statut
        var roleProchaine = prochaine.RoleRequis?.Trim() ?? $"ROLE{prochaine.Ordre}";
        return (StatutEnAttente(roleProchaine), prochaine.Ordre);
    }
}