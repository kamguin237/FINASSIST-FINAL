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
/// Tests d'intégration pour la gestion des catégories via BesoinsService + Repository + InMemory DB.
/// </summary>
public class CategoriesIntegrationTests
{
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
        var wfRepo = new WorkflowRepository(db);
        var hubMock = new Mock<IBesoinsHubService>();
        hubMock.Setup(h => h.NotifierHistoriqueAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
               .Returns(Task.CompletedTask);
        return new BesoinsService(repo, wfRepo, new Mock<INotificationService>().Object, hubMock.Object);
    }

    private static async Task<WorkflowCircuit> SeedCircuitAsync(AppDbContext db)
    {
        var circuit = new WorkflowCircuit
        {
            Nom = "Circuit test",
            NomCreateur = "Admin",
            DateCreation = DateTime.UtcNow,
            DateModification = DateTime.UtcNow,
            Etapes = [new EtapeCircuit { Ordre = 1, RoleRequis = "Responsable", EstDerniereEtape = true, DelaiMaxJours = 60 }]
        };
        db.WorkflowCircuits.Add(circuit);
        await db.SaveChangesAsync();
        return circuit;
    }

    // ── CreateCategorieAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task CreateCategorieAsync_NomUnique_PersistEnBase()
    {
        // Arrange
        using var db = CreateDb(nameof(CreateCategorieAsync_NomUnique_PersistEnBase));
        var circuit = await SeedCircuitAsync(db);
        var service = CreateService(db);

        // Act
        var result = await service.CreateCategorieAsync(new CreateCategorieDTO
        {
            Nom = "Informatique",
            Description = "Catégorie IT",
            WorkflowCircuitId = circuit.Id
        });

        // Assert
        result.Should().NotBeNull();
        result.Nom.Should().Be("Informatique");
        result.WorkflowCircuitId.Should().Be(circuit.Id);

        var inDb = await db.Categories.FindAsync(result.Id);
        inDb.Should().NotBeNull();
        inDb!.Nom.Should().Be("Informatique");
    }

    [Fact]
    public async Task CreateCategorieAsync_NomDuplique_LeveInvalidOperation()
    {
        // Arrange
        using var db = CreateDb(nameof(CreateCategorieAsync_NomDuplique_LeveInvalidOperation));
        var circuit = await SeedCircuitAsync(db);
        var service = CreateService(db);

        await service.CreateCategorieAsync(new CreateCategorieDTO
        {
            Nom = "Informatique",
            WorkflowCircuitId = circuit.Id
        });

        // Act
        var act = async () => await service.CreateCategorieAsync(new CreateCategorieDTO
        {
            Nom = "Informatique",
            WorkflowCircuitId = circuit.Id
        });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Informatique*");
    }

    // ── GetAllCategoriesAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllCategoriesAsync_Retourne2Categories()
    {
        // Arrange
        using var db = CreateDb(nameof(GetAllCategoriesAsync_Retourne2Categories));
        var circuit = await SeedCircuitAsync(db);
        var service = CreateService(db);

        await service.CreateCategorieAsync(new CreateCategorieDTO { Nom = "Cat A", WorkflowCircuitId = circuit.Id });
        await service.CreateCategorieAsync(new CreateCategorieDTO { Nom = "Cat B", WorkflowCircuitId = circuit.Id });

        // Act
        var categories = (await service.GetAllCategoriesAsync()).ToList();

        // Assert
        categories.Should().HaveCount(2);
        categories.Select(c => c.Nom).Should().Contain("Cat A");
        categories.Select(c => c.Nom).Should().Contain("Cat B");
    }

    // ── UpdateCategorieAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task UpdateCategorieAsync_ModifieNom_PersisteLaModification()
    {
        // Arrange
        using var db = CreateDb(nameof(UpdateCategorieAsync_ModifieNom_PersisteLaModification));
        var circuit = await SeedCircuitAsync(db);
        var service = CreateService(db);

        var created = await service.CreateCategorieAsync(new CreateCategorieDTO
        {
            Nom = "Nom original",
            WorkflowCircuitId = circuit.Id
        });

        // Act
        var updated = await service.UpdateCategorieAsync(created.Id, new UpdateCategorieDTO
        {
            Nom = "Nom modifié"
        });

        // Assert
        updated.Nom.Should().Be("Nom modifié");

        var inDb = await db.Categories.FindAsync(created.Id);
        inDb!.Nom.Should().Be("Nom modifié");
    }

    // ── DeleteCategorieAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task DeleteCategorieAsync_SansBesoins_SupprimeDeLaBase()
    {
        // Arrange
        using var db = CreateDb(nameof(DeleteCategorieAsync_SansBesoins_SupprimeDeLaBase));
        var circuit = await SeedCircuitAsync(db);
        var service = CreateService(db);

        var created = await service.CreateCategorieAsync(new CreateCategorieDTO
        {
            Nom = "À supprimer",
            WorkflowCircuitId = circuit.Id
        });

        // Act
        await service.DeleteCategorieAsync(created.Id);

        // Assert
        var inDb = await db.Categories.FindAsync(created.Id);
        inDb.Should().BeNull();
    }

    [Fact]
    public async Task DeleteCategorieAsync_AvecBesoins_LeveInvalidOperation()
    {
        // Arrange
        using var db = CreateDb(nameof(DeleteCategorieAsync_AvecBesoins_LeveInvalidOperation));
        var circuit = await SeedCircuitAsync(db);
        var service = CreateService(db);

        var created = await service.CreateCategorieAsync(new CreateCategorieDTO
        {
            Nom = "Catégorie avec besoins",
            WorkflowCircuitId = circuit.Id
        });

        // Ajouter un rôle et utilisateur pour créer un besoin
        db.Roles.Add(new Role { Id = 1, Code = "Agent", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow });
        db.Utilisateurs.Add(new Utilisateur
        {
            Id = 10, Nom = "Test", Prenom = "User", Email = "test@finstar-cm.com",
            MotDePasse = "hash", RoleId = 1, Actif = true,
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        // Créer un besoin dans cette catégorie
        db.Besoins.Add(new Besoin
        {
            Titre = "Besoin lié", Description = "Desc", Statut = "BROUILLON",
            NiveauImportance = "MOYEN", CategorieId = created.Id, UtilisateurId = 10,
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        // Act
        var act = async () => await service.DeleteCategorieAsync(created.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*besoins*");
    }

    // ── GetCategorieByIdAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetCategorieByIdAsync_AvecCircuit_RetourneDetailComplet()
    {
        // Arrange
        using var db = CreateDb(nameof(GetCategorieByIdAsync_AvecCircuit_RetourneDetailComplet));
        var circuit = await SeedCircuitAsync(db);
        var service = CreateService(db);

        var created = await service.CreateCategorieAsync(new CreateCategorieDTO
        {
            Nom = "Catégorie détail",
            WorkflowCircuitId = circuit.Id
        });

        // Act
        var detail = await service.GetCategorieByIdAsync(created.Id);

        // Assert
        detail.Should().NotBeNull();
        detail.Nom.Should().Be("Catégorie détail");
        detail.Circuit.Should().NotBeNull();
        detail.Circuit!.Etapes.Should().HaveCount(1);
        detail.Circuit.Etapes[0].RoleRequis.Should().Be("Responsable");
    }
}
