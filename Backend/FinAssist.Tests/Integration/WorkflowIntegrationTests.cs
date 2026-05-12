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
/// Tests d'intégration pour WorkflowService + WorkflowRepository + AppDbContext (InMemory).
/// </summary>
public class WorkflowIntegrationTests
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
        var wfRepo = new WorkflowRepository(db);
        var besoinsRepo = new BesoinsRepository(db);
        var sigRepo = new Mock<ISignatureRepository>();
        var notifMock = new Mock<INotificationService>();
        var hubMock = new Mock<IBesoinsHubService>();
        hubMock.Setup(h => h.NotifierHistoriqueAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
               .Returns(Task.CompletedTask);
        notifMock.Setup(n => n.NotifierTransmissionAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                 .Returns(Task.CompletedTask);
        notifMock.Setup(n => n.NotifierRejetAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
                 .Returns(Task.CompletedTask);
        return new WorkflowService(wfRepo, besoinsRepo, sigRepo.Object, notifMock.Object, hubMock.Object);
    }

    // ── CreateCircuitAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateCircuitAsync_CircuitValide_PersistEnBase()
    {
        // Arrange
        using var db = CreateDb(nameof(CreateCircuitAsync_CircuitValide_PersistEnBase));
        var service = CreateService(db);

        var dto = new CreateWorkflowCircuitDTO
        {
            Nom = "Circuit Intégration",
            Description = "Test",
            Etapes =
            [
                new CreateEtapeCircuitDTO { Ordre = 1, RoleRequis = "Responsable", DelaiMaxJours = 60 },
                new CreateEtapeCircuitDTO { Ordre = 2, RoleRequis = "Direction", DelaiMaxJours = 60, EstDerniereEtape = true }
            ]
        };

        // Act
        var result = await service.CreateCircuitAsync(dto, "Admin");

        // Assert
        result.Should().NotBeNull();
        result.Nom.Should().Be("Circuit Intégration");
        result.Etapes.Should().HaveCount(2);

        // Vérifier en base
        var inDb = await db.WorkflowCircuits
            .Include(c => c.Etapes)
            .FirstOrDefaultAsync(c => c.Id == result.Id);
        inDb.Should().NotBeNull();
        inDb!.Etapes.Should().HaveCount(2);
        inDb.Etapes.Any(e => e.RoleRequis == "Responsable").Should().BeTrue();
    }

    [Fact]
    public async Task CreateCircuitAsync_NomDuplique_LeveInvalidOperation()
    {
        // Arrange
        using var db = CreateDb(nameof(CreateCircuitAsync_NomDuplique_LeveInvalidOperation));
        var service = CreateService(db);

        var dto = new CreateWorkflowCircuitDTO
        {
            Nom = "Circuit Unique",
            Etapes = [new CreateEtapeCircuitDTO { Ordre = 1, RoleRequis = "Responsable", DelaiMaxJours = 60, EstDerniereEtape = true }]
        };

        await service.CreateCircuitAsync(dto, "Admin");

        // Act — créer un second circuit avec le même nom
        var act = async () => await service.CreateCircuitAsync(dto, "Admin");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Circuit Unique*");
    }

    // ── GetAllCircuitsAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetAllCircuitsAsync_ApresCreation_RetourneLeCircuit()
    {
        // Arrange
        using var db = CreateDb(nameof(GetAllCircuitsAsync_ApresCreation_RetourneLeCircuit));
        var service = CreateService(db);

        await service.CreateCircuitAsync(new CreateWorkflowCircuitDTO
        {
            Nom = "Circuit A",
            Etapes = [new CreateEtapeCircuitDTO { Ordre = 1, RoleRequis = "Responsable", DelaiMaxJours = 60, EstDerniereEtape = true }]
        }, "Admin");

        await service.CreateCircuitAsync(new CreateWorkflowCircuitDTO
        {
            Nom = "Circuit B",
            Etapes = [new CreateEtapeCircuitDTO { Ordre = 1, RoleRequis = "Direction", DelaiMaxJours = 60, EstDerniereEtape = true }]
        }, "Admin");

        // Act
        var circuits = (await service.GetAllCircuitsAsync()).ToList();

        // Assert
        circuits.Should().HaveCount(2);
        circuits.Select(c => c.Nom).Should().Contain("Circuit A");
        circuits.Select(c => c.Nom).Should().Contain("Circuit B");
    }

    // ── DeleteCircuitAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteCircuitAsync_CircuitNonUtilise_SupprimeDeLaBase()
    {
        // Arrange
        using var db = CreateDb(nameof(DeleteCircuitAsync_CircuitNonUtilise_SupprimeDeLaBase));
        var service = CreateService(db);

        var circuit = await service.CreateCircuitAsync(new CreateWorkflowCircuitDTO
        {
            Nom = "Circuit à supprimer",
            Etapes = [new CreateEtapeCircuitDTO { Ordre = 1, RoleRequis = "Responsable", DelaiMaxJours = 60, EstDerniereEtape = true }]
        }, "Admin");

        // Act
        await service.DeleteCircuitAsync(circuit.Id);

        // Assert
        var inDb = await db.WorkflowCircuits.FindAsync(circuit.Id);
        inDb.Should().BeNull();
    }

    [Fact]
    public async Task DeleteCircuitAsync_CircuitUtiliseParCategorie_LeveInvalidOperation()
    {
        // Arrange
        using var db = CreateDb(nameof(DeleteCircuitAsync_CircuitUtiliseParCategorie_LeveInvalidOperation));
        var service = CreateService(db);

        var circuit = await service.CreateCircuitAsync(new CreateWorkflowCircuitDTO
        {
            Nom = "Circuit utilisé",
            Etapes = [new CreateEtapeCircuitDTO { Ordre = 1, RoleRequis = "Responsable", DelaiMaxJours = 60, EstDerniereEtape = true }]
        }, "Admin");

        // Associer une catégorie à ce circuit
        db.Categories.Add(new Categorie
        {
            Id = 1, Nom = "Catégorie liée",
            WorkflowCircuitId = circuit.Id,
            DateCreation = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        // Act
        var act = async () => await service.DeleteCircuitAsync(circuit.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*catégories*");
    }

    // ── UpdateCircuitAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateCircuitAsync_ModifieNom_PersisteLaModification()
    {
        // Arrange
        using var db = CreateDb(nameof(UpdateCircuitAsync_ModifieNom_PersisteLaModification));
        var service = CreateService(db);

        var circuit = await service.CreateCircuitAsync(new CreateWorkflowCircuitDTO
        {
            Nom = "Nom original",
            Etapes = [new CreateEtapeCircuitDTO { Ordre = 1, RoleRequis = "Responsable", DelaiMaxJours = 60, EstDerniereEtape = true }]
        }, "Admin");

        // Act
        var updated = await service.UpdateCircuitAsync(circuit.Id, new UpdateWorkflowCircuitDTO
        {
            Nom = "Nom modifié"
        });

        // Assert
        updated.Nom.Should().Be("Nom modifié");

        var inDb = await db.WorkflowCircuits.FindAsync(circuit.Id);
        inDb!.Nom.Should().Be("Nom modifié");
    }
}
