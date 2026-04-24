using FinAssist.Core.DTOs.Workflow;
using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;

namespace FinAssist.Application.Services;

public class WorkflowService(
    IWorkflowRepository workflowRepo,
    IBesoinsRepository besoinsRepo,
    ISignatureRepository signatureRepo,
    INotificationService notificationService) : IWorkflowService
{
    // ── Valider un besoin ────────────────────────────────────────────────────

    public async Task<ValidationDTO> ValiderAsync(
        int besoinId, ValiderBesoinDTO dto, int validateurId, string roleCode, string nomValidateur)
    {
        var besoin = await besoinsRepo.GetByIdAsync(besoinId)
            ?? throw new KeyNotFoundException($"Besoin {besoinId} introuvable.");

        if (besoin.UtilisateurId == validateurId)
            throw new UnauthorizedAccessException("Vous ne pouvez pas valider un besoin que vous avez créé.");

        if (roleCode is "Administrateur")
            throw new UnauthorizedAccessException("L'Administrateur ne participe pas au processus de validation des besoins.");

        if (string.IsNullOrWhiteSpace(dto.Decision))
            throw new ArgumentException("La décision est obligatoire.");

        var decision = dto.Decision.Trim().ToUpperInvariant();
        if (decision != "APPROUVE" && decision != "REJETE")
            throw new ArgumentException("La décision doit être APPROUVE ou REJETE.");

        if (decision == "REJETE" && string.IsNullOrWhiteSpace(dto.Motif))
            throw new ArgumentException("Le motif est obligatoire en cas de rejet.");

        var circuit = await GetCircuitDuBesoinAsync(besoin);

        var etapes = circuit.Etapes.OrderBy(e => e.Ordre).ToList();
        var etapeCourante = WorkflowEngine.ResoudreEtapeCourante(besoin.Statut.ToString(), etapes, besoin.EtapeCouranteOrdre)
            ?? throw new InvalidOperationException(
                $"Aucune étape active pour le statut '{besoin.Statut}'. " +
                $"Statuts attendus : EN_ATTENTE_ROLE{{N}} ou APPROUVE_ROLE{{N}}.");

        var permissions = (await besoinsRepo.GetCodesPermissionsUtilisateurAsync(validateurId)).ToHashSet();
        WorkflowEngine.VerifierRole(etapeCourante, roleCode, permissions);

         // ── AJOUT : bloquer la double validation ─────────────────────────────
        // Vérifie si cet utilisateur a déjà posé une décision sur ce besoin
        // pour cette étape précise (même niveau, même besoin)
        var dejaValide = await besoinsRepo.UtilisateurADejaValideAsync(besoinId, validateurId);
        if (dejaValide)
            throw new InvalidOperationException(
                "Vous avez déjà rendu une décision sur ce besoin. " +
                "Une double validation n'est pas autorisée.");

        if (decision == "APPROUVE" &&
            besoin.Statut.ToString() == WorkflowEngine.StatutApprouve(etapeCourante.RoleRequis?.ToString() ?? "") &&
            etapeCourante.SignatureRequise)
        {
            // La signature se fait via POST /api/signatures/besoins/{id}/signer — pas bloquant ici
        }

        var (nouveauStatut, prochaineEtapeOrdre) =
            WorkflowEngine.AppliquerDecision(besoin.Statut.ToString(), etapeCourante, etapes, decision);

        var validation = new Validation
        {
            BesoinId = besoinId,
            ValidateurId = validateurId,
            Niveau = etapeCourante.Ordre,
            EtapeOrdre = etapeCourante.Ordre,
            Decision = decision == "APPROUVE" ? DecisionValidation.APPROUVE : DecisionValidation.REJETE,
            Motif = dto.Motif,
            Commentaire = dto.Commentaire,
            StatutApres = nouveauStatut,
            DateDecision = DateTime.UtcNow
        };

        await workflowRepo.AddValidationAsync(validation);

        besoin.Statut = nouveauStatut;
        besoin.EtapeCouranteOrdre = prochaineEtapeOrdre;
        besoin.DateModification = DateTime.UtcNow;
        await besoinsRepo.UpdateAsync(besoin);

        await besoinsRepo.AddHistoriqueAsync(new Historique
        {
            BesoinId = besoinId,
            Action = $"{decision}_ETAPE{etapeCourante.Ordre}",
            Description = dto.Motif ?? $"Décision étape {etapeCourante.Ordre} ({etapeCourante.RoleRequis}) : {decision}",
            DateAction = DateTime.UtcNow
        });

        // Notification dynamique selon la décision et le nouveau statut
        if (validation.Decision == DecisionValidation.REJETE)
        {
            // Notifier le créateur du besoin
            await notificationService.NotifierRejetAsync(
                besoinId, besoin.Titre, besoin.UtilisateurId, etapeCourante.RoleRequis ?? string.Empty);
        }
        else if (WorkflowEngine.EstEnAttente(nouveauStatut))
        {
            // Approuvé sans signature → avance directement à la prochaine étape → notifier le prochain rôle
            var prochaineEtape = etapes.FirstOrDefault(e => e.Ordre == prochaineEtapeOrdre);
            if (prochaineEtape is not null)
                await notificationService.NotifierTransmissionAsync(
                    besoinId, besoin.Titre, prochaineEtape.RoleRequis ?? string.Empty, nomValidateur);
        }
        // Si APPROUVE_ROLE{N} (signature requise), pas de notification ici — elle sera envoyée après signature

        return ToValidationDTO(validation);
    }

    // ── Transmettre manuellement ─────────────────────────────────────────────

    public async Task<ValidationDTO> TransmettreAsync(int besoinId, int validateurId, string nomTransmetteur)
    {
        var besoin = await besoinsRepo.GetByIdAsync(besoinId)
            ?? throw new KeyNotFoundException($"Besoin {besoinId} introuvable.");

        var circuit = await GetCircuitDuBesoinAsync(besoin);
        var etapes = circuit.Etapes.OrderBy(e => e.Ordre).ToList();

        EtapeCircuit? etapeCourante = null;
        foreach (var e in etapes)
        {
            if (besoin.Statut.ToString() == WorkflowEngine.StatutSigne(e.RoleRequis?.ToString() ?? "") ||
                besoin.Statut.ToString() == WorkflowEngine.StatutApprouve(e.RoleRequis?.ToString() ?? ""))
            {
                etapeCourante = e;
                break;
            }
        }

        if (etapeCourante is null)
            throw new InvalidOperationException(
                $"La transmission n'est possible que depuis un statut APPROUVE_ROLE{{N}} ou SIGNE_ROLE{{N}}. " +
                $"Statut actuel : {besoin.Statut}.");

        // Bloquer si la signature est requise mais pas encore posée
        if (etapeCourante.SignatureRequise)
        {
            if (besoin.Statut.ToString() == WorkflowEngine.StatutApprouve(etapeCourante.RoleRequis?.ToString() ?? ""))
                throw new InvalidOperationException(
                    $"La signature électronique est obligatoire avant de transmettre. " +
                    $"Veuillez signer le document (étape {etapeCourante.Ordre} — {etapeCourante.RoleRequis}).");

            // Statut SIGNE_ROLE{N} : vérifier qu'une signature valide existe en base
            var signature = await signatureRepo.GetByBesoinIdAsync(besoinId);
            if (signature is null || !signature.Valide)
                throw new InvalidOperationException(
                    $"Aucune signature valide trouvée pour ce besoin. " +
                    $"Veuillez signer le document avant de transmettre.");
        }

        if (etapeCourante.EstDerniereEtape)
            throw new InvalidOperationException("Impossible de transmettre depuis la dernière étape.");

        var prochaine = etapes.FirstOrDefault(e => e.Ordre > etapeCourante.Ordre)
            ?? throw new InvalidOperationException("Aucune étape suivante trouvée.");

        var validation = new Validation
        {
            BesoinId = besoinId,
            ValidateurId = validateurId,
            Niveau = etapeCourante.Ordre,
            EtapeOrdre = etapeCourante.Ordre,
            Decision = DecisionValidation.TRANSMIS,
            StatutApres = WorkflowEngine.StatutEnAttente(prochaine.RoleRequis?.ToString() ?? ""),
            Commentaire = $"Transmis à l'étape {prochaine.Ordre} ({prochaine.RoleRequis}).",
            DateDecision = DateTime.UtcNow
        };

        await workflowRepo.AddValidationAsync(validation);

        besoin.Statut = WorkflowEngine.StatutEnAttente(prochaine.RoleRequis?.ToString() ?? "");
        besoin.EtapeCouranteOrdre = prochaine.Ordre;
        besoin.DateModification = DateTime.UtcNow;
        await besoinsRepo.UpdateAsync(besoin);

        await besoinsRepo.AddHistoriqueAsync(new Historique
        {
            BesoinId = besoinId,
            Action = "TRANSMISSION",
            Description = $"Transmis à l'étape {prochaine.Ordre} ({prochaine.RoleRequis}).",
            DateAction = DateTime.UtcNow
        });

        // Notifier les utilisateurs du rôle de la prochaine étape
        await notificationService.NotifierTransmissionAsync(besoinId, besoin.Titre, prochaine.RoleRequis ?? string.Empty, nomTransmetteur);

        return ToValidationDTO(validation);
    }

    // ── Circuits ─────────────────────────────────────────────────────────────

    public async Task<WorkflowCircuitDTO> GetCircuitByIdAsync(int id)
    {
        var circuit = await workflowRepo.GetCircuitByIdAsync(id)
            ?? throw new KeyNotFoundException($"Circuit {id} introuvable.");
        return ToCircuitDTO(circuit);
    }

    public async Task<IEnumerable<WorkflowCircuitDTO>> GetAllCircuitsAsync()
    {
        var circuits = await workflowRepo.GetAllCircuitsAsync();
        return circuits.Select(ToCircuitDTO);
    }

    public async Task<WorkflowCircuitDTO> CreateCircuitAsync(
        CreateWorkflowCircuitDTO dto, string nomCreateur)
    {
        if (await workflowRepo.CircuitNomExistsAsync(dto.Nom))
            throw new InvalidOperationException(
                $"Un circuit nommé '{dto.Nom}' existe déjà.");

        if (dto.Etapes.Count == 0)
            throw new ArgumentException("Le circuit doit contenir au moins une étape.");

        var ordres = dto.Etapes.Select(e => e.Ordre).ToList();
        if (ordres.Distinct().Count() != ordres.Count)
            throw new ArgumentException("Les ordres des étapes doivent être uniques.");

        var dernieres = dto.Etapes.Where(e => e.EstDerniereEtape).ToList();
        if (dernieres.Count != 1)
            throw new ArgumentException("Exactement une étape doit être marquée estDerniereEtape = true.");

        var maxOrdre = ordres.Max();
        if (dernieres[0].Ordre != maxOrdre)
            throw new ArgumentException(
                "L'étape marquée estDerniereEtape doit avoir l'ordre le plus élevé.");

        var rolesValides = new HashSet<string>(
            ["Responsable", "Direction", "Administrateur", "Agent"],
            StringComparer.OrdinalIgnoreCase);
        var rolesInvalides = dto.Etapes
            .Where(e => !rolesValides.Contains(e.RoleRequis))
            .Select(e => e.RoleRequis)
            .Distinct()
            .ToList();
        if (rolesInvalides.Count > 0)
            throw new ArgumentException(
                $"Rôles invalides : {string.Join(", ", rolesInvalides)}. " +
                $"Valeurs acceptées : {string.Join(", ", rolesValides)}.");

        var circuit = new WorkflowCircuit
        {
            Nom = dto.Nom,
            Description = dto.Description,
            NomCreateur = nomCreateur,
            DateCreation = DateTime.UtcNow,
            DateModification = DateTime.UtcNow,
            Etapes = dto.Etapes.Select(e => new EtapeCircuit
            {
                Ordre = e.Ordre,
                RoleRequis = e.RoleRequis,
                ApprobationRequise = true,
                SignatureRequise = e.SignatureRequise,
                DelaiMaxJours = e.DelaiMaxJours,
                EstDerniereEtape = e.EstDerniereEtape
            }).ToList()
        };

        var created = await workflowRepo.CreateCircuitAsync(circuit);
        return ToCircuitDTO(created);
    }

    public async Task<WorkflowCircuitDTO> UpdateCircuitAsync(int id, UpdateWorkflowCircuitDTO dto)
    {
        var circuit = await workflowRepo.GetCircuitByIdAsync(id)
            ?? throw new KeyNotFoundException($"Circuit {id} introuvable.");

        if (dto.Nom is not null && dto.Nom != circuit.Nom)
        {
            if (await workflowRepo.CircuitNomExistsAsync(dto.Nom, id))
                throw new InvalidOperationException($"Un circuit nommé '{dto.Nom}' existe déjà.");
            circuit.Nom = dto.Nom;
        }

        if (dto.Description is not null)
            circuit.Description = dto.Description;

        circuit.DateModification = DateTime.UtcNow;
        var updated = await workflowRepo.UpdateCircuitAsync(circuit);
        return ToCircuitDTO(updated);
    }

    public async Task DeleteCircuitAsync(int id)
    {
        var circuit = await workflowRepo.GetCircuitByIdAsync(id)
            ?? throw new KeyNotFoundException($"Circuit {id} introuvable.");

        // Bloquer si des catégories utilisent ce circuit
        var categoriesLiees = await besoinsRepo.GetAllCategoriesAsync();
        if (categoriesLiees.Any(c => c.WorkflowCircuitId == id))
            throw new InvalidOperationException(
                "Impossible de supprimer ce circuit : des catégories l'utilisent encore.");

        await workflowRepo.DeleteCircuitAsync(circuit);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<WorkflowCircuit> GetCircuitDuBesoinAsync(Besoin besoin)
    {
        var categorie = await besoinsRepo.GetCategorieWithCircuitAsync(besoin.CategorieId)
            ?? throw new InvalidOperationException(
                $"Catégorie {besoin.CategorieId} introuvable.");

        if (categorie.WorkflowCircuitId is null)
            throw new InvalidOperationException(
                $"Aucun circuit de validation configuré pour la catégorie '{categorie.Nom}'.");

        var circuit = await workflowRepo.GetCircuitByIdAsync(categorie.WorkflowCircuitId.Value)
            ?? throw new InvalidOperationException(
                $"Circuit {categorie.WorkflowCircuitId} introuvable.");

        if (!circuit.Etapes.Any())
            throw new InvalidOperationException(
                $"Le circuit '{circuit.Nom}' ne contient aucune étape.");

        return circuit;
    }

    private static ValidationDTO ToValidationDTO(Validation v) => new()
    {
        Id = v.Id,
        Niveau = v.Niveau,
        Decision = v.Decision.ToString(),
        Motif = v.Motif,
        Commentaire = v.Commentaire,
        DateDecision = v.DateDecision,
        ValidateurId = v.ValidateurId,
        ValidateurNom = v.Validateur is not null
            ? $"{v.Validateur.Prenom} {v.Validateur.Nom}"
            : string.Empty,
        BesoinId = v.BesoinId
    };

    private static WorkflowCircuitDTO ToCircuitDTO(WorkflowCircuit c) => new()
    {
        Id = c.Id,
        Nom = c.Nom,
        Description = c.Description,
        NomCreateur = c.NomCreateur,
        DateCreation = c.DateCreation,
        DateModification = c.DateModification,
        Etapes = c.Etapes.OrderBy(e => e.Ordre).Select(e => new EtapeCircuitDTO
        {
            Id = e.Id,
            Ordre = e.Ordre,
            RoleRequis = e.RoleRequis,
            ApprobationRequise = e.ApprobationRequise,
            SignatureRequise = e.SignatureRequise,
            DelaiMaxJours = e.DelaiMaxJours,
            EstDerniereEtape = e.EstDerniereEtape,
            StatutApres = e.SignatureRequise
                ? WorkflowEngine.StatutSigne(e.RoleRequis?.ToString() ?? "")
                : (e.EstDerniereEtape
                    ? WorkflowEngine.TERMINE.ToString()
                    : WorkflowEngine.TRANSMIS.ToString())
        }).ToList()
    };
}
