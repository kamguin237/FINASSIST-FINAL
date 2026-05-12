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
/// Tests d'intégration pour WorkflowService.CreateCircuitAsync — validations métier
/// non couvertes dans WorkflowIntegrationTests (qui teste les cas nominaux).
/// Couvre : rôles invalides, ordres dupliqués, plusieurs dernières étapes,
/// dernière étape pas au max, circuit sans étapes, et SignatureUtilisateurService
/// validations (type invalide, image vide).
/// </summary>
public class WorkflowServiceCircuitValidationIntegrationTests
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
        var notifMock   = new Mock<INotificationService>();
        var hubMock     = new Mock<IBesoinsHubService>();
        hubMock.Setup(h => h.NotifierHistoriqueAsync(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        return new WorkflowService(wfRepo, besoinsRepo, sigRepo.Object, notifMock.Object, hubMock.Object);
    }

    private static SignatureUtilisateurService CreateSigService(AppDbContext db)
        => new(new SignatureUtilisateurRepository(db));

    private static async Task<Utilisateur> SeedUserAsync(AppDbContext db)
    {
        var role = new Role { Code = "Agent", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow };
        db.Roles.Add(role);
        var user = new Utilisateur
        {
            Nom = "Dupont", Prenom = "Jean", Email = "jean@finstar-cm.com",
            MotDePasse = "hash", RoleId = role.Id, Actif = true,
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow
        };
        db.Utilisateurs.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CreateCircuitAsync — validations métier
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateCircuitAsync_SansEtapes_LeveArgumentException()
    {
        // Arrange
        using var db = CreateDb(nameof(CreateCircuitAsync_SansEtapes_LeveArgumentException));
        var service = CreateService(db);

        // Act
        var act = async () => await service.CreateCircuitAsync(new CreateWorkflowCircuitDTO
        {
            Nom = "Circuit vide",
            Etapes = [] // aucune étape
        }, "Admin");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*étape*");
    }

    [Fact]
    public async Task CreateCircuitAsync_OrdresDupliques_LeveArgumentException()
    {
        // Arrange
        using var db = CreateDb(nameof(CreateCircuitAsync_OrdresDupliques_LeveArgumentException));
        var service = CreateService(db);

        // Act
        var act = async () => await service.CreateCircuitAsync(new CreateWorkflowCircuitDTO
        {
            Nom = "Circuit ordres dupliqués",
            Etapes =
            [
                new CreateEtapeCircuitDTO { Ordre = 1, RoleRequis = "Responsable", DelaiMaxJours = 60 },
                new CreateEtapeCircuitDTO { Ordre = 1, RoleRequis = "Direction",   DelaiMaxJours = 60, EstDerniereEtape = true } // ordre 1 dupliqué
            ]
        }, "Admin");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*uniques*");
    }

    [Fact]
    public async Task CreateCircuitAsync_PlusieursEtapesDernieres_LeveArgumentException()
    {
        // Arrange
        using var db = CreateDb(nameof(CreateCircuitAsync_PlusieursEtapesDernieres_LeveArgumentException));
        var service = CreateService(db);

        // Act
        var act = async () => await service.CreateCircuitAsync(new CreateWorkflowCircuitDTO
        {
            Nom = "Circuit multi-dernières",
            Etapes =
            [
                new CreateEtapeCircuitDTO { Ordre = 1, RoleRequis = "Responsable", DelaiMaxJours = 60, EstDerniereEtape = true },
                new CreateEtapeCircuitDTO { Ordre = 2, RoleRequis = "Direction",   DelaiMaxJours = 60, EstDerniereEtape = true } // deux dernières
            ]
        }, "Admin");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Exactement une*");
    }

    [Fact]
    public async Task CreateCircuitAsync_AucuneEtapeDerniere_LeveArgumentException()
    {
        // Arrange
        using var db = CreateDb(nameof(CreateCircuitAsync_AucuneEtapeDerniere_LeveArgumentException));
        var service = CreateService(db);

        // Act
        var act = async () => await service.CreateCircuitAsync(new CreateWorkflowCircuitDTO
        {
            Nom = "Circuit sans dernière",
            Etapes =
            [
                new CreateEtapeCircuitDTO { Ordre = 1, RoleRequis = "Responsable", DelaiMaxJours = 60, EstDerniereEtape = false },
                new CreateEtapeCircuitDTO { Ordre = 2, RoleRequis = "Direction",   DelaiMaxJours = 60, EstDerniereEtape = false }
            ]
        }, "Admin");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Exactement une*");
    }

    [Fact]
    public async Task CreateCircuitAsync_DerniereEtapePasAuMaxOrdre_LeveArgumentException()
    {
        // Arrange
        using var db = CreateDb(nameof(CreateCircuitAsync_DerniereEtapePasAuMaxOrdre_LeveArgumentException));
        var service = CreateService(db);

        // Act — la dernière étape est à l'ordre 1, mais l'ordre max est 2
        var act = async () => await service.CreateCircuitAsync(new CreateWorkflowCircuitDTO
        {
            Nom = "Circuit mauvais ordre",
            Etapes =
            [
                new CreateEtapeCircuitDTO { Ordre = 1, RoleRequis = "Responsable", DelaiMaxJours = 60, EstDerniereEtape = true },
                new CreateEtapeCircuitDTO { Ordre = 2, RoleRequis = "Direction",   DelaiMaxJours = 60, EstDerniereEtape = false }
            ]
        }, "Admin");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*ordre le plus élevé*");
    }

    [Fact]
    public async Task CreateCircuitAsync_RoleInvalide_LeveArgumentException()
    {
        // Arrange
        using var db = CreateDb(nameof(CreateCircuitAsync_RoleInvalide_LeveArgumentException));
        var service = CreateService(db);

        // Act
        var act = async () => await service.CreateCircuitAsync(new CreateWorkflowCircuitDTO
        {
            Nom = "Circuit rôle invalide",
            Etapes =
            [
                new CreateEtapeCircuitDTO { Ordre = 1, RoleRequis = "RoleInexistant", DelaiMaxJours = 60, EstDerniereEtape = true }
            ]
        }, "Admin");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*invalides*");
    }

    [Theory]
    [InlineData("Responsable")]
    [InlineData("Direction")]
    [InlineData("Administrateur")]
    [InlineData("Agent")]
    public async Task CreateCircuitAsync_RolesValides_PersistentCorrectement(string role)
    {
        // Arrange
        using var db = CreateDb($"{nameof(CreateCircuitAsync_RolesValides_PersistentCorrectement)}_{role}");
        var service = CreateService(db);

        // Act
        var result = await service.CreateCircuitAsync(new CreateWorkflowCircuitDTO
        {
            Nom = $"Circuit {role}",
            Etapes = [new CreateEtapeCircuitDTO { Ordre = 1, RoleRequis = role, DelaiMaxJours = 60, EstDerniereEtape = true }]
        }, "Admin");

        // Assert
        result.Should().NotBeNull();
        result.Etapes.Should().HaveCount(1);
        result.Etapes[0].RoleRequis.Should().Be(role);
    }

    [Fact]
    public async Task CreateCircuitAsync_NomDuplique_LeveInvalidOperation()
    {
        // Arrange — préfixe pour éviter le conflit avec WorkflowIntegrationTests qui a le même nom de méthode
        using var db = CreateDb($"Validation_{nameof(CreateCircuitAsync_NomDuplique_LeveInvalidOperation)}");
        var service = CreateService(db);

        await service.CreateCircuitAsync(new CreateWorkflowCircuitDTO
        {
            Nom = "Circuit Unique",
            Etapes = [new CreateEtapeCircuitDTO { Ordre = 1, RoleRequis = "Responsable", DelaiMaxJours = 60, EstDerniereEtape = true }]
        }, "Admin");

        // Act
        var act = async () => await service.CreateCircuitAsync(new CreateWorkflowCircuitDTO
        {
            Nom = "Circuit Unique",
            Etapes = [new CreateEtapeCircuitDTO { Ordre = 1, RoleRequis = "Direction", DelaiMaxJours = 60, EstDerniereEtape = true }]
        }, "Admin");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Circuit Unique*");
    }

    [Fact]
    public async Task CreateCircuitAsync_NomCreateur_PersisteDansLeCircuit()
    {
        // Arrange
        using var db = CreateDb(nameof(CreateCircuitAsync_NomCreateur_PersisteDansLeCircuit));
        var service = CreateService(db);

        // Act
        var result = await service.CreateCircuitAsync(new CreateWorkflowCircuitDTO
        {
            Nom = "Circuit Admin",
            Etapes = [new CreateEtapeCircuitDTO { Ordre = 1, RoleRequis = "Responsable", DelaiMaxJours = 60, EstDerniereEtape = true }]
        }, "Jean Dupont");

        // Assert
        result.NomCreateur.Should().Be("Jean Dupont");

        var inDb = await db.WorkflowCircuits.FindAsync(result.Id);
        inDb!.NomCreateur.Should().Be("Jean Dupont");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // UpdateCircuitAsync — validations
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateCircuitAsync_NomDuplique_LeveInvalidOperation()
    {
        // Arrange
        using var db = CreateDb(nameof(UpdateCircuitAsync_NomDuplique_LeveInvalidOperation));
        var service = CreateService(db);

        await service.CreateCircuitAsync(new CreateWorkflowCircuitDTO
        {
            Nom = "Circuit A",
            Etapes = [new CreateEtapeCircuitDTO { Ordre = 1, RoleRequis = "Responsable", DelaiMaxJours = 60, EstDerniereEtape = true }]
        }, "Admin");

        var circuitB = await service.CreateCircuitAsync(new CreateWorkflowCircuitDTO
        {
            Nom = "Circuit B",
            Etapes = [new CreateEtapeCircuitDTO { Ordre = 1, RoleRequis = "Direction", DelaiMaxJours = 60, EstDerniereEtape = true }]
        }, "Admin");

        // Act — renommer B en A (déjà pris)
        var act = async () => await service.UpdateCircuitAsync(circuitB.Id, new UpdateWorkflowCircuitDTO
        {
            Nom = "Circuit A"
        });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Circuit A*");
    }

    [Fact]
    public async Task UpdateCircuitAsync_MemeNom_NeLancePasException()
    {
        // Arrange
        using var db = CreateDb(nameof(UpdateCircuitAsync_MemeNom_NeLancePasException));
        var service = CreateService(db);

        var circuit = await service.CreateCircuitAsync(new CreateWorkflowCircuitDTO
        {
            Nom = "Circuit Test",
            Etapes = [new CreateEtapeCircuitDTO { Ordre = 1, RoleRequis = "Responsable", DelaiMaxJours = 60, EstDerniereEtape = true }]
        }, "Admin");

        // Act — mettre à jour avec le même nom (doit être autorisé)
        var act = async () => await service.UpdateCircuitAsync(circuit.Id, new UpdateWorkflowCircuitDTO
        {
            Nom = "Circuit Test",
            Description = "Nouvelle description"
        });

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task UpdateCircuitAsync_CircuitInexistant_LeveKeyNotFound()
    {
        // Arrange
        using var db = CreateDb(nameof(UpdateCircuitAsync_CircuitInexistant_LeveKeyNotFound));
        var service = CreateService(db);

        // Act
        var act = async () => await service.UpdateCircuitAsync(999, new UpdateWorkflowCircuitDTO { Nom = "Test" });

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SignatureUtilisateurService — validations
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SauvegarderAsync_TypeInvalide_LeveArgumentException()
    {
        // Arrange
        using var db = CreateDb(nameof(SauvegarderAsync_TypeInvalide_LeveArgumentException));
        var user = await SeedUserAsync(db);
        var service = CreateSigService(db);

        // Act
        var act = async () => await service.SauvegarderAsync(user.Id, new Core.DTOs.Signatures.SaveSignatureUtilisateurDTO
        {
            Type = "type_invalide",
            ImageBase64 = "data:image/png;base64,abc"
        });

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*invalide*");
    }

    [Fact]
    public async Task SauvegarderAsync_ImageVide_LeveArgumentException()
    {
        // Arrange
        using var db = CreateDb(nameof(SauvegarderAsync_ImageVide_LeveArgumentException));
        var user = await SeedUserAsync(db);
        var service = CreateSigService(db);

        // Act
        var act = async () => await service.SauvegarderAsync(user.Id, new Core.DTOs.Signatures.SaveSignatureUtilisateurDTO
        {
            Type = "manuscrite",
            ImageBase64 = "" // vide
        });

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*obligatoire*");
    }

    [Fact]
    public async Task SauvegarderAsync_ImageEspacesSeuls_LeveArgumentException()
    {
        // Arrange
        using var db = CreateDb(nameof(SauvegarderAsync_ImageEspacesSeuls_LeveArgumentException));
        var user = await SeedUserAsync(db);
        var service = CreateSigService(db);

        // Act
        var act = async () => await service.SauvegarderAsync(user.Id, new Core.DTOs.Signatures.SaveSignatureUtilisateurDTO
        {
            Type = "manuscrite",
            ImageBase64 = "   " // espaces seulement
        });

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*obligatoire*");
    }

    [Fact]
    public async Task SauvegarderAsync_AvecPolice_PersisteLaPolice()
    {
        // Arrange
        using var db = CreateDb(nameof(SauvegarderAsync_AvecPolice_PersisteLaPolice));
        var user = await SeedUserAsync(db);
        var service = CreateSigService(db);

        // Act
        var result = await service.SauvegarderAsync(user.Id, new Core.DTOs.Signatures.SaveSignatureUtilisateurDTO
        {
            Type = "typographique",
            ImageBase64 = "data:image/png;base64,abc",
            Police = "Dancing Script"
        });

        // Assert
        result.Police.Should().Be("Dancing Script");
        result.Type.Should().Be("typographique");
    }

    [Fact]
    public async Task SauvegarderAsync_DateCreationEtModificationInitialisees()
    {
        // Arrange
        using var db = CreateDb(nameof(SauvegarderAsync_DateCreationEtModificationInitialisees));
        var user = await SeedUserAsync(db);
        var service = CreateSigService(db);

        var avant = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var result = await service.SauvegarderAsync(user.Id, new Core.DTOs.Signatures.SaveSignatureUtilisateurDTO
        {
            Type = "manuscrite",
            ImageBase64 = "data:image/png;base64,abc"
        });

        // Assert
        result.DateCreation.Should().BeAfter(avant);
        result.DateModification.Should().BeAfter(avant);
    }
}
