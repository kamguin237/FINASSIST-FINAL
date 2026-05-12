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
/// Tests d'intégration pour BesoinsService + BesoinsRepository + AppDbContext (InMemory).
/// Ces tests vérifient que la couche service et la couche repository fonctionnent ensemble
/// correctement avec une vraie base de données en mémoire.
/// </summary>
public class BesoinsIntegrationTests
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
        var repo = new BesoinsRepository(db);
        var wfRepoMock = new Mock<IWorkflowRepository>();
        var notifMock = new Mock<INotificationService>();
        var hubMock = new Mock<IBesoinsHubService>();
        hubMock.Setup(h => h.NotifierHistoriqueAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
               .Returns(Task.CompletedTask);
        notifMock.Setup(n => n.NotifierSoumissionAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
                 .Returns(Task.CompletedTask);
        return new BesoinsService(repo, wfRepoMock.Object, notifMock.Object, hubMock.Object);
    }

    private static async Task SeedBaseAsync(AppDbContext db)
    {
        var role = new Role { Id = 1, Code = "Agent", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow };
        var user = new Utilisateur
        {
            Id = 10, Nom = "Dupont", Prenom = "Jean",
            Email = "jean@finstar-cm.com", MotDePasse = "hashed",
            RoleId = 1, Actif = true,
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow
        };
        var circuit = new WorkflowCircuit
        {
            Id = 1, Nom = "Circuit test", NomCreateur = "Admin",
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow,
            Etapes = [new EtapeCircuit { Ordre = 1, RoleRequis = "Responsable", EstDerniereEtape = true, DelaiMaxJours = 60 }]
        };
        var categorie = new Categorie
        {
            Id = 1, Nom = "Informatique",
            WorkflowCircuitId = 1,
            DateCreation = DateTime.UtcNow
        };

        db.Roles.Add(role);
        db.Utilisateurs.Add(user);
        db.WorkflowCircuits.Add(circuit);
        db.Categories.Add(categorie);
        await db.SaveChangesAsync();
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_CategorieExistante_PersisteBesoinEnBase()
    {
        // Arrange
        using var db = CreateDb(nameof(CreateAsync_CategorieExistante_PersisteBesoinEnBase));
        await SeedBaseAsync(db);
        var service = CreateService(db);

        // Act
        var result = await service.CreateAsync(new CreateBesoinDTO
        {
            Titre = "Besoin intégration",
            Description = "Test complet",
            NiveauImportance = "MOYEN",
            CategorieId = 1
        }, utilisateurId: 10, roleCode: "Agent");

        // Assert
        result.Should().NotBeNull();
        result.Titre.Should().Be("Besoin intégration");
        result.Statut.Should().Be("BROUILLON");

        // Vérifier en base
        var inDb = await db.Besoins.FindAsync(result.Id);
        inDb.Should().NotBeNull();
        inDb!.Titre.Should().Be("Besoin intégration");
        inDb.UtilisateurId.Should().Be(10);
    }

    [Fact]
    public async Task CreateAsync_AjouteHistoriqueCreation()
    {
        // Arrange
        using var db = CreateDb(nameof(CreateAsync_AjouteHistoriqueCreation));
        await SeedBaseAsync(db);
        var service = CreateService(db);

        // Act
        var result = await service.CreateAsync(new CreateBesoinDTO
        {
            Titre = "Test historique",
            Description = "Desc",
            NiveauImportance = "FAIBLE",
            CategorieId = 1
        }, utilisateurId: 10, roleCode: "Agent");

        // Assert — un historique CREATION doit être créé
        var historiques = await db.Historiques.Where(h => h.BesoinId == result.Id).ToListAsync();
        historiques.Should().HaveCount(1);
        historiques[0].Action.Should().Be("CREATION");
    }

    // ── EnregistrerAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task EnregistrerAsync_BesoinBrouillon_PasseEnEnregistre()
    {
        // Arrange
        using var db = CreateDb(nameof(EnregistrerAsync_BesoinBrouillon_PasseEnEnregistre));
        await SeedBaseAsync(db);
        var service = CreateService(db);

        var created = await service.CreateAsync(new CreateBesoinDTO
        {
            Titre = "Test enregistrement",
            Description = "Desc",
            NiveauImportance = "MOYEN",
            CategorieId = 1
        }, utilisateurId: 10, roleCode: "Agent");

        // Act
        var result = await service.EnregistrerAsync(created.Id, utilisateurId: 10);

        // Assert
        result.Statut.Should().Be("ENREGISTRE");

        var inDb = await db.Besoins.FindAsync(created.Id);
        inDb!.Statut.Should().Be("ENREGISTRE");
    }

    // ── GetAllCategoriesAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllCategoriesAsync_RetourneCategorieSeedee()
    {
        // Arrange
        using var db = CreateDb(nameof(GetAllCategoriesAsync_RetourneCategorieSeedee));
        await SeedBaseAsync(db);
        var service = CreateService(db);

        // Act
        var categories = (await service.GetAllCategoriesAsync()).ToList();

        // Assert
        categories.Should().HaveCount(1);
        categories[0].Nom.Should().Be("Informatique");
        categories[0].WorkflowCircuitId.Should().Be(1);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_BesoinBrouillon_SupprimeDeLaBase()
    {
        // Arrange
        using var db = CreateDb(nameof(DeleteAsync_BesoinBrouillon_SupprimeDeLaBase));
        await SeedBaseAsync(db);
        var service = CreateService(db);

        var created = await service.CreateAsync(new CreateBesoinDTO
        {
            Titre = "À supprimer",
            Description = "Desc",
            NiveauImportance = "FAIBLE",
            CategorieId = 1
        }, utilisateurId: 10, roleCode: "Agent");

        // Act
        await service.DeleteAsync(created.Id);

        // Assert
        var inDb = await db.Besoins.FindAsync(created.Id);
        inDb.Should().BeNull();
    }

    // ── GetHistoriqueAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetHistoriqueAsync_ApresCreationEtEnregistrement_Retourne2Entrees()
    {
        // Arrange
        using var db = CreateDb(nameof(GetHistoriqueAsync_ApresCreationEtEnregistrement_Retourne2Entrees));
        await SeedBaseAsync(db);
        var service = CreateService(db);

        var created = await service.CreateAsync(new CreateBesoinDTO
        {
            Titre = "Test historique complet",
            Description = "Desc",
            NiveauImportance = "MOYEN",
            CategorieId = 1
        }, utilisateurId: 10, roleCode: "Agent");

        await service.EnregistrerAsync(created.Id, utilisateurId: 10);

        // Act
        var historique = (await service.GetHistoriqueAsync(created.Id)).ToList();

        // Assert
        historique.Should().HaveCount(2);
        historique.Select(h => h.Action).Should().Contain("CREATION");
        historique.Select(h => h.Action).Should().Contain("ENREGISTREMENT");
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ModifieTitre_PersisteLaModification()
    {
        // Arrange
        using var db = CreateDb(nameof(UpdateAsync_ModifieTitre_PersisteLaModification));
        await SeedBaseAsync(db);
        var service = CreateService(db);

        var created = await service.CreateAsync(new CreateBesoinDTO
        {
            Titre = "Titre original",
            Description = "Desc",
            NiveauImportance = "MOYEN",
            CategorieId = 1
        }, utilisateurId: 10, roleCode: "Agent");

        // Act
        var updated = await service.UpdateAsync(created.Id, new UpdateBesoinDTO
        {
            Titre = "Titre modifié"
        }, utilisateurId: 10);

        // Assert
        updated.Titre.Should().Be("Titre modifié");

        var inDb = await db.Besoins.FindAsync(created.Id);
        inDb!.Titre.Should().Be("Titre modifié");
    }
}
