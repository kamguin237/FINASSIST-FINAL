using FinAssist.Application.Services;
using FinAssist.Core.DTOs.Besoins;
using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;
using FinAssist.Infrastructure.Data;
using FinAssist.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FinAssist.Tests.Integration;

/// <summary>
/// Tests d'intégration pour BesoinsService.GetAllAsync et GetByIdAsync.
/// Vérifie le filtrage par rôle : Admin voit tout, Agent voit ses besoins
/// + ceux en attente de son rôle + ceux qu'il a déjà validés.
/// Vérifie aussi le contrôle d'accès de GetByIdAsync.
/// </summary>
public class BesoinsServiceGetAllIntegrationTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    private static BesoinsService CreateService(AppDbContext db)
    {
        var repo    = new BesoinsRepository(db);
        var wfRepo  = new WorkflowRepository(db);
        var notif   = new Mock<INotificationService>();
        notif.Setup(n => n.NotifierSoumissionAsync(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        var hub = new Mock<IBesoinsHubService>();
        hub.Setup(h => h.NotifierHistoriqueAsync(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        return new BesoinsService(repo, wfRepo, notif.Object, hub.Object);
    }

    private static async Task<(Role agentRole, Role respRole, Utilisateur agent, Utilisateur resp, Categorie cat)>
        SeedBaseAsync(AppDbContext db)
    {
        var agentRole = new Role { Code = "Agent",       DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow };
        var respRole  = new Role { Code = "Responsable", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow };
        db.Roles.AddRange(agentRole, respRole);

        var agent = new Utilisateur
        {
            Nom = "Dupont", Prenom = "Jean", Email = "jean@finstar-cm.com",
            MotDePasse = "hash", RoleId = agentRole.Id, Actif = true,
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow
        };
        var resp = new Utilisateur
        {
            Nom = "Martin", Prenom = "Paul", Email = "paul@finstar-cm.com",
            MotDePasse = "hash", RoleId = respRole.Id, Actif = true,
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow
        };
        db.Utilisateurs.AddRange(agent, resp);

        var circuit = new WorkflowCircuit
        {
            Nom = "Circuit Test", NomCreateur = "Admin",
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow,
            Etapes = [new EtapeCircuit { Ordre = 1, RoleRequis = "Responsable", DelaiMaxJours = 60, EstDerniereEtape = true }]
        };
        db.WorkflowCircuits.Add(circuit);

        var cat = new Categorie { Nom = "Informatique", WorkflowCircuitId = circuit.Id, DateCreation = DateTime.UtcNow };
        db.Categories.Add(cat);
        await db.SaveChangesAsync();

        return (agentRole, respRole, agent, resp, cat);
    }

    private static Besoin MakeBesoin(int userId, int catId, string statut, string titre = "Besoin") => new()
    {
        Titre = titre, Description = "Desc", Statut = statut,
        NiveauImportance = "MOYEN", UtilisateurId = userId, CategorieId = catId,
        DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow
    };

    // ═══════════════════════════════════════════════════════════════════════════
    // GetAllAsync — Administrateur
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetAllAsync_Administrateur_VoitTousLesBesoins()
    {
        // Arrange
        using var db = CreateDb(nameof(GetAllAsync_Administrateur_VoitTousLesBesoins));
        var (_, _, agent, resp, cat) = await SeedBaseAsync(db);
        var service = CreateService(db);

        db.Besoins.AddRange(
            MakeBesoin(agent.Id, cat.Id, "BROUILLON", "Besoin Agent 1"),
            MakeBesoin(agent.Id, cat.Id, "EN_ATTENTE_RESPONSABLE", "Besoin Agent 2"),
            MakeBesoin(resp.Id, cat.Id, "TERMINE", "Besoin Resp")
        );
        await db.SaveChangesAsync();

        // Act
        var result = (await service.GetAllAsync(resp.Id, "Administrateur")).ToList();

        // Assert — admin voit les 3 besoins
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAllAsync_Administrateur_BaseVide_RetourneListeVide()
    {
        // Arrange
        using var db = CreateDb(nameof(GetAllAsync_Administrateur_BaseVide_RetourneListeVide));
        await SeedBaseAsync(db);
        var service = CreateService(db);

        // Act
        var result = (await service.GetAllAsync(1, "Administrateur")).ToList();

        // Assert
        result.Should().BeEmpty();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetAllAsync — Agent (créateur)
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetAllAsync_Agent_VoitSesPropresBesoins()
    {
        // Arrange
        using var db = CreateDb(nameof(GetAllAsync_Agent_VoitSesPropresBesoins));
        var (_, _, agent, resp, cat) = await SeedBaseAsync(db);
        var service = CreateService(db);

        db.Besoins.AddRange(
            MakeBesoin(agent.Id, cat.Id, "BROUILLON", "Mon besoin 1"),
            MakeBesoin(agent.Id, cat.Id, "ENREGISTRE", "Mon besoin 2"),
            MakeBesoin(resp.Id, cat.Id, "BROUILLON", "Besoin du resp") // pas à l'agent
        );
        await db.SaveChangesAsync();

        // Act
        var result = (await service.GetAllAsync(agent.Id, "Agent")).ToList();

        // Assert — l'agent voit ses 2 besoins, pas celui du responsable
        result.Should().HaveCount(2);
        result.All(b => b.UtilisateurId == agent.Id).Should().BeTrue();
    }

    [Fact]
    public async Task GetAllAsync_Agent_VoitAussiBesoinsEnAttenteDesonRole()
    {
        // Arrange
        using var db = CreateDb(nameof(GetAllAsync_Agent_VoitAussiBesoinsEnAttenteDesonRole));
        var (_, _, agent, resp, cat) = await SeedBaseAsync(db);

        // Créer un rôle Agent avec un circuit qui attend l'Agent
        var circuitAgent = new WorkflowCircuit
        {
            Nom = "Circuit Agent", NomCreateur = "Admin",
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow,
            Etapes = [new EtapeCircuit { Ordre = 1, RoleRequis = "Agent", DelaiMaxJours = 60, EstDerniereEtape = true }]
        };
        db.WorkflowCircuits.Add(circuitAgent);
        var catAgent = new Categorie { Nom = "Cat Agent", WorkflowCircuitId = circuitAgent.Id, DateCreation = DateTime.UtcNow };
        db.Categories.Add(catAgent);
        await db.SaveChangesAsync();

        var service = CreateService(db);

        db.Besoins.AddRange(
            MakeBesoin(agent.Id, cat.Id, "BROUILLON", "Mon brouillon"),
            MakeBesoin(resp.Id, catAgent.Id, "EN_ATTENTE_AGENT", "Besoin en attente de l'agent") // créé par resp, attend Agent
        );
        await db.SaveChangesAsync();

        // Act
        var result = (await service.GetAllAsync(agent.Id, "Agent")).ToList();

        // Assert — l'agent voit son brouillon + le besoin en attente de son rôle
        result.Should().HaveCount(2);
        result.Select(b => b.Titre).Should().Contain("Mon brouillon");
        result.Select(b => b.Titre).Should().Contain("Besoin en attente de l'agent");
    }

    [Fact]
    public async Task GetAllAsync_Responsable_VoitBesoinsEnAttenteDesonRole()
    {
        // Arrange
        using var db = CreateDb(nameof(GetAllAsync_Responsable_VoitBesoinsEnAttenteDesonRole));
        var (_, _, agent, resp, cat) = await SeedBaseAsync(db);
        var service = CreateService(db);

        db.Besoins.AddRange(
            MakeBesoin(agent.Id, cat.Id, "EN_ATTENTE_RESPONSABLE", "Besoin 1 en attente"),
            MakeBesoin(agent.Id, cat.Id, "EN_ATTENTE_RESPONSABLE", "Besoin 2 en attente"),
            MakeBesoin(agent.Id, cat.Id, "BROUILLON", "Brouillon agent") // pas visible par resp
        );
        await db.SaveChangesAsync();

        // Act
        var result = (await service.GetAllAsync(resp.Id, "Responsable")).ToList();

        // Assert — le responsable voit les 2 besoins en attente de son rôle
        result.Should().HaveCount(2);
        result.All(b => b.Statut == "EN_ATTENTE_RESPONSABLE").Should().BeTrue();
    }

    [Fact]
    public async Task GetAllAsync_Responsable_VoitAussiBesoinsDejaValides()
    {
        // Arrange
        using var db = CreateDb(nameof(GetAllAsync_Responsable_VoitAussiBesoinsDejaValides));
        var (_, _, agent, resp, cat) = await SeedBaseAsync(db);
        var service = CreateService(db);

        var besoin = MakeBesoin(agent.Id, cat.Id, "TERMINE", "Besoin terminé");
        db.Besoins.Add(besoin);
        await db.SaveChangesAsync();

        // Ajouter une validation du responsable sur ce besoin
        db.Validations.Add(new Validation
        {
            BesoinId = besoin.Id, ValidateurId = resp.Id,
            Niveau = 1, EtapeOrdre = 1,
            Decision = DecisionValidation.APPROUVE,
            StatutApres = "TERMINE",
            DateDecision = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        // Act
        var result = (await service.GetAllAsync(resp.Id, "Responsable")).ToList();

        // Assert — le responsable voit le besoin qu'il a déjà validé
        result.Should().HaveCount(1);
        result[0].Titre.Should().Be("Besoin terminé");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetByIdAsync — contrôle d'accès
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetByIdAsync_Createur_PeutAcceder()
    {
        // Arrange
        using var db = CreateDb(nameof(GetByIdAsync_Createur_PeutAcceder));
        var (_, _, agent, _, cat) = await SeedBaseAsync(db);
        var service = CreateService(db);

        var besoin = MakeBesoin(agent.Id, cat.Id, "BROUILLON", "Mon besoin");
        db.Besoins.Add(besoin);
        await db.SaveChangesAsync();

        // Act
        var result = await service.GetByIdAsync(besoin.Id, agent.Id, "Agent");

        // Assert
        result.Should().NotBeNull();
        result.Titre.Should().Be("Mon besoin");
    }

    [Fact]
    public async Task GetByIdAsync_Administrateur_PeutAccederNimporteQuelBesoin()
    {
        // Arrange
        using var db = CreateDb(nameof(GetByIdAsync_Administrateur_PeutAccederNimporteQuelBesoin));
        var (_, _, agent, resp, cat) = await SeedBaseAsync(db);
        var service = CreateService(db);

        var besoin = MakeBesoin(agent.Id, cat.Id, "BROUILLON", "Besoin privé");
        db.Besoins.Add(besoin);
        await db.SaveChangesAsync();

        // Act — le responsable en tant qu'admin peut accéder au besoin de l'agent
        var result = await service.GetByIdAsync(besoin.Id, resp.Id, "Administrateur");

        // Assert
        result.Should().NotBeNull();
        result.Titre.Should().Be("Besoin privé");
    }

    [Fact]
    public async Task GetByIdAsync_ValidateurDuBonRole_PeutAcceder()
    {
        // Arrange
        using var db = CreateDb(nameof(GetByIdAsync_ValidateurDuBonRole_PeutAcceder));
        var (_, _, agent, resp, cat) = await SeedBaseAsync(db);
        var service = CreateService(db);

        var besoin = MakeBesoin(agent.Id, cat.Id, "EN_ATTENTE_RESPONSABLE", "Besoin en attente");
        db.Besoins.Add(besoin);
        await db.SaveChangesAsync();

        // Act — le responsable peut accéder au besoin en attente de son rôle
        var result = await service.GetByIdAsync(besoin.Id, resp.Id, "Responsable");

        // Assert
        result.Should().NotBeNull();
        result.Titre.Should().Be("Besoin en attente");
    }

    [Fact]
    public async Task GetByIdAsync_UtilisateurSansAcces_LeveUnauthorized()
    {
        // Arrange
        using var db = CreateDb(nameof(GetByIdAsync_UtilisateurSansAcces_LeveUnauthorized));
        var (_, _, agent, resp, cat) = await SeedBaseAsync(db);
        var service = CreateService(db);

        // Besoin de l'agent en BROUILLON (pas en attente du responsable)
        var besoin = MakeBesoin(agent.Id, cat.Id, "BROUILLON", "Besoin privé");
        db.Besoins.Add(besoin);
        await db.SaveChangesAsync();

        // Act — le responsable essaie d'accéder au brouillon de l'agent
        var act = async () => await service.GetByIdAsync(besoin.Id, resp.Id, "Responsable");

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*refusé*");
    }

    [Fact]
    public async Task GetByIdAsync_ValidateurAyantDejaValide_PeutAcceder()
    {
        // Arrange
        using var db = CreateDb(nameof(GetByIdAsync_ValidateurAyantDejaValide_PeutAcceder));
        var (_, _, agent, resp, cat) = await SeedBaseAsync(db);
        var service = CreateService(db);

        var besoin = MakeBesoin(agent.Id, cat.Id, "TERMINE", "Besoin terminé");
        db.Besoins.Add(besoin);
        await db.SaveChangesAsync();

        // Ajouter une validation du responsable
        db.Validations.Add(new Validation
        {
            BesoinId = besoin.Id, ValidateurId = resp.Id,
            Niveau = 1, EtapeOrdre = 1,
            Decision = DecisionValidation.APPROUVE,
            StatutApres = "TERMINE",
            DateDecision = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        // Act — le responsable peut accéder au besoin qu'il a déjà validé
        var result = await service.GetByIdAsync(besoin.Id, resp.Id, "Responsable");

        // Assert
        result.Should().NotBeNull();
        result.DejaValideParMoi.Should().BeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_BesoinInexistant_LeveKeyNotFound()
    {
        // Arrange
        using var db = CreateDb(nameof(GetByIdAsync_BesoinInexistant_LeveKeyNotFound));
        var service = CreateService(db);

        // Act
        var act = async () => await service.GetByIdAsync(999, 1, "Agent");

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetHistoriqueAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetHistoriqueAsync_BesoinAvecHistorique_RetourneListe()
    {
        // Arrange
        using var db = CreateDb(nameof(GetHistoriqueAsync_BesoinAvecHistorique_RetourneListe));
        var (_, _, agent, _, cat) = await SeedBaseAsync(db);
        var service = CreateService(db);

        var created = await service.CreateAsync(new CreateBesoinDTO
        {
            Titre = "Besoin historique", Description = "Desc",
            NiveauImportance = "MOYEN", CategorieId = cat.Id
        }, agent.Id, "Agent");

        await service.EnregistrerAsync(created.Id, agent.Id);

        // Act
        var historique = (await service.GetHistoriqueAsync(created.Id)).ToList();

        // Assert — CREATION + ENREGISTREMENT
        historique.Should().HaveCount(2);
        historique.Select(h => h.Action).Should().Contain("CREATION");
        historique.Select(h => h.Action).Should().Contain("ENREGISTREMENT");
    }

    [Fact]
    public async Task GetHistoriqueAsync_BesoinInexistant_LeveKeyNotFound()
    {
        // Arrange
        using var db = CreateDb(nameof(GetHistoriqueAsync_BesoinInexistant_LeveKeyNotFound));
        var service = CreateService(db);

        // Act
        var act = async () => await service.GetHistoriqueAsync(999);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
