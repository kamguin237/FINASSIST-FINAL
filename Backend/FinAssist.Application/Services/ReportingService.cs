using FinAssist.Core.DTOs.Reporting;
using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;

namespace FinAssist.Application.Services;

public class ReportingService(
    IReportingRepository reportingRepo,
    IBesoinsRepository besoinsRepo,
    IExportService exportService) : IReportingService
{
    public async Task<StatistiquesDTO> GetStatistiquesAsync()
    {
        var besoins = await reportingRepo.GetAllBesoinsAsync();
        var utilisateurs = await reportingRepo.GetAllUtilisateursAsync();
        var signatures = await reportingRepo.CountSignaturesAsync();
        var notifications = await reportingRepo.CountNotificationsAsync();

        return new StatistiquesDTO
        {
            TotalBesoins = besoins.Count(),
            TotalUtilisateurs = utilisateurs.Count(),
            TotalActifs = utilisateurs.Count(u => u.Actif),
            BesoinsByStatut = besoins
                .GroupBy(b => b.Statut.ToString())
                .ToDictionary(g => g.Key, g => g.Count()),
            BesoinsByCategorie = besoins
                .GroupBy(b => b.Categorie?.Nom ?? "Sans catégorie")
                .ToDictionary(g => g.Key, g => g.Count()),
            SignaturesApposees = signatures,
            NotificationsEnvoyees = notifications
        };
    }

    public async Task<RapportDTO> GetRapportBesoinsAsync(FiltreRapportDTO? filtres, int generateurId)
    {
        var besoins = await reportingRepo.GetBesoinsFiltrésAsync(filtres);

        var contenu = besoins.Select(b => new
        {
            b.Id,
            b.Titre,
            Statut = b.Statut.ToString(),
            b.NiveauImportance,
            Categorie = b.Categorie?.Nom ?? string.Empty,
            Createur = b.Utilisateur is not null ? $"{b.Utilisateur.Prenom} {b.Utilisateur.Nom}" : string.Empty,
            b.DateCreation,
            b.DateModification
        });

        var parametres = new Dictionary<string, string>();
        if (filtres?.DateDebut is not null) parametres["dateDebut"] = filtres.DateDebut.Value.ToString("yyyy-MM-dd");
        if (filtres?.DateFin is not null) parametres["dateFin"] = filtres.DateFin.Value.ToString("yyyy-MM-dd");
        if (filtres?.Statut is not null) parametres["statut"] = filtres.Statut;

        return new RapportDTO
        {
            Type = "RAPPORT_BESOINS",
            DateGeneration = DateTime.UtcNow,
            Contenu = contenu,
            GenerateurId = generateurId,
            Parametres = parametres
        };
    }

    
public async Task<DashboardDTO> GetDashboardAsync(int utilisateurId, string roleCode)
{
    var tousBesoins = (await reportingRepo.GetAllBesoinsAsync()).ToList();
    var sesValidations = (await besoinsRepo.GetValidationsParUtilisateurAsync(utilisateurId)).ToList();
    var sesPermissions = (await besoinsRepo.GetCodesPermissionsUtilisateurAsync(utilisateurId)).ToHashSet();

    Console.WriteLine($"[DASHBOARD] userId={utilisateurId} role={roleCode}");
    Console.WriteLine($"[DASHBOARD] tousBesoins={tousBesoins.Count} sesValidations={sesValidations.Count} sesPermissions={sesPermissions.Count}");

    // ── Cas Administrateur : voit tout ───────────────────────────────────
    if (roleCode is "Administrateur")
    {
        var enAttenteAdmin = tousBesoins.Count(b => WorkflowEngine.EstEnAttente(b.Statut));
        var approuvesAdmin = tousBesoins.Count(b => WorkflowEngine.EstApprouve(b.Statut) || WorkflowEngine.EstSigne(b.Statut) || b.Statut == WorkflowEngine.TERMINE);
        var rejetesAdmin = tousBesoins.Count(b => WorkflowEngine.EstRejete(b.Statut));
        var signesAdmin = tousBesoins.Count(b => WorkflowEngine.EstSigne(b.Statut) || b.Statut == WorkflowEngine.TERMINE);
        var nonLuesAdmin = await reportingRepo.CountNotificationsNonLuesAsync(utilisateurId);

        Console.WriteLine($"[DASHBOARD][Admin] enAttente={enAttenteAdmin} approuves={approuvesAdmin} rejetes={rejetesAdmin}");

         // ── BesoinsSoumisParMoi — 100% dynamique, aucune référence au rôle ───
        // Fonctionne pour tout utilisateur, tout rôle présent ou futur.
        // Condition : créé par lui + a quitté BROUILLON/ENREGISTRE (= soumis)
        var besoinsSoumisParMoi = tousBesoins.Count(b =>
            b.UtilisateurId == utilisateurId &&
            b.Statut != WorkflowEngine.BROUILLON &&
            b.Statut != WorkflowEngine.ENREGISTRE);

        return new DashboardDTO
        {
            BesoinsEnAttente = enAttenteAdmin,
            BesoinsApprouves = approuvesAdmin,
            BesoinsRejetes = rejetesAdmin,
            BesoinsSignes = signesAdmin,
            BesoinsSoumis = tousBesoins.Count(b => WorkflowEngine.EstEnAttente(b.Statut) || WorkflowEngine.EstApprouve(b.Statut) || WorkflowEngine.EstSigne(b.Statut) || WorkflowEngine.EstRejete(b.Statut) || b.Statut == WorkflowEngine.TERMINE),
            BesoinsEnregistres = tousBesoins.Count(b => b.Statut == "ENREGISTRE"),
            BesoinsBrouillons = tousBesoins.Count(b => b.Statut == "BROUILLON"),
            NotificationsNonLues = nonLuesAdmin,
            BesoinsSoumisParMoi  = besoinsSoumisParMoi,
            DerniersBesoins = tousBesoins
                .OrderByDescending(b => b.DateModification)
                .Take(5)
                .Select(b => new BesoinRecent
                {
                    Id = b.Id,
                    Titre = b.Titre,
                    Statut = b.Statut,
                    DateCreation = b.DateCreation,
                    DateModification = b.DateModification
                })
        };
    }

    // ── Logique pour les non-administrateurs (Responsable, Agent, Direction, etc.) ──
    
    // 1. Besoins créés par l'utilisateur
    var besoinsCrees = tousBesoins
        .Where(b => b.UtilisateurId == utilisateurId)
        .ToList();

    Console.WriteLine($"[DASHBOARD] besoinsCrees={besoinsCrees.Count}");

    // 2. Besoins dont l'étape courante requiert cet utilisateur
    var besoinsEnAttentePourMoi = tousBesoins.Where(b =>
    {
        var statutStr = b.Statut.ToString();
        if (!WorkflowEngine.EstEnAttente(statutStr)) return false;

        // Fallback si navigation properties null
        var roleRequis = statutStr.Replace("EN_ATTENTE_", "", StringComparison.OrdinalIgnoreCase);
        if (roleRequis.Equals(roleCode, StringComparison.OrdinalIgnoreCase) || 
            sesPermissions.Contains($"VALIDER_ROLE_{roleRequis.ToUpperInvariant()}"))
        {
            return true;
        }

        var etape = b.Categorie?.WorkflowCircuit?.Etapes is not null
            ? WorkflowEngine.ResoudreEtapeCourante(
                statutStr, b.Categorie.WorkflowCircuit.Etapes, b.EtapeCouranteOrdre)
            : null;
        return etape is not null && UtilisateurPeutAgirSurEtape(etape, roleCode, sesPermissions);
    }).ToList();

    Console.WriteLine($"[DASHBOARD] besoinsEnAttentePourMoi={besoinsEnAttentePourMoi.Count}");
    foreach (var b in besoinsEnAttentePourMoi)
        Console.WriteLine($"  → EN_ATTENTE: id={b.Id} statut={b.Statut}");

    // 3. Besoins déjà traités par l'utilisateur
    var besoinIdsTraites = sesValidations.Select(v => v.BesoinId).ToHashSet();
    var besoinsDejaTraites = tousBesoins.Where(b => besoinIdsTraites.Contains(b.Id)).ToList();

    Console.WriteLine($"[DASHBOARD] besoinsDejaTraites={besoinsDejaTraites.Count}");

    // 4. IDs des décisions passées
    var besoinIdsApprouves = sesValidations
        .Where(v => v.Decision == DecisionValidation.APPROUVE)
        .Select(v => v.BesoinId).ToHashSet();

    var besoinIdsRejetes = sesValidations
        .Where(v => v.Decision == DecisionValidation.REJETE)
        .Select(v => v.BesoinId).ToHashSet();

    var besoinIdsCrees = besoinsCrees.Select(b => b.Id).ToHashSet();

    Console.WriteLine($"[DASHBOARD] besoinIdsApprouves={besoinIdsApprouves.Count} besoinIdsRejetes={besoinIdsRejetes.Count}");

    // 5. Compteurs
    var enAttente = besoinsEnAttentePourMoi.Count;

    var approuves = besoinIdsApprouves.Count
        + besoinsCrees.Count(b =>
            !besoinIdsApprouves.Contains(b.Id) &&
            (WorkflowEngine.EstApprouve(b.Statut) ||
             WorkflowEngine.EstSigne(b.Statut) ||
             b.Statut == WorkflowEngine.TERMINE));

    var rejetes = besoinIdsRejetes.Count
        + besoinsCrees.Count(b =>
            !besoinIdsRejetes.Contains(b.Id) &&
            WorkflowEngine.EstRejete(b.Statut));

    var signes = tousBesoins.Count(b =>
        (besoinIdsApprouves.Contains(b.Id) || besoinIdsCrees.Contains(b.Id)) &&
        (WorkflowEngine.EstSigne(b.Statut) || b.Statut == WorkflowEngine.TERMINE));

    Console.WriteLine($"[DASHBOARD] RESULT: enAttente={enAttente} approuves={approuves} rejetes={rejetes} signes={signes}");

    // Calcul des "Reçus" : Besoins transmis pour validation (en attente ou déjà traités) 
    // en excluant les besoins créés par l'utilisateur lui-même.
    var recus = besoinsEnAttentePourMoi
        .Union(besoinsDejaTraites, new BesoinIdEqualityComparer())
        .Count(b => b.UtilisateurId != utilisateurId);

    var soumisParMoi = besoinsCrees.Count(b => 
        b.Statut != WorkflowEngine.BROUILLON && 
        b.Statut != WorkflowEngine.ENREGISTRE);

    // 6. Périmètre pour "derniers besoins"
    var perimetre = besoinsCrees
        .Union(besoinsEnAttentePourMoi, new BesoinIdEqualityComparer())
        .Union(besoinsDejaTraites, new BesoinIdEqualityComparer())
        .ToList();

    var nonLues = await reportingRepo.CountNotificationsNonLuesAsync(utilisateurId);

    return new DashboardDTO
    {
        BesoinsEnAttente = enAttente,
        BesoinsApprouves = approuves,
        BesoinsRejetes = rejetes,
        BesoinsSignes = signes,
        BesoinsSoumis = recus, // Correspond maintenant aux besoins "Reçus" pour validation
        BesoinsSoumisParMoi = soumisParMoi,
        BesoinsEnregistres = perimetre.Count(b => b.Statut == "ENREGISTRE"),
        BesoinsBrouillons = perimetre.Count(b => b.Statut == "BROUILLON"),
        NotificationsNonLues = nonLues,
        DerniersBesoins = perimetre
            .OrderByDescending(b => b.DateModification)
            .Take(5)
            .Select(b => new BesoinRecent
            {
                Id = b.Id,
                Titre = b.Titre,
                Statut = b.Statut,
                DateCreation = b.DateCreation,
                DateModification = b.DateModification
            })
    };
}

    
public async Task<IEnumerable<EvolutionPointDTO>> GetEvolutionBesoinsAsync(
    string periode, int utilisateurId, string roleCode)
{
    var tousBesoins = (await reportingRepo.GetAllBesoinsAsync()).ToList();

    // ── Cas Administrateur : tous les besoins ──────────────────────────────
    if (roleCode is "Administrateur")
    {
        var now = DateTime.UtcNow.Date;
        IEnumerable<DateTime> dates = periode switch
        {
            "semaines" => Enumerable.Range(0, 8).Select(i =>
                now.AddDays(-(int)now.DayOfWeek + 1).AddDays(-7 * (7 - i))),
            "mois" => Enumerable.Range(0, 12).Select(i =>
                new DateTime(now.Year, now.Month, 1).AddMonths(i - 11)),
            _ => Enumerable.Range(0, 14).Select(i => now.AddDays(i - 13))
        };

        var points = new List<EvolutionPointDTO>();
        foreach (var date in dates)
        {
            var fin = periode switch
            {
                "semaines" => date.AddDays(7),
                "mois"     => date.AddMonths(1),
                _          => date.AddDays(1)
            };

            var slice = tousBesoins.Where(b =>
                b.DateCreation.Date >= date && b.DateCreation.Date < fin).ToList();

            var recus     = slice.Count(b => b.Statut != WorkflowEngine.BROUILLON && b.Statut != WorkflowEngine.ENREGISTRE);
            var approuves = slice.Count(b => WorkflowEngine.EstApprouve(b.Statut) || WorkflowEngine.EstSigne(b.Statut) || b.Statut == WorkflowEngine.TERMINE);
            var rejetes   = slice.Count(b => WorkflowEngine.EstRejete(b.Statut));
            var attente48 = tousBesoins.Count(b => WorkflowEngine.EstEnAttente(b.Statut) && b.DateModification.Date <= fin.AddDays(-2));

            points.Add(new EvolutionPointDTO
            {
                Date             = date.ToString("yyyy-MM-dd"),
                Recus            = recus,
                Approuves        = approuves,
                Rejetes          = rejetes,
                EnAttentePlus48h = attente48,
                TauxApprobation  = recus > 0 ? Math.Round((double)approuves / recus * 100, 1) : 0
            });
        }
        return points;
    }

    // ── Cas utilisateur normal : périmètre restreint ───────────────────────
    var sesValidations = (await besoinsRepo.GetValidationsParUtilisateurAsync(utilisateurId)).ToList();
    var sesPermissions = (await besoinsRepo.GetCodesPermissionsUtilisateurAsync(utilisateurId)).ToHashSet();

    var besoinsCrees = tousBesoins
        .Where(b => b.UtilisateurId == utilisateurId)
        .ToList();

    var besoinIdsCrees = besoinsCrees.Select(b => b.Id).ToHashSet();

    var besoinsEnAttentePourMoi = tousBesoins.Where(b =>
    {
        var statutStr = b.Statut.ToString();
        if (!WorkflowEngine.EstEnAttente(statutStr)) return false;
        var etape = b.Categorie?.WorkflowCircuit?.Etapes is not null
            ? WorkflowEngine.ResoudreEtapeCourante(
                b.Statut.ToString(), b.Categorie.WorkflowCircuit.Etapes, b.EtapeCouranteOrdre)
            : null;
        return etape is not null &&
               UtilisateurPeutAgirSurEtape(etape, roleCode, sesPermissions);
    }).ToList();

    var besoinIdsTraites = sesValidations.Select(v => v.BesoinId).ToHashSet();

    var dateDecisionApprouve = sesValidations
        .Where(v => v.Decision == DecisionValidation.APPROUVE)
        .GroupBy(v => v.BesoinId)
        .ToDictionary(g => g.Key, g => g.Min(v => v.DateDecision));

    var dateDecisionRejete = sesValidations
        .Where(v => v.Decision == DecisionValidation.REJETE)
        .GroupBy(v => v.BesoinId)
        .ToDictionary(g => g.Key, g => g.Min(v => v.DateDecision));

    // ✅ RENOMMÉ : currentNow au lieu de now
    var currentNow = DateTime.UtcNow.Date;
    IEnumerable<DateTime> bucketDates = periode switch
    {
        "semaines" => Enumerable.Range(0, 8).Select(i =>
            currentNow.AddDays(-(int)currentNow.DayOfWeek + 1).AddDays(-7 * (7 - i))),
        "mois" => Enumerable.Range(0, 12).Select(i =>
            new DateTime(currentNow.Year, currentNow.Month, 1).AddMonths(i - 11)),
        _ => Enumerable.Range(0, 14).Select(i => currentNow.AddDays(i - 13))
    };

    // ✅ RENOMMÉ : resultPoints au lieu de points
    var resultPoints = new List<EvolutionPointDTO>();
    foreach (var date in bucketDates)
    {
        var fin = periode switch
        {
            "semaines" => date.AddDays(7),
            "mois"     => date.AddMonths(1),
            _          => date.AddDays(1)
        };

        var traitesDansBucket = dateDecisionApprouve
            .Where(kv => kv.Value.Date >= date && kv.Value.Date < fin && !besoinIdsCrees.Contains(kv.Key))
            .Select(kv => kv.Key)
            .Union(
                dateDecisionRejete
                    .Where(kv => kv.Value.Date >= date && kv.Value.Date < fin && !besoinIdsCrees.Contains(kv.Key))
                    .Select(kv => kv.Key))
            .ToHashSet();

        var enAttenteDansBucket = besoinsEnAttentePourMoi.Count(b =>
            !besoinIdsTraites.Contains(b.Id) &&
            !besoinIdsCrees.Contains(b.Id) &&
            b.DateModification.Date >= date &&
            b.DateModification.Date < fin);

        var recus = traitesDansBucket.Count + enAttenteDansBucket;

        var approuvesViaValidation = dateDecisionApprouve
            .Count(kv => kv.Value.Date >= date && kv.Value.Date < fin && !besoinIdsCrees.Contains(kv.Key));

        var approuvesViaCreation = besoinsCrees.Count(b =>
            !dateDecisionApprouve.ContainsKey(b.Id) &&
            b.DateModification.Date >= date &&
            b.DateModification.Date < fin &&
            (WorkflowEngine.EstApprouve(b.Statut)||
             WorkflowEngine.EstSigne(b.Statut) ||
             b.Statut == WorkflowEngine.TERMINE));

        var approuves = approuvesViaValidation + approuvesViaCreation;

        var rejetes = dateDecisionRejete.Count(kv =>
                kv.Value.Date >= date && kv.Value.Date < fin && !besoinIdsCrees.Contains(kv.Key))
            + besoinsCrees.Count(b =>
                !dateDecisionRejete.ContainsKey(b.Id) &&
                b.DateModification.Date >= date &&
                b.DateModification.Date < fin &&
                WorkflowEngine.EstRejete(b.Statut));

        var enAttentePlus48h = besoinsEnAttentePourMoi.Count(b =>
            b.DateModification.Date <= fin.AddDays(-2));

        resultPoints.Add(new EvolutionPointDTO
        {
            Date             = date.ToString("yyyy-MM-dd"),
            Recus            = recus,
            Approuves        = approuves,
            Rejetes          = rejetes,
            EnAttentePlus48h = enAttentePlus48h,
            TauxApprobation  = recus > 0 ? Math.Round((double)approuves / recus * 100, 1) : 0
        });
    }

    return resultPoints;
}


    public async Task<(byte[] contenu, string contentType, string nomFichier)> ExporterAsync(
        ExportRequestDTO request, int generateurId)
    {
        var rapport = await GetRapportBesoinsAsync(request.Filtres, generateurId);
        var besoins = await reportingRepo.GetBesoinsFiltrésAsync(request.Filtres);

        return request.Format.ToUpperInvariant() switch
        {
            "PDF" => (
                exportService.ExporterPdf(besoins),
                "application/pdf",
                $"rapport_besoins_{DateTime.UtcNow:yyyyMMdd}.pdf"),
            _ => (
                exportService.ExporterExcel(besoins),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"rapport_besoins_{DateTime.UtcNow:yyyyMMdd}.xlsx")
        };
    }

    /// <summary>
    /// Détermine si un utilisateur peut agir sur une étape du circuit,
    /// selon son rôle OU ses permissions individuelles.
    /// Convention de permission : "VALIDER_ROLE_{ROLE_CODE_EN_MAJUSCULES}"
    /// Exemple : pour agir à la place d'un "Responsable" → permission "VALIDER_ROLE_RESPONSABLE"
    /// </summary>
    private static bool UtilisateurPeutAgirSurEtape(
        EtapeCircuit etape, string roleCode, HashSet<string> permissions)
    {
        if (string.IsNullOrEmpty(etape.RoleRequis)) return false;

        // 1. Match direct par rôle
        if (etape.RoleRequis.Equals(roleCode, StringComparison.OrdinalIgnoreCase))
            return true;

        // 2. Permission explicite d'agir à la place du rôle requis par l'étape
        var permissionRequise = $"VALIDER_ROLE_{etape.RoleRequis.ToUpperInvariant()}";
        return permissions.Contains(permissionRequise);
    }

}

file class BesoinIdEqualityComparer : IEqualityComparer<Besoin>
{
    public bool Equals(Besoin? x, Besoin? y) => x?.Id == y?.Id;
    public int GetHashCode(Besoin obj) => obj.Id.GetHashCode();
}
