using FinAssist.Core.DTOs.Besoins;
using FinAssist.Core.DTOs.Workflow;
using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;

namespace FinAssist.Application.Services;

public class BesoinsService(
    IBesoinsRepository besoinsRepo, 
    IWorkflowRepository workflowRepo, 
    INotificationService notificationService,
    IBesoinsHubService besoinsHubService) : IBesoinsService
{
    // Filtrage selon le rôle :
    // - Administrateur / Direction / Responsable → tous les besoins
    // - Agent → uniquement les besoins qui lui sont assignés
    // - Autres (Agent créateur) → uniquement ses propres besoins
    public async Task<IEnumerable<BesoinDTO>> GetAllAsync(int utilisateurId, string roleCode)
    {
        if (roleCode is Roles.Administrateur)
        {
            var all = (await besoinsRepo.GetAllAsync()).ToList();
            Console.WriteLine($"[BESOINS][Admin] Total: {all.Count}");
            return all.Select(ToDTO);
        }

        var sesPermissions = (await besoinsRepo.GetCodesPermissionsUtilisateurAsync(utilisateurId)).ToHashSet();

        // 1. Besoins créés par l'utilisateur
        var mesBesoins = (await besoinsRepo.GetByUtilisateurAsync(utilisateurId)).ToList();
        Console.WriteLine($"[BESOINS][userId={utilisateurId}][role={roleCode}] mesBesoins: {mesBesoins.Count}");
        foreach (var b in mesBesoins)
            Console.WriteLine($"  → MES: id={b.Id} titre='{b.Titre}' statut={b.Statut}");

        // 2. Besoins soumis pour validation (EN_ATTENTE/TRANSMIS dont l'étape requiert son rôle)
        //    + besoins qu'il a approuvés/signés en attente de transmission (APPROUVE_ROLE/SIGNE_ROLE)
        var tousBesoins = await besoinsRepo.GetAllAsync();
        var besoinsAValider = tousBesoins
            .Where(b =>
            {
                var statutStr = b.Statut.ToString();

                // Sécurité : si les objets liés (Circuit/Etapes) ne sont pas chargés,
                // on se base sur le rôle extrait de la chaîne de statut.
                var roleExtrait = statutStr.Replace("EN_ATTENTE_", "").Replace("APPROUVE_PAR_", "").Replace("SIGNE_PAR_", "");
                if (roleExtrait.Equals(roleCode, StringComparison.OrdinalIgnoreCase) || 
                    sesPermissions.Contains($"VALIDER_ROLE_{roleExtrait.ToUpperInvariant()}"))
                {
                    return true;
                }

                if (WorkflowEngine.EstEnAttente(statutStr))
                {
                    var etape = b.Categorie?.WorkflowCircuit?.Etapes is not null
                        ? WorkflowEngine.ResoudreEtapeCourante(statutStr, b.Categorie.WorkflowCircuit.Etapes, b.EtapeCouranteOrdre)
                        : null;
                    return etape is not null && UtilisateurPeutAgirSurEtape(etape, roleCode, sesPermissions);
                }

                // Vérification pour la transmission manuelle après signature
                if (b.Categorie?.WorkflowCircuit?.Etapes is not null)
                {
                    foreach (var etape in b.Categorie.WorkflowCircuit.Etapes)
                    {
                        if ((statutStr == WorkflowEngine.StatutApprouve(etape.RoleRequis ?? "") ||
                             statutStr == WorkflowEngine.StatutSigne(etape.RoleRequis ?? "")) &&
                            UtilisateurPeutAgirSurEtape(etape, roleCode, sesPermissions))
                            return true;
                    }
                }
                return false;
            })
            .ToList();

        Console.WriteLine($"[BESOINS][userId={utilisateurId}][role={roleCode}] besoinsAValider: {besoinsAValider.Count}");
        foreach (var b in besoinsAValider)
            Console.WriteLine($"  → VALIDER: id={b.Id} titre='{b.Titre}' statut={b.Statut}");

        // 3. Besoins sur lesquels l'utilisateur a déjà validé (approuvé/rejeté/signé)
        //    → il doit continuer à les voir même après transmission à l'étape suivante
        var besoinIdsValides = (await besoinsRepo.GetBesoinIdsValidesParUtilisateurAsync(utilisateurId)).ToHashSet();
        var besoinsDejaValides = tousBesoins
            .Where(b => besoinIdsValides.Contains(b.Id))
            .ToList();

        Console.WriteLine($"[BESOINS][userId={utilisateurId}][role={roleCode}] besoinsDejaValides: {besoinsDejaValides.Count}");
        foreach (var b in besoinsDejaValides)
            Console.WriteLine($"  → DEJA_VALIDE: id={b.Id} titre='{b.Titre}' statut={b.Statut}");

        // Union des trois ensembles sans doublons
        var tous = mesBesoins
            .Union(besoinsAValider, BesoinIdComparer.Instance)
            .Union(besoinsDejaValides, BesoinIdComparer.Instance)
            .OrderByDescending(b => b.DateModification)
            .ToList();

        Console.WriteLine($"[BESOINS][userId={utilisateurId}][role={roleCode}] TOTAL union: {tous.Count}");

        return tous.Select(ToDTO);
    }

    // public async Task<BesoinDTO> GetByIdAsync(int id, int utilisateurId, string roleCode)
    // {
    //     var besoin = await besoinsRepo.GetByIdAsync(id)
    //         ?? throw new KeyNotFoundException($"Besoin {id} introuvable.");

    //     // L'administrateur peut accéder à n'importe quel besoin
    //     if (roleCode is Roles.Administrateur)
    //         return ToDTO(besoin);

    //     // Le créateur peut accéder à son propre besoin
    //     if (besoin.UtilisateurId == utilisateurId)
    //         return ToDTO(besoin);

    //     var sesPermissions = (await besoinsRepo.GetCodesPermissionsUtilisateurAsync(utilisateurId)).ToHashSet();

    //     // Un validateur peut accéder aux besoins soumis pour validation selon son rôle
    //     var statutStr = besoin.Statut.ToString();
    //     if (WorkflowEngine.EstEnAttente(statutStr) || WorkflowEngine.EstApprouve(statutStr) || WorkflowEngine.EstSigne(statutStr))
    //     {
    //         // Accès autorisé si le rôle extrait du statut correspond à l'utilisateur
    //         var roleExtrait = statutStr.Replace("EN_ATTENTE_", "").Replace("APPROUVE_PAR_", "").Replace("SIGNE_PAR_", "");
    //         if (roleExtrait.Equals(roleCode, StringComparison.OrdinalIgnoreCase) || 
    //             sesPermissions.Contains($"VALIDER_ROLE_{roleExtrait.ToUpperInvariant()}"))
    //         {
    //             return ToDTO(besoin);
    //         }

    //         var etapeCourante = besoin.Categorie?.WorkflowCircuit?.Etapes is not null
    //             ? WorkflowEngine.ResoudreEtapeCourante(
    //                 statutStr,
    //                 besoin.Categorie.WorkflowCircuit.Etapes,
    //                 besoin.EtapeCouranteOrdre)
    //             : null;

    //         if (etapeCourante is not null && UtilisateurPeutAgirSurEtape(etapeCourante, roleCode, sesPermissions))
    //             return ToDTO(besoin);
    //     }

    //     // Un validateur peut aussi accéder aux besoins qu'il a déjà validés
    //     var besoinIdsValides = (await besoinsRepo.GetBesoinIdsValidesParUtilisateurAsync(utilisateurId)).ToHashSet();
    //     if (besoinIdsValides.Contains(id))
    //         return ToDTO(besoin);

    //     throw new UnauthorizedAccessException("Accès refusé.");
    // }
    public async Task<BesoinDTO> GetByIdAsync(int id, int utilisateurId, string roleCode)
{
    var besoin = await besoinsRepo.GetByIdAsync(id)
        ?? throw new KeyNotFoundException($"Besoin {id} introuvable.");

    // Calculer DejaValideParMoi UNE SEULE FOIS pour tous les cas
    var dejaValide = await besoinsRepo.UtilisateurADejaValideAsync(id, utilisateurId);

    // Helper local pour ne pas répéter le champ partout
    BesoinDTO DtoAvecValidation() 
    {
        var dto = ToDTO(besoin);
        dto.DejaValideParMoi = dejaValide;
        return dto;
    }

    // L'administrateur peut accéder à n'importe quel besoin
    if (roleCode is Roles.Administrateur)
        return DtoAvecValidation();

    // Le créateur peut accéder à son propre besoin
    if (besoin.UtilisateurId == utilisateurId)
        return DtoAvecValidation();

    var sesPermissions = (await besoinsRepo.GetCodesPermissionsUtilisateurAsync(utilisateurId)).ToHashSet();

    // Un validateur peut accéder aux besoins soumis pour validation selon son rôle
    var statutStr = besoin.Statut.ToString();
    if (WorkflowEngine.EstEnAttente(statutStr) ||
        WorkflowEngine.EstApprouve(statutStr) ||
        WorkflowEngine.EstSigne(statutStr))
    {
        var roleExtrait = statutStr
            .Replace("EN_ATTENTE_", "")
            .Replace("APPROUVE_PAR_", "")
            .Replace("SIGNE_PAR_", "");

        if (roleExtrait.Equals(roleCode, StringComparison.OrdinalIgnoreCase) ||
            sesPermissions.Contains($"VALIDER_ROLE_{roleExtrait.ToUpperInvariant()}"))
            return DtoAvecValidation();

        var etapeCourante = besoin.Categorie?.WorkflowCircuit?.Etapes is not null
            ? WorkflowEngine.ResoudreEtapeCourante(
                statutStr,
                besoin.Categorie.WorkflowCircuit.Etapes,
                besoin.EtapeCouranteOrdre)
            : null;

        if (etapeCourante is not null &&
            UtilisateurPeutAgirSurEtape(etapeCourante, roleCode, sesPermissions))
            return DtoAvecValidation();
    }

    // Un validateur peut aussi accéder aux besoins qu'il a déjà validés
    var besoinIdsValides = (await besoinsRepo
        .GetBesoinIdsValidesParUtilisateurAsync(utilisateurId)).ToHashSet();

    if (besoinIdsValides.Contains(id))
        return DtoAvecValidation();

    throw new UnauthorizedAccessException("Accès refusé.");
}

    public async Task<BesoinDTO> CreateAsync(CreateBesoinDTO dto, int utilisateurId, string roleCode)
    {
        // Vérifier que la catégorie existe
        _ = await besoinsRepo.GetCategorieByIdAsync(dto.CategorieId)
            ?? throw new KeyNotFoundException($"Catégorie {dto.CategorieId} introuvable.");

        // Vérifier que le circuit de la catégorie ne contient pas le rôle de l'utilisateur
        // (un utilisateur ne peut pas créer un besoin qu'il devra lui-même valider)
        var detail = await besoinsRepo.GetCategorieWithCircuitAsync(dto.CategorieId);
        if (detail?.WorkflowCircuit?.Etapes is not null)
        {
            var rolesCircuit = detail.WorkflowCircuit.Etapes
                .Select(e => e.RoleRequis?.Trim().ToUpperInvariant())
                .Where(r => r is not null)
                .ToHashSet();

            if (rolesCircuit.Contains(roleCode.Trim().ToUpperInvariant()))
                throw new InvalidOperationException(
                    $"Vous ne pouvez pas créer un besoin dans cette catégorie car votre rôle « {roleCode} » fait partie du circuit de validation.");
        }

        var besoin = new Besoin
        {
            Titre = dto.Titre,
            Description = dto.Description,
            NiveauImportance = dto.NiveauImportance,
            CategorieId = dto.CategorieId,
            UtilisateurId = utilisateurId,
            Statut = WorkflowEngine.BROUILLON,
            DateCreation = DateTime.UtcNow,
            DateModification = DateTime.UtcNow
        };

        var created = await besoinsRepo.CreateAsync(besoin);

        await besoinsRepo.AddHistoriqueAsync(new Historique
        {
            BesoinId = created.Id,
            Action = "CREATION",
            Description = $"Besoin créé : {created.Titre}",
            DateAction = DateTime.UtcNow
        });

        await besoinsHubService.NotifierHistoriqueAsync(created.Id, "CREATION", $"Besoin créé : {created.Titre}");

        return ToDTO(await besoinsRepo.GetByIdAsync(created.Id) ?? created);
    }

    public async Task<BesoinDTO> UpdateAsync(int id, UpdateBesoinDTO dto, int utilisateurId)
    {
        var besoin = await besoinsRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Besoin {id} introuvable.");

        if (besoin.Statut != WorkflowEngine.BROUILLON)
            throw new InvalidOperationException("Seuls les besoins en statut BROUILLON peuvent être modifiés.");

        if (besoin.UtilisateurId != utilisateurId)
            throw new UnauthorizedAccessException("Vous ne pouvez modifier que vos propres besoins.");

        var changes = new List<string>();
        if (dto.Titre is not null && dto.Titre != besoin.Titre)
        {
            changes.Add($"Titre: {besoin.Titre} → {dto.Titre}"); besoin.Titre = dto.Titre;
        }
        if (dto.Description is not null && dto.Description != besoin.Description) { changes.Add("Description modifiée"); besoin.Description = dto.Description; }
        if (dto.NiveauImportance is not null && dto.NiveauImportance != besoin.NiveauImportance) { changes.Add($"Importance: {besoin.NiveauImportance} → {dto.NiveauImportance}"); besoin.NiveauImportance = dto.NiveauImportance; }
        if (dto.CategorieId.HasValue && dto.CategorieId.Value != besoin.CategorieId) { changes.Add($"CategorieId: {besoin.CategorieId} → {dto.CategorieId.Value}"); besoin.CategorieId = dto.CategorieId.Value; }
        besoin.DateModification = DateTime.UtcNow;

        await besoinsRepo.UpdateAsync(besoin);

        if (changes.Count > 0)
        {
            var description = string.Join(", ", changes);
            await besoinsRepo.AddHistoriqueAsync(new Historique
            {
                BesoinId = id,
                Action = "MODIFICATION",
                Description = description,
                DateAction = DateTime.UtcNow
            });
            
            await besoinsHubService.NotifierHistoriqueAsync(id, "MODIFICATION", description);
        }

        return ToDTO(await besoinsRepo.GetByIdAsync(id) ?? besoin);
    }

    public async Task<BesoinDTO> EnregistrerAsync(int id, int utilisateurId)
    {
        var besoin = await besoinsRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Besoin {id} introuvable.");

        if (besoin.UtilisateurId != utilisateurId)
            throw new UnauthorizedAccessException("Vous ne pouvez enregistrer que vos propres besoins.");

        if (besoin.Statut != WorkflowEngine.BROUILLON)
            throw new InvalidOperationException($"Seuls les besoins en statut BROUILLON peuvent être enregistrés. Statut actuel : {besoin.Statut}.");

        besoin.Statut = WorkflowEngine.ENREGISTRE;
        besoin.DateModification = DateTime.UtcNow;
        await besoinsRepo.UpdateAsync(besoin);

        await besoinsRepo.AddHistoriqueAsync(new Historique
        {
            BesoinId = id,
            Action = "ENREGISTREMENT",
            Description = "Besoin enregistré et prêt à être soumis.",
            DateAction = DateTime.UtcNow
        });

        await besoinsHubService.NotifierHistoriqueAsync(id, "ENREGISTREMENT", "Besoin enregistré et prêt à être soumis.");
        await besoinsHubService.NotifierStatutBesoinAsync(id, WorkflowEngine.ENREGISTRE);

        return ToDTO(await besoinsRepo.GetByIdAsync(id) ?? besoin);
    }

    public async Task<BesoinDTO> SoumettreAsync(int id, int utilisateurId)
    {
        var besoin = await besoinsRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Besoin {id} introuvable.");

        if (besoin.UtilisateurId != utilisateurId)
            throw new UnauthorizedAccessException("Vous ne pouvez soumettre que vos propres besoins.");

        if (besoin.Statut != WorkflowEngine.ENREGISTRE)
            throw new InvalidOperationException($"Le besoin doit être enregistré avant d'être soumis. Statut actuel : {besoin.Statut}.");

        var premiereEtape = besoin.Categorie?.WorkflowCircuit?.Etapes
            ?.OrderBy(e => e.Ordre).FirstOrDefault();
        besoin.Statut = WorkflowEngine.StatutEnAttente(premiereEtape?.RoleRequis?.ToString() ?? "ROLE1");
        besoin.EtapeCouranteOrdre = 1;
        besoin.DateModification = DateTime.UtcNow;
        besoin.DateEntreeEnAttente = DateTime.UtcNow; // initialiser le tracking deadline
        besoin.Rappel1Envoye = false;
        besoin.Rappel2Envoye = false;
        besoin.EmailRappelEnvoye = false;
        besoin.RejeteAutomatiquement = false;
        await besoinsRepo.UpdateAsync(besoin);

        await besoinsRepo.AddHistoriqueAsync(new Historique
        {
            BesoinId = id,
            Action = "SOUMISSION",
            Description = "Besoin soumis, en attente de prise en charge.",
            DateAction = DateTime.UtcNow
        });

        await besoinsHubService.NotifierHistoriqueAsync(id, "SOUMISSION", "Besoin soumis, en attente de prise en charge.");
        await besoinsHubService.NotifierStatutBesoinAsync(id, besoin.Statut);

        // Récupérer le rôle de la première étape du circuit pour notifier les bons utilisateurs
        var roleEtape1 = besoin.Categorie?.WorkflowCircuit?.Etapes
            ?.OrderBy(e => e.Ordre)
            .FirstOrDefault()?.RoleRequis ?? "Responsable";

        var nomSoumetteur = besoin.Utilisateur is not null
            ? $"{besoin.Utilisateur.Prenom} {besoin.Utilisateur.Nom}"
            : "Utilisateur";

        await notificationService.NotifierSoumissionAsync(id, besoin.Titre, utilisateurId, roleEtape1, nomSoumetteur);
        return ToDTO(await besoinsRepo.GetByIdAsync(id) ?? besoin);
    }

    public async Task SupprimerDocumentAsync(int besoinId, int documentId)
    {
        var besoin = await besoinsRepo.GetByIdAsync(besoinId)
            ?? throw new KeyNotFoundException($"Besoin {besoinId} introuvable.");

        if (besoin.Statut != WorkflowEngine.BROUILLON)
            throw new InvalidOperationException("Impossible de supprimer un document une fois le besoin enregistré.");

        var doc = await besoinsRepo.GetDocumentByIdAsync(documentId)
            ?? throw new KeyNotFoundException($"Document {documentId} introuvable.");
        if (doc.BesoinId != besoinId)
            throw new KeyNotFoundException("Document non associé à ce besoin.");
        await besoinsRepo.DeleteDocumentAsync(documentId);
        
        var description = $"Document '{doc.Nom}' supprimé.";
        await besoinsRepo.AddHistoriqueAsync(new Historique
        {
            BesoinId = besoinId,
            Action = "SUPPRESSION_DOCUMENT",
            Description = description,
            DateAction = DateTime.UtcNow
        });
        
        await besoinsHubService.NotifierHistoriqueAsync(besoinId, "SUPPRESSION_DOCUMENT", description);
    }

    public async Task<IEnumerable<DocumentDTO>> GetDocumentsAsync(int id, int utilisateurId, string roleCode)
    {
        // Vérifier que l'utilisateur a accès au besoin (créateur, validateur, ou ayant déjà validé)
        _ = await GetByIdAsync(id, utilisateurId, roleCode);
        
        var docs = await besoinsRepo.GetDocumentsAsync(id);
        return docs.Select(d => new DocumentDTO
        {
            Id = d.Id, Nom = d.Nom, Type = d.Type, Checksum = d.Checksum, DateCreation = d.DateCreation
        });
    }

    public async Task<(byte[] contenu, string nom, string type)> GetDocumentContenuAsync(int besoinId, int documentId, int utilisateurId, string roleCode)
    {
        // Vérifier que l'utilisateur a accès au besoin (créateur, validateur, ou ayant déjà validé)
        _ = await GetByIdAsync(besoinId, utilisateurId, roleCode);
        
        var doc = await besoinsRepo.GetDocumentByIdAsync(documentId)
            ?? throw new KeyNotFoundException($"Document {documentId} introuvable.");
        if (doc.BesoinId != besoinId)
            throw new KeyNotFoundException("Document non associé à ce besoin.");
        return (doc.Contenu, doc.Nom, doc.Type);
    }

    public async Task<DocumentDTO> AjouterPieceJointeAsync(int id, string nom, string type, byte[] contenu)
    {
        var besoin = await besoinsRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Besoin {id} introuvable.");

        if (besoin.Statut != WorkflowEngine.BROUILLON)
            throw new InvalidOperationException("Impossible d'ajouter un document une fois le besoin enregistré.");

        var checksum = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(contenu));

        var doc = new Document
        {
            BesoinId = id,
            Nom = nom,
            Type = type,
            Contenu = contenu,
            Checksum = checksum,
            DateCreation = DateTime.UtcNow
        };

        await besoinsRepo.AddDocumentAsync(doc);

        var description = $"Document ajouté : {nom} ({type})";
        await besoinsRepo.AddHistoriqueAsync(new Historique
        {
            BesoinId = id,
            Action = "PIECE_JOINTE",
            Description = description,
            DateAction = DateTime.UtcNow
        });

        await besoinsHubService.NotifierHistoriqueAsync(id, "PIECE_JOINTE", description);

        return new DocumentDTO
        {
            Id = doc.Id,
            Nom = doc.Nom,
            Type = doc.Type,
            Checksum = doc.Checksum,
            DateCreation = doc.DateCreation
        };
    }

    public async Task<IEnumerable<HistoriqueDTO>> GetHistoriqueAsync(int id)
    {
        _ = await besoinsRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Besoin {id} introuvable.");

        var historiques = await besoinsRepo.GetHistoriqueAsync(id);
        return historiques.Select(h => new HistoriqueDTO
        {
            Id = h.Id,
            Action = h.Action,
            Description = h.Description,
            DateAction = h.DateAction
        });
    }

    public async Task<IEnumerable<CategorieDTO>> GetAllCategoriesAsync()
    {
        var cats = await besoinsRepo.GetAllCategoriesAsync();
        return cats.Select(ToCategorieDTO);
    }

    public async Task<CategorieDetailDTO> GetCategorieByIdAsync(int id)
    {
        var cat = await besoinsRepo.GetCategorieWithCircuitAsync(id)
            ?? throw new KeyNotFoundException($"Catégorie {id} introuvable.");

        var dto = new CategorieDetailDTO
        {
            Id = cat.Id,
            Nom = cat.Nom,
            Description = cat.Description,
            DateCreation = cat.DateCreation,
            WorkflowCircuitId = cat.WorkflowCircuitId,
            WorkflowCircuitNom = cat.WorkflowCircuit?.Nom
        };

        if (cat.WorkflowCircuit is not null)
        {
            dto.Circuit = new WorkflowCircuitDTO
            {
                Id = cat.WorkflowCircuit.Id,
                Nom = cat.WorkflowCircuit.Nom,
                Description = cat.WorkflowCircuit.Description,
                NomCreateur = cat.WorkflowCircuit.NomCreateur,
                DateCreation = cat.WorkflowCircuit.DateCreation,
                DateModification = cat.WorkflowCircuit.DateModification,
                Etapes = cat.WorkflowCircuit.Etapes.OrderBy(e => e.Ordre).Select(e => new EtapeCircuitDTO
                {
                    Id = e.Id,
                    Ordre = e.Ordre,
                    RoleRequis = e.RoleRequis,
                    ApprobationRequise = e.ApprobationRequise,
                    SignatureRequise = e.SignatureRequise,
                    DelaiMaxJours = e.DelaiMaxJours,
                    EstDerniereEtape = e.EstDerniereEtape,
                    StatutApres = e.SignatureRequise
                        ? WorkflowEngine.StatutSigne(e.RoleRequis ?? "")
                        : (e.EstDerniereEtape ? WorkflowEngine.TERMINE : WorkflowEngine.TRANSMIS)
                }).ToList()
            };
        }

        return dto;
    }

    public async Task<CategorieDTO> CreateCategorieAsync(CreateCategorieDTO dto)
    {
        if (await besoinsRepo.CategorieNomExistsAsync(dto.Nom))
            throw new InvalidOperationException($"Une catégorie nommée '{dto.Nom}' existe déjà.");

        // Vérifier que le circuit existe
        var circuit = await workflowRepo.GetCircuitByIdAsync(dto.WorkflowCircuitId)
            ?? throw new KeyNotFoundException(
                $"Circuit {dto.WorkflowCircuitId} introuvable. Créez d'abord un circuit via POST /api/workflow/circuits.");

        var cat = await besoinsRepo.CreateCategorieAsync(new Categorie
        {
            Nom = dto.Nom,
            Description = dto.Description,
            WorkflowCircuitId = dto.WorkflowCircuitId,
            DateCreation = DateTime.UtcNow
        });
        return ToCategorieDTO(cat);
    }

    public async Task<CategorieDTO> AssignerCircuitAsync(int categorieId, int workflowCircuitId)
    {
        var categorie = await besoinsRepo.GetCategorieByIdAsync(categorieId)
            ?? throw new KeyNotFoundException($"Catégorie {categorieId} introuvable.");

        _ = await workflowRepo.GetCircuitByIdAsync(workflowCircuitId)
            ?? throw new KeyNotFoundException($"Circuit {workflowCircuitId} introuvable.");

        categorie.WorkflowCircuitId = workflowCircuitId;
        await besoinsRepo.UpdateCategorieAsync(categorie);

        var updated = await besoinsRepo.GetCategorieWithCircuitAsync(categorieId) ?? categorie;
        return ToCategorieDTO(updated);
    }

    public async Task<CategorieDTO> UpdateCategorieAsync(int id, UpdateCategorieDTO dto)
    {
        var categorie = await besoinsRepo.GetCategorieByIdAsync(id)
            ?? throw new KeyNotFoundException($"Catégorie {id} introuvable.");

        if (dto.Nom is not null && dto.Nom != categorie.Nom)
        {
            if (await besoinsRepo.CategorieNomExistsAsync(dto.Nom, id))
                throw new InvalidOperationException($"Une catégorie nommée '{dto.Nom}' existe déjà.");
            categorie.Nom = dto.Nom;
        }

        if (dto.Description is not null)
            categorie.Description = dto.Description;

        if (dto.WorkflowCircuitId.HasValue)
        {
            _ = await workflowRepo.GetCircuitByIdAsync(dto.WorkflowCircuitId.Value)
                ?? throw new KeyNotFoundException($"Circuit {dto.WorkflowCircuitId} introuvable.");
            categorie.WorkflowCircuitId = dto.WorkflowCircuitId;
        }

        await besoinsRepo.UpdateCategorieAsync(categorie);
        var updated = await besoinsRepo.GetCategorieWithCircuitAsync(id) ?? categorie;
        return ToCategorieDTO(updated);
    }

    public async Task DeleteCategorieAsync(int id)
    {
        var categorie = await besoinsRepo.GetCategorieByIdAsync(id)
            ?? throw new KeyNotFoundException($"Catégorie {id} introuvable.");

        var besoins = await besoinsRepo.GetAllAsync();
        if (besoins.Any(b => b.CategorieId == id))
            throw new InvalidOperationException(
                "Impossible de supprimer cette catégorie : des besoins y sont rattachés.");

        await besoinsRepo.DeleteCategorieAsync(categorie);
    }

    public async Task DeleteAsync(int id)
    {
        var besoin = await besoinsRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Besoin {id} introuvable.");

        if (WorkflowEngine.EstEnAttente(besoin.Statut) ||
            WorkflowEngine.EstApprouve(besoin.Statut) ||
            WorkflowEngine.EstSigne(besoin.Statut))
            throw new InvalidOperationException(
                $"Impossible de supprimer un besoin en cours de validation (statut : {besoin.Statut}).");

        await besoinsRepo.DeleteBesoinAsync(besoin);
    }

    public async Task<IEnumerable<DeadlineBesoinDTO>> GetDeadlinesAsync(int utilisateurId, string roleCode)
    {
        var tousBesoins = (await besoinsRepo.GetAllAsync()).ToList();

        var enAttente = tousBesoins.Where(b => WorkflowEngine.EstEnAttente(b.Statut)).ToList();

        // L'admin voit tout, les autres voient uniquement les besoins en attente de leur rôle
        if (!roleCode.Equals("Administrateur", StringComparison.OrdinalIgnoreCase))
        {
            var statutAttendu = WorkflowEngine.StatutEnAttente(roleCode);
            enAttente = enAttente.Where(b => b.Statut == statutAttendu).ToList();
        }

        return enAttente.Select(b =>
        {
            var etape = b.Categorie?.WorkflowCircuit?.Etapes
                ?.FirstOrDefault(e => e.Ordre == b.EtapeCouranteOrdre);
            var delaiMin = etape?.DelaiMaxJours ?? 0;
            // Si DateEntreeEnAttente est null (besoins antérieurs à la migration), on utilise DateModification
            var dateRef = b.DateEntreeEnAttente ?? b.DateModification;
            var elapsed = delaiMin > 0
                ? (DateTime.UtcNow - dateRef).TotalMinutes
                : 0;
            var pct = delaiMin > 0 ? Math.Min((elapsed / delaiMin) * 100, 200) : 0;
            var restant = delaiMin > 0 ? Math.Max(delaiMin - elapsed, 0) : 0;

            var urgence = pct >= 100 ? "expired"
                : pct >= 80 ? "danger"
                : pct >= 50 ? "warning"
                : "normal";

            return new DeadlineBesoinDTO
            {
                Id = b.Id,
                Titre = b.Titre,
                Statut = b.Statut,
                CategorieNom = b.Categorie?.Nom ?? string.Empty,
                UtilisateurNom = b.Utilisateur is not null
                    ? $"{b.Utilisateur.Prenom} {b.Utilisateur.Nom}" : string.Empty,
                DateEntreeEnAttente = b.DateEntreeEnAttente,
                DelaiMaxMinutes = delaiMin,
                PourcentageEcoule = Math.Round(pct, 1),
                MinutesRestantes = Math.Round(restant, 1),
                Rappel1Envoye = b.Rappel1Envoye,
                Rappel2Envoye = b.Rappel2Envoye,
                EmailRappelEnvoye = b.EmailRappelEnvoye,
                RejeteAutomatiquement = b.RejeteAutomatiquement,
                EtapeRole = b.Statut.Replace("EN_ATTENTE_", ""),
                Urgence = urgence
            };
        }).ToList();
    }

    private static bool UtilisateurPeutAgirSurEtape(EtapeCircuit etape, string roleCode, HashSet<string> permissions)
    {
        if (string.IsNullOrEmpty(etape.RoleRequis)) return false;
        if (etape.RoleRequis.Equals(roleCode, StringComparison.OrdinalIgnoreCase))
            return true;

        var permissionRequise = $"VALIDER_ROLE_{etape.RoleRequis.ToUpperInvariant()}";
        return permissions.Contains(permissionRequise);
    }

    private static BesoinDTO ToDTO(Besoin b) => new()
    {
        Id = b.Id,
        Titre = b.Titre,
        Description = b.Description,
        Statut = b.Statut.ToString(),
        NiveauImportance = b.NiveauImportance,
        DateCreation = b.DateCreation,
        DateModification = b.DateModification,
        UtilisateurId = b.UtilisateurId,
        UtilisateurNom = b.Utilisateur is not null ? $"{b.Utilisateur.Prenom} {b.Utilisateur.Nom}" : string.Empty,
        CategorieId = b.CategorieId,
        CategorieNom = b.Categorie?.Nom ?? string.Empty,
        // TERMINE explicite OU SIGNE_ROLE{N} sur la dernière étape (besoins existants avant le fix)
        EstTermine = b.Statut.ToString() == WorkflowEngine.TERMINE.ToString() ||
                     (WorkflowEngine.EstSigne(b.Statut.ToString()) &&
                      b.Categorie?.WorkflowCircuit?.Etapes is not null &&
                      b.Categorie.WorkflowCircuit.Etapes
                          .Any(e => b.Statut.ToString() == WorkflowEngine.StatutSigne(e.RoleRequis?.ToString() ?? "") && e.EstDerniereEtape))
    };

    private static CategorieDTO ToCategorieDTO(Categorie c) => new()
    {
        Id = c.Id,
        Nom = c.Nom,
        Description = c.Description,
        DateCreation = c.DateCreation,
        WorkflowCircuitId = c.WorkflowCircuitId,
        WorkflowCircuitNom = c.WorkflowCircuit?.Nom
    };
}

// Constantes rôles (dupliquées ici pour éviter la dépendance API → Application)
file static class Roles
{
    public const string Administrateur = "Administrateur";
    public const string Direction = "Direction";
    public const string Responsable = "Responsable";
    public const string Agent = "Agent";
}

file class BesoinIdComparer : IEqualityComparer<Besoin>
{
    public static readonly BesoinIdComparer Instance = new();
    public bool Equals(Besoin? x, Besoin? y) => x?.Id == y?.Id;
    public int GetHashCode(Besoin obj) => obj.Id.GetHashCode();
}
