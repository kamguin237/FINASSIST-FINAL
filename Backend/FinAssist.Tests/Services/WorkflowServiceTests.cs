using FinAssist.Application.Services;
using FinAssist.Core.DTOs.Workflow;
using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;
using FluentAssertions;
using Moq;

namespace FinAssist.Tests.Services;

/// <summary>
/// Tests unitaires pour WorkflowService — validation, transmission et gestion des circuits.
/// </summary>
public class WorkflowServiceTests
{
    // ── Setup ─────────────────────────────────────────────────────────────────

    private readonly Mock<IWorkflowRepository>  _wfRepoMock    = new();
    private readonly Mock<IBesoinsRepository>   _besoinsRepoMock = new();
    private readonly Mock<ISignatureRepository> _sigRepoMock   = new();
    private readonly Mock<INotificationService> _notifMock     = new();
    private readonly Mock<IBesoinsHubService>   _hubMock       = new();

    private WorkflowService CreateService() =>
        new(_wfRepoMock.Object, _besoinsRepoMock.Object, _sigRepoMock.Object,
            _notifMock.Object, _hubMock.Object);

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static EtapeCircuit Etape(int ordre, string role, bool signature = false, bool derniere = false) =>
        new() { Id = ordre, Ordre = ordre, RoleRequis = role, SignatureRequise = signature, EstDerniereEtape = derniere, ApprobationRequise = true, DelaiMaxJours = 7 };

    private static WorkflowCircuit CircuitDeuxEtapes() => new()
    {
        Id = 1, Nom = "Circuit test",
        Etapes = [Etape(1, "Responsable"), Etape(2, "Direction", derniere: true)]
    };

    private static Categorie CategorieAvecCircuit(WorkflowCircuit circuit) => new()
    {
        Id = 1, Nom = "Informatique", WorkflowCircuitId = circuit.Id, WorkflowCircuit = circuit
    };

    private Besoin BesoinEnAttente(string statut = "EN_ATTENTE_RESPONSABLE", int userId = 10) => new()
    {
        Id = 1, Titre = "Test", Description = "Desc", Statut = statut,
        UtilisateurId = userId, CategorieId = 1, EtapeCouranteOrdre = 1,
        DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow
    };

    private void SetupCircuit(WorkflowCircuit circuit)
    {
        var categorie = CategorieAvecCircuit(circuit);
        _besoinsRepoMock.Setup(r => r.GetCategorieWithCircuitAsync(1)).ReturnsAsync(categorie);
        _wfRepoMock.Setup(r => r.GetCircuitByIdAsync(circuit.Id)).ReturnsAsync(circuit);
    }

