using FinAssist.Application.Services;
using FinAssist.Core.DTOs.Workflow;
using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;
using FinAssist.Infrastructure.Data;
using FinAssist.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FinAssist.Tests.Integration;

/// <summary>
/// Tests d'intégration pour WorkflowService.ValiderAsync et TransmettreAsync
/// avec de vrais circuits, besoins et validations en base InMemory.
/// Complète WorkflowIntegrationTests qui couvre uniquement la gestion des circuits.
/// </summary>
public class WorkflowServiceValidationIntegrationTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    private static WorkflowService CreateService(AppDbContext db)
    {
        var wfRepo      = new WorkflowRepository(db);
        var besoinsRepo = new BesoinsRepository(db);
        var sigRepo     = new Mock<ISignatureRepository>();
        sigRepo.Setup(s => s.GetByBesoinIdAsync(It.IsAny<int>()))
               .ReturnsAsync((SignatureElectronique?)null);
        var notifMock = new Mock<INotificationService>();
        notifMock.Setup(n => n.NotifierTransmissionAsync(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        notifMock.Setup(n => n.NotifierRejetAsync(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        var hubMock = new Mock<IBesoinsHubService>();
        hubMock.Setup(h => h.NotifierHistoriqueAsync(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        return new WorkflowService(wfRepo, besoinsRepo, sigRepo.Object, notifMock.Object, hubMock.Object);
    }

    /// <summary>
    /// Seed complet : circuit 2 étapes, agent créateur, responsable validateur, besoin EN_ATTENTE_RESPONSABLE.
    /// </summary>
    private static async Task<(Utilisateur agent, Utilisateur responsable, Besoin besoin)>
        SeedBesoinEnAttenteAsync(AppDbContext db, bool signatureRequise = false)
    {
        var roleAgent = new Role { Code = "Agent",       DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow };
        var roleResp  = new Role { Code = "Responsable", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow };
        db.Roles.AddRange(roleAgent, roleResp);

        var agent = new Utilisateur
        {
            Nom = "Dupont", Prenom = "Jean", Email = "jean@finstar-cm.com",
            MotDePasse = "hash", RoleId = roleAgent.Id, Actif = true,
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow
        };
        var responsable = new Utilisateur
        {
            Nom = "Martin", Prenom = "Paul", Email = "paul@finstar-cm.com",
            MotDePasse = "hash", RoleId = roleResp.Id, Actif = true,
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow
        };
        db.Utilisateurs.AddRange(agent, responsable);

        var circuit = new WorkflowCircuit
        {
            Nom = "Circuit Test", NomCreateur = "Admin",
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow,
            Etapes =
            [
                new EtapeCircuit
                {
                    Ordre = 1, RoleRequis = "Responsable",
                    DelaiMaxJours = 60, EstDerniereEtape = true,
                    SignatureRequise = signatureRequise
                }
            ]
        };
        db.WorkflowCircuits.Add(circuit);

        var cat = new Categorie
        {
            Nom = "Informatique", WorkflowCircuitId = circuit.Id,
            DateCreation = DateTime.UtcNow
        };
        db.Categories.Add(cat);
        await db.SaveChangesAsync();

        var besoin = new Besoin
        {
            Titre = "Besoin test", Description = "Desc",
            Statut = "EN_ATTENTE_RESPONSABLE",
            NiveauImportance = "MOYEN",
            UtilisateurId = agent.Id, CategorieId = cat.Id,
            EtapeCouranteOrdre = 1,
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow
        };
        db.Besoins.Add(besoin);
        await db.SaveChangesAsync();

        return (agent, responsable, besoin);
    }

    private static async Task<(Utilisateur agent, Utilisateur responsable, Utilisateur direction, Besoin besoin)>
        SeedBesoinMultiEtapesAsync(AppDbContext db)
    {
        var roleAgent = new Role { Code = "Agent",       DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow };
        var roleResp  = new Role { Code = "Responsable", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow };
        var roleDir   = new Role { Code = "Direction",   DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow };
        db.Roles.AddRange(roleAgent, roleResp, roleDir);

        var agent = new Utilisateur
        {
            Nom = "Dupont", Prenom = "Jean", Email = "jean@finstar-cm.com",
            MotDePasse = "hash", RoleId = roleAgent.Id, Actif = true,
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow
        };
        var responsable = new Utilisateur
        {
            Nom = "Martin", Prenom = "Paul", Email = "paul@finstar-cm.com",
            MotDePasse = "hash", RoleId = roleResp.Id, Actif = true,
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow
        };
        var direction = new Utilisateur
        {
            Nom = "Biya", Prenom = "Marc", Email = "marc@finstar-cm.com",
            MotDePasse = "hash", RoleId = roleDir.Id, Actif = true,
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow
        };
        db.Utilisateurs.AddRange(agent, responsable, direction);

        var circuit = new WorkflowCircuit
        {
            Nom = "Circuit Multi", NomCreateur = "Admin",
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow,
            Etapes =
            [
                new EtapeCircuit { Ordre = 1, RoleRequis = "Responsable", DelaiMaxJours = 60, EstDerniereEtape = false, SignatureRequise = false },
                new EtapeCircuit { Ordre = 2, RoleRequis = "Direction",   DelaiMaxJours = 60, EstDerniereEtape = true,  SignatureRequise = false }
            ]
        };
        db.WorkflowCircuits.Add(circuit);

        var cat = new Categorie { Nom = "Finance", WorkflowCircuitId = circuit.Id, DateCreation = DateTime.UtcNow };
        db.Categories.Add(cat);
        await db.SaveChangesAsync();

        var besoin = new Besoin
        {
            Titre = "Besoin multi", Description = "Desc",
            Statut = "EN_ATTENTE_RESPONSABLE",
            NiveauImportance = "ELEVE",
            UtilisateurId = agent.Id, CategorieId = cat.Id,
            EtapeCouranteOrdre = 1,
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow
        };
        db.Besoins.Add(besoin);
        await db.SaveChangesAsync();

        return (agent, responsable, direction, besoin);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ValiderAsync — approbation
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ValiderAsync_ApprobationDerniereEtape_StatutPasseATermine()
    {
        // Arrange
        using var db = CreateDb(nameof(ValiderAsync_ApprobationDerniereEtape_StatutPasseATermine));
        var (_, responsable, besoin) = await SeedBesoinEnAttenteAsync(db);
        var service = CreateService(db);

        // Act
        var result = await service.ValiderAsync(
            besoin.Id,
            new ValiderBesoinDTO { Decision = "APPROUVE" },
            validateurId: responsable.Id,
            roleCode: "Responsable",
            nomValidateur: "Paul Martin");

        // Assert
        result.Decision.Should().Be("APPROUVE");

        var besoinInDb = await db.Besoins.FindAsync(besoin.Id);
        besoinInDb!.Statut.Should().Be("TERMINE");
    }

    [Fact]
    public async Task ValiderAsync_ApprobationEtapeIntermediaire_AvanceVersEtapeSuivante()
    {
        // Arrange
        using var db = CreateDb(nameof(ValiderAsync_ApprobationEtapeIntermediaire_AvanceVersEtapeSuivante));
        var (_, responsable, _, besoin) = await SeedBesoinMultiEtapesAsync(db);
        var service = CreateService(db);

        // Act
        var result = await service.ValiderAsync(
            besoin.Id,
            new ValiderBesoinDTO { Decision = "APPROUVE" },
            validateurId: responsable.Id,
            roleCode: "Responsable",
            nomValidateur: "Paul Martin");

        // Assert — avance vers étape 2 (Direction)
        result.Decision.Should().Be("APPROUVE");

        var besoinInDb = await db.Besoins.FindAsync(besoin.Id);
        besoinInDb!.Statut.Should().Be("EN_ATTENTE_DIRECTION");
        besoinInDb.EtapeCouranteOrdre.Should().Be(2);
    }

    [Fact]
    public async Task ValiderAsync_Approbation_PersistValidationEnBase()
    {
        // Arrange
        using var db = CreateDb(nameof(ValiderAsync_Approbation_PersistValidationEnBase));
        var (_, responsable, besoin) = await SeedBesoinEnAttenteAsync(db);
        var service = CreateService(db);

        // Act
        await service.ValiderAsync(
            besoin.Id,
            new ValiderBesoinDTO { Decision = "APPROUVE", Commentaire = "Conforme" },
            validateurId: responsable.Id,
            roleCode: "Responsable",
            nomValidateur: "Paul Martin");

        // Assert — validation persistée
        var validation = await db.Validations
            .FirstOrDefaultAsync(v => v.BesoinId == besoin.Id);
        validation.Should().NotBeNull();
        validation!.Decision.Should().Be(DecisionValidation.APPROUVE);
        validation.ValidateurId.Should().Be(responsable.Id);
        validation.Commentaire.Should().Be("Conforme");
    }

    [Fact]
    public async Task ValiderAsync_Approbation_AjouteHistorique()
    {
        // Arrange
        using var db = CreateDb(nameof(ValiderAsync_Approbation_AjouteHistorique));
        var (_, responsable, besoin) = await SeedBesoinEnAttenteAsync(db);
        var service = CreateService(db);

        // Act
        await service.ValiderAsync(
            besoin.Id,
            new ValiderBesoinDTO { Decision = "APPROUVE" },
            validateurId: responsable.Id,
            roleCode: "Responsable",
            nomValidateur: "Paul Martin");

        // Assert — historique créé
        var historique = await db.Historiques
            .FirstOrDefaultAsync(h => h.BesoinId == besoin.Id && h.Action.Contains("APPROUVE"));
        historique.Should().NotBeNull();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ValiderAsync — rejet
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ValiderAsync_Rejet_StatutPasseARejete()
    {
        // Arrange
        using var db = CreateDb(nameof(ValiderAsync_Rejet_StatutPasseARejete));
        var (_, responsable, besoin) = await SeedBesoinEnAttenteAsync(db);
        var service = CreateService(db);

        // Act
        var result = await service.ValiderAsync(
            besoin.Id,
            new ValiderBesoinDTO { Decision = "REJETE", Motif = "Budget insuffisant" },
            validateurId: responsable.Id,
            roleCode: "Responsable",
            nomValidateur: "Paul Martin");

        // Assert
        result.Decision.Should().Be("REJETE");

        var besoinInDb = await db.Besoins.FindAsync(besoin.Id);
        besoinInDb!.Statut.Should().Be("REJETE_PAR_RESPONSABLE");
    }

    [Fact]
    public async Task ValiderAsync_RejetSansMotif_LeveArgumentException()
    {
        // Arrange
        using var db = CreateDb(nameof(ValiderAsync_RejetSansMotif_LeveArgumentException));
        var (_, responsable, besoin) = await SeedBesoinEnAttenteAsync(db);
        var service = CreateService(db);

        // Act
        var act = async () => await service.ValiderAsync(
            besoin.Id,
            new ValiderBesoinDTO { Decision = "REJETE" }, // pas de motif
            validateurId: responsable.Id,
            roleCode: "Responsable",
            nomValidateur: "Paul Martin");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*motif*");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ValiderAsync — cas d'erreur métier
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ValiderAsync_CreateurValidesonPropre_LeveUnauthorized()
    {
        // Arrange
        using var db = CreateDb(nameof(ValiderAsync_CreateurValidesonPropre_LeveUnauthorized));
        var (agent, _, besoin) = await SeedBesoinEnAttenteAsync(db);
        var service = CreateService(db);

        // Act — l'agent essaie de valider son propre besoin
        var act = async () => await service.ValiderAsync(
            besoin.Id,
            new ValiderBesoinDTO { Decision = "APPROUVE" },
            validateurId: agent.Id,
            roleCode: "Responsable",
            nomValidateur: "Jean Dupont");

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*créé*");
    }

    [Fact]
    public async Task ValiderAsync_AdministrateurValide_LeveUnauthorized()
    {
        // Arrange
        using var db = CreateDb(nameof(ValiderAsync_AdministrateurValide_LeveUnauthorized));
        var (_, responsable, besoin) = await SeedBesoinEnAttenteAsync(db);
        var service = CreateService(db);

        // Act — l'administrateur ne peut pas valider
        var act = async () => await service.ValiderAsync(
            besoin.Id,
            new ValiderBesoinDTO { Decision = "APPROUVE" },
            validateurId: responsable.Id,
            roleCode: "Administrateur",
            nomValidateur: "Admin");

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Administrateur*");
    }

    [Fact]
    public async Task ValiderAsync_MauvaisRole_LeveUnauthorized()
    {
        // Arrange
        using var db = CreateDb(nameof(ValiderAsync_MauvaisRole_LeveUnauthorized));
        var (_, responsable, besoin) = await SeedBesoinEnAttenteAsync(db);
        var service = CreateService(db);

        // Act — Direction essaie de valider une étape Responsable
        var act = async () => await service.ValiderAsync(
            besoin.Id,
            new ValiderBesoinDTO { Decision = "APPROUVE" },
            validateurId: responsable.Id,
            roleCode: "Direction",
            nomValidateur: "Paul Martin");

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Responsable*");
    }

    [Fact]
    public async Task ValiderAsync_DoubleValidation_LeveInvalidOperation()
    {
        // Arrange
        using var db = CreateDb(nameof(ValiderAsync_DoubleValidation_LeveInvalidOperation));
        var (_, responsable, _, besoin) = await SeedBesoinMultiEtapesAsync(db);
        var service = CreateService(db);

        // Première validation
        await service.ValiderAsync(
            besoin.Id,
            new ValiderBesoinDTO { Decision = "APPROUVE" },
            validateurId: responsable.Id,
            roleCode: "Responsable",
            nomValidateur: "Paul Martin");

        // Remettre le besoin en EN_ATTENTE_RESPONSABLE pour simuler une tentative de double validation
        var besoinInDb = await db.Besoins.FindAsync(besoin.Id);
        besoinInDb!.Statut = "EN_ATTENTE_RESPONSABLE";
        besoinInDb.EtapeCouranteOrdre = 1; // réinitialiser l'étape courante
        db.Besoins.Update(besoinInDb);
        await db.SaveChangesAsync();

        // Act — deuxième tentative par le même validateur
        var act = async () => await service.ValiderAsync(
            besoin.Id,
            new ValiderBesoinDTO { Decision = "APPROUVE" },
            validateurId: responsable.Id,
            roleCode: "Responsable",
            nomValidateur: "Paul Martin");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*double validation*");
    }

    [Fact]
    public async Task ValiderAsync_BesoinInexistant_LeveKeyNotFound()
    {
        // Arrange
        using var db = CreateDb(nameof(ValiderAsync_BesoinInexistant_LeveKeyNotFound));
        var service = CreateService(db);

        // Act
        var act = async () => await service.ValiderAsync(
            999,
            new ValiderBesoinDTO { Decision = "APPROUVE" },
            validateurId: 1,
            roleCode: "Responsable",
            nomValidateur: "Test");

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task ValiderAsync_DecisionInvalide_LeveArgumentException()
    {
        // Arrange
        using var db = CreateDb(nameof(ValiderAsync_DecisionInvalide_LeveArgumentException));
        var (_, responsable, besoin) = await SeedBesoinEnAttenteAsync(db);
        var service = CreateService(db);

        // Act
        var act = async () => await service.ValiderAsync(
            besoin.Id,
            new ValiderBesoinDTO { Decision = "INVALIDE" },
            validateurId: responsable.Id,
            roleCode: "Responsable",
            nomValidateur: "Paul Martin");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*APPROUVE*REJETE*");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TransmettreAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task TransmettreAsync_DepuisApprouveSansSignature_AvanceVersEtapeSuivante()
    {
        // Arrange
        using var db = CreateDb(nameof(TransmettreAsync_DepuisApprouveSansSignature_AvanceVersEtapeSuivante));
        var (_, responsable, _, besoin) = await SeedBesoinMultiEtapesAsync(db);
        var service = CreateService(db);

        // Mettre le besoin en APPROUVE_PAR_RESPONSABLE (sans signature requise)
        besoin.Statut = "APPROUVE_PAR_RESPONSABLE";
        db.Besoins.Update(besoin);
        await db.SaveChangesAsync();

        // Act
        var result = await service.TransmettreAsync(besoin.Id, responsable.Id, "Paul Martin");

        // Assert
        result.Decision.Should().Be("TRANSMIS");

        var besoinInDb = await db.Besoins.FindAsync(besoin.Id);
        besoinInDb!.Statut.Should().Be("EN_ATTENTE_DIRECTION");
        besoinInDb.EtapeCouranteOrdre.Should().Be(2);
    }

    [Fact]
    public async Task TransmettreAsync_DepuisSigneParResponsable_AvanceVersEtapeSuivante()
    {
        // Arrange
        using var db = CreateDb(nameof(TransmettreAsync_DepuisSigneParResponsable_AvanceVersEtapeSuivante));
        var (_, responsable, _, besoin) = await SeedBesoinMultiEtapesAsync(db);

        // Ajouter une signature valide pour satisfaire la vérification
        var doc = new Document
        {
            BesoinId = besoin.Id, Nom = "doc.pdf", Type = "application/pdf",
            Checksum = "abc", Contenu = [0x25, 0x50, 0x44, 0x46],
            DateCreation = DateTime.UtcNow
        };
        db.Documents.Add(doc);
        await db.SaveChangesAsync();

        db.Signatures.Add(new SignatureElectronique
        {
            DocumentId = doc.Id, UtilisateurId = responsable.Id,
            Valeur = "val", Empreinte = "hash",
            Horodatage = DateTime.UtcNow, Valide = true
        });

        // Mettre le circuit avec SignatureRequise = true pour l'étape 1
        var circuit = await db.WorkflowCircuits.Include(c => c.Etapes).FirstAsync();
        circuit.Etapes.First().SignatureRequise = true;
        db.WorkflowCircuits.Update(circuit);

        besoin.Statut = "SIGNE_PAR_RESPONSABLE";
        db.Besoins.Update(besoin);
        await db.SaveChangesAsync();

        // Créer le service avec un vrai SignatureRepository (pas un mock)
        var wfRepo      = new WorkflowRepository(db);
        var besoinsRepo = new BesoinsRepository(db);
        var sigRepo     = new SignatureRepository(db); // vrai repo pour trouver la signature
        var notifMock   = new Mock<INotificationService>();
        notifMock.Setup(n => n.NotifierTransmissionAsync(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        var hubMock = new Mock<IBesoinsHubService>();
        hubMock.Setup(h => h.NotifierHistoriqueAsync(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        var service = new WorkflowService(wfRepo, besoinsRepo, sigRepo, notifMock.Object, hubMock.Object);

        // Act
        var result = await service.TransmettreAsync(besoin.Id, responsable.Id, "Paul Martin");

        // Assert
        result.Decision.Should().Be("TRANSMIS");

        var besoinInDb = await db.Besoins.FindAsync(besoin.Id);
        besoinInDb!.Statut.Should().Be("EN_ATTENTE_DIRECTION");
    }

    [Fact]
    public async Task TransmettreAsync_DepuisEnAttente_LeveInvalidOperation()
    {
        // Arrange
        using var db = CreateDb(nameof(TransmettreAsync_DepuisEnAttente_LeveInvalidOperation));
        var (_, responsable, besoin) = await SeedBesoinEnAttenteAsync(db);
        var service = CreateService(db);

        // Act — besoin encore EN_ATTENTE, pas APPROUVE
        var act = async () => await service.TransmettreAsync(besoin.Id, responsable.Id, "Paul Martin");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*APPROUVE*SIGNE*");
    }

    [Fact]
    public async Task TransmettreAsync_AjouteHistoriqueTransmission()
    {
        // Arrange
        using var db = CreateDb(nameof(TransmettreAsync_AjouteHistoriqueTransmission));
        var (_, responsable, _, besoin) = await SeedBesoinMultiEtapesAsync(db);
        var service = CreateService(db);

        besoin.Statut = "APPROUVE_PAR_RESPONSABLE";
        db.Besoins.Update(besoin);
        await db.SaveChangesAsync();

        // Act
        await service.TransmettreAsync(besoin.Id, responsable.Id, "Paul Martin");

        // Assert — historique TRANSMISSION créé
        var historique = await db.Historiques
            .FirstOrDefaultAsync(h => h.BesoinId == besoin.Id && h.Action == "TRANSMISSION");
        historique.Should().NotBeNull();
        historique!.Description.Should().Contain("Direction");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Cycle complet : soumission → validation → terminé
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CycleComplet_SoumissionValidationTermine_StatutsCoherents()
    {
        // Arrange
        using var db = CreateDb(nameof(CycleComplet_SoumissionValidationTermine_StatutsCoherents));
        var (_, responsable, direction, besoin) = await SeedBesoinMultiEtapesAsync(db);
        var service = CreateService(db);

        // Étape 1 : Responsable approuve
        await service.ValiderAsync(
            besoin.Id,
            new ValiderBesoinDTO { Decision = "APPROUVE" },
            validateurId: responsable.Id,
            roleCode: "Responsable",
            nomValidateur: "Paul Martin");

        var apresEtape1 = await db.Besoins.FindAsync(besoin.Id);
        apresEtape1!.Statut.Should().Be("EN_ATTENTE_DIRECTION");

        // Étape 2 : Direction approuve (dernière étape)
        await service.ValiderAsync(
            besoin.Id,
            new ValiderBesoinDTO { Decision = "APPROUVE" },
            validateurId: direction.Id,
            roleCode: "Direction",
            nomValidateur: "Marc Biya");

        var apresEtape2 = await db.Besoins.FindAsync(besoin.Id);
        apresEtape2!.Statut.Should().Be("TERMINE");

        // Vérifier les 2 validations en base
        var validations = await db.Validations
            .Where(v => v.BesoinId == besoin.Id)
            .ToListAsync();
        validations.Should().HaveCount(2);
        validations.All(v => v.Decision == DecisionValidation.APPROUVE).Should().BeTrue();
    }
}