    private void SetupValidationMocks(int besoinId, int validateurId)
    {
        _besoinsRepoMock.Setup(r => r.GetCodesPermissionsUtilisateurAsync(validateurId))
            .ReturnsAsync(new List<string>());
        _besoinsRepoMock.Setup(r => r.UtilisateurADejaValideAsync(besoinId, validateurId))
            .ReturnsAsync(false);
        _wfRepoMock.Setup(r => r.AddValidationAsync(It.IsAny<Validation>())).ReturnsAsync(new Validation());
        _besoinsRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Besoin>())).ReturnsAsync(new Besoin());
        _besoinsRepoMock.Setup(r => r.AddHistoriqueAsync(It.IsAny<Historique>())).Returns(Task.CompletedTask);
        _hubMock.Setup(h => h.NotifierHistoriqueAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        _notifMock.Setup(n => n.NotifierRejetAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        _notifMock.Setup(n => n.NotifierTransmissionAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
    }

    // ── ValiderAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ValiderAsync_Approbation_AvanceEtapeSuivante()
    {
        // Arrange
        var circuit = CircuitDeuxEtapes();
        var besoin = BesoinEnAttente();
        SetupCircuit(circuit);
        SetupValidationMocks(1, 20);
        _besoinsRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(besoin);

        var service = CreateService();

        // Act
        var result = await service.ValiderAsync(1,
            new ValiderBesoinDTO { Decision = "APPROUVE" },
            validateurId: 20, roleCode: "Responsable", nomValidateur: "Jean Dupont");

        // Assert
        result.Decision.Should().Be("APPROUVE");
        _besoinsRepoMock.Verify(r => r.UpdateAsync(It.Is<Besoin>(b =>
            b.Statut == "EN_ATTENTE_DIRECTION" && b.EtapeCouranteOrdre == 2)), Times.Once);
    }

    [Fact]
    public async Task ValiderAsync_Rejet_StatutRejete()
    {
        // Arrange
        var circuit = CircuitDeuxEtapes();
        var besoin = BesoinEnAttente();
        SetupCircuit(circuit);
        SetupValidationMocks(1, 20);
        _besoinsRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(besoin);

        var service = CreateService();

        // Act
        var result = await service.ValiderAsync(1,
            new ValiderBesoinDTO { Decision = "REJETE", Motif = "Non conforme" },
            validateurId: 20, roleCode: "Responsable", nomValidateur: "Jean Dupont");

        // Assert
        result.Decision.Should().Be("REJETE");
        _besoinsRepoMock.Verify(r => r.UpdateAsync(It.Is<Besoin>(b =>
            b.Statut == "REJETE_PAR_RESPONSABLE")), Times.Once);
    }

    [Fact]
    public async Task ValiderAsync_CreateurTenteDautoValider_LeveUnauthorized()
    {
        // Arrange
        var besoin = BesoinEnAttente(userId: 20); // même userId que le validateur
        _besoinsRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(besoin);
        var service = CreateService();

        // Act
        var act = async () => await service.ValiderAsync(1,
            new ValiderBesoinDTO { Decision = "APPROUVE" },
            validateurId: 20, roleCode: "Responsable", nomValidateur: "Jean");

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*créé*");
    }

    [Fact]
    public async Task ValiderAsync_AdministrateurTenteDeValider_LeveUnauthorized()
    {
        // Arrange
        var besoin = BesoinEnAttente(userId: 10);
        _besoinsRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(besoin);
        var service = CreateService();

        // Act
        var act = async () => await service.ValiderAsync(1,
            new ValiderBesoinDTO { Decision = "APPROUVE" },
            validateurId: 99, roleCode: "Administrateur", nomValidateur: "Admin");

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Administrateur*");
    }

    [Fact]
    public async Task ValiderAsync_RejetSansMotif_LeveArgumentException()
    {
        // Arrange
        var circuit = CircuitDeuxEtapes();
        var besoin = BesoinEnAttente();
        SetupCircuit(circuit);
        _besoinsRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(besoin);
        _besoinsRepoMock.Setup(r => r.GetCodesPermissionsUtilisateurAsync(20)).ReturnsAsync(new List<string>());
        _besoinsRepoMock.Setup(r => r.UtilisateurADejaValideAsync(1, 20)).ReturnsAsync(false);
        var service = CreateService();

        // Act
        var act = async () => await service.ValiderAsync(1,
            new ValiderBesoinDTO { Decision = "REJETE", Motif = "" },
            validateurId: 20, roleCode: "Responsable", nomValidateur: "Jean");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*motif*");
    }

    [Fact]
    public async Task ValiderAsync_DoubleValidation_LeveInvalidOperation()
    {
        // Arrange
        var circuit = CircuitDeuxEtapes();
        var besoin = BesoinEnAttente();
        SetupCircuit(circuit);
        _besoinsRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(besoin);
        _besoinsRepoMock.Setup(r => r.GetCodesPermissionsUtilisateurAsync(20)).ReturnsAsync(new List<string>());
        _besoinsRepoMock.Setup(r => r.UtilisateurADejaValideAsync(1, 20)).ReturnsAsync(true); // déjà validé
        var service = CreateService();

        // Act
        var act = async () => await service.ValiderAsync(1,
            new ValiderBesoinDTO { Decision = "APPROUVE" },
            validateurId: 20, roleCode: "Responsable", nomValidateur: "Jean");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*double validation*");
    }

    [Fact]
    public async Task ValiderAsync_MauvaisRole_LeveUnauthorized()
    {
        // Arrange
        var circuit = CircuitDeuxEtapes();
        var besoin = BesoinEnAttente(); // EN_ATTENTE_RESPONSABLE
        SetupCircuit(circuit);
        _besoinsRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(besoin);
        _besoinsRepoMock.Setup(r => r.GetCodesPermissionsUtilisateurAsync(20)).ReturnsAsync(new List<string>());
        _besoinsRepoMock.Setup(r => r.UtilisateurADejaValideAsync(1, 20)).ReturnsAsync(false);
        var service = CreateService();

        // Act — Direction tente de valider une étape Responsable
        var act = async () => await service.ValiderAsync(1,
            new ValiderBesoinDTO { Decision = "APPROUVE" },
            validateurId: 20, roleCode: "Direction", nomValidateur: "Jean");

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Responsable*");
    }

    // ── CreateCircuitAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateCircuitAsync_CircuitValide_CreeSansErreur()
    {
        // Arrange
        var dto = new CreateWorkflowCircuitDTO
        {
            Nom = "Nouveau circuit",
            Etapes =
            [
                new CreateEtapeCircuitDTO { Ordre = 1, RoleRequis = "Responsable", DelaiMaxJours = 60 },
                new CreateEtapeCircuitDTO { Ordre = 2, RoleRequis = "Direction", DelaiMaxJours = 60, EstDerniereEtape = true }
            ]
        };

        _wfRepoMock.Setup(r => r.CircuitNomExistsAsync("Nouveau circuit")).ReturnsAsync(false);
        _wfRepoMock.Setup(r => r.CreateCircuitAsync(It.IsAny<WorkflowCircuit>()))
            .ReturnsAsync(new WorkflowCircuit { Id = 1, Nom = "Nouveau circuit", Etapes = [] });

        var service = CreateService();

        // Act
        var result = await service.CreateCircuitAsync(dto, "Admin");

        // Assert
        result.Nom.Should().Be("Nouveau circuit");
        _wfRepoMock.Verify(r => r.CreateCircuitAsync(It.IsAny<WorkflowCircuit>()), Times.Once);
    }

    [Fact]
    public async Task CreateCircuitAsync_NomDuplique_LeveInvalidOperation()
    {
        // Arrange
        _wfRepoMock.Setup(r => r.CircuitNomExistsAsync("Existant")).ReturnsAsync(true);
        var service = CreateService();

        // Act
        var act = async () => await service.CreateCircuitAsync(
            new CreateWorkflowCircuitDTO { Nom = "Existant", Etapes = [] }, "Admin");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Existant*");
    }

    [Fact]
    public async Task CreateCircuitAsync_SansEtapes_LeveArgumentException()
    {
        // Arrange
        _wfRepoMock.Setup(r => r.CircuitNomExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        var service = CreateService();

        // Act
        var act = async () => await service.CreateCircuitAsync(
            new CreateWorkflowCircuitDTO { Nom = "Circuit vide", Etapes = [] }, "Admin");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*étape*");
    }

    [Fact]
    public async Task CreateCircuitAsync_OrdresDupliques_LeveArgumentException()
    {
        // Arrange
        _wfRepoMock.Setup(r => r.CircuitNomExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        var service = CreateService();

        var dto = new CreateWorkflowCircuitDTO
        {
            Nom = "Circuit",
            Etapes =
            [
                new CreateEtapeCircuitDTO { Ordre = 1, RoleRequis = "Responsable", DelaiMaxJours = 60 },
                new CreateEtapeCircuitDTO { Ordre = 1, RoleRequis = "Direction", DelaiMaxJours = 60, EstDerniereEtape = true } // ordre dupliqué
            ]
        };

        // Act
        var act = async () => await service.CreateCircuitAsync(dto, "Admin");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*uniques*");
    }

    [Fact]
    public async Task CreateCircuitAsync_PlusieursDerniereEtape_LeveArgumentException()
    {
        // Arrange
        _wfRepoMock.Setup(r => r.CircuitNomExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        var service = CreateService();

        var dto = new CreateWorkflowCircuitDTO
        {
            Nom = "Circuit",
            Etapes =
            [
                new CreateEtapeCircuitDTO { Ordre = 1, RoleRequis = "Responsable", DelaiMaxJours = 60, EstDerniereEtape = true },
                new CreateEtapeCircuitDTO { Ordre = 2, RoleRequis = "Direction", DelaiMaxJours = 60, EstDerniereEtape = true }
            ]
        };

        // Act
        var act = async () => await service.CreateCircuitAsync(dto, "Admin");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Exactement une*");
    }

    [Fact]
    public async Task CreateCircuitAsync_RoleInvalide_LeveArgumentException()
    {
        // Arrange
        _wfRepoMock.Setup(r => r.CircuitNomExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        var service = CreateService();

        var dto = new CreateWorkflowCircuitDTO
        {
            Nom = "Circuit",
            Etapes =
            [
                new CreateEtapeCircuitDTO { Ordre = 1, RoleRequis = "RoleInexistant", DelaiMaxJours = 60, EstDerniereEtape = true }
            ]
        };

        // Act
        var act = async () => await service.CreateCircuitAsync(dto, "Admin");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*RoleInexistant*");
    }

    // ── DeleteCircuitAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteCircuitAsync_CircuitUtilise_LeveInvalidOperation()
    {
        // Arrange
        var circuit = new WorkflowCircuit { Id = 1, Nom = "Circuit", Etapes = [] };
        _wfRepoMock.Setup(r => r.GetCircuitByIdAsync(1)).ReturnsAsync(circuit);
        _besoinsRepoMock.Setup(r => r.GetAllCategoriesAsync())
            .ReturnsAsync(new List<Categorie> { new() { Id = 1, Nom = "Cat", WorkflowCircuitId = 1 } });

        var service = CreateService();

        // Act
        var act = async () => await service.DeleteCircuitAsync(1);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*catégories*");
    }

    [Fact]
    public async Task DeleteCircuitAsync_CircuitNonUtilise_SupprimeSansErreur()
    {
        // Arrange
        var circuit = new WorkflowCircuit { Id = 1, Nom = "Circuit", Etapes = [] };
        _wfRepoMock.Setup(r => r.GetCircuitByIdAsync(1)).ReturnsAsync(circuit);
        _besoinsRepoMock.Setup(r => r.GetAllCategoriesAsync()).ReturnsAsync(new List<Categorie>());
        _wfRepoMock.Setup(r => r.DeleteCircuitAsync(It.IsAny<WorkflowCircuit>())).Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        await service.DeleteCircuitAsync(1);

        // Assert
        _wfRepoMock.Verify(r => r.DeleteCircuitAsync(It.IsAny<WorkflowCircuit>()), Times.Once);
    }
}
