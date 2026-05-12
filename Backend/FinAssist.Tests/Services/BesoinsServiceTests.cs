using FinAssist.Application.Services;
using FinAssist.Core.DTOs.Besoins;
using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;
using FluentAssertions;
using Moq;

namespace FinAssist.Tests.Services;

/// <summary>
/// Tests unitaires pour BesoinsService — les dépendances sont mockées.
/// </summary>
public class BesoinsServiceTests
{
    // ── Setup ─────────────────────────────────────────────────────────────────

    private readonly Mock<IBesoinsRepository>  _repoMock    = new();
    private readonly Mock<IWorkflowRepository> _wfRepoMock  = new();
    private readonly Mock<INotificationService> _notifMock  = new();
    private readonly Mock<IBesoinsHubService>  _hubMock     = new();

    private BesoinsService CreateService() =>
        new(_repoMock.Object, _wfRepoMock.Object, _notifMock.Object, _hubMock.Object);

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Besoin BesoinBrouillon(int id = 1, int userId = 10) => new()
    {
        Id = id, Titre = "Test besoin", Description = "Desc",
        Statut = WorkflowEngine.BROUILLON, UtilisateurId = userId,
        NiveauImportance = "NORMAL", CategorieId = 1,
        DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow
    };

    private static Besoin BesoinEnregistre(int id = 1, int userId = 10) => new()
    {
        Id = id, Titre = "Test besoin", Description = "Desc",
        Statut = WorkflowEngine.ENREGISTRE, UtilisateurId = userId,
        NiveauImportance = "NORMAL", CategorieId = 1,
        DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow,
        Categorie = new Categorie
        {
            Id = 1, Nom = "Informatique",
            WorkflowCircuit = new WorkflowCircuit
            {
                Id = 1, Nom = "Circuit test",
                Etapes = [new EtapeCircuit { Id = 1, Ordre = 1, RoleRequis = "Responsable", EstDerniereEtape = true }]
            }
        }
    };

    private static Categorie CategorieAvecCircuit() => new()
    {
        Id = 1, Nom = "Informatique",
        WorkflowCircuitId = 1,
        WorkflowCircuit = new WorkflowCircuit
        {
            Id = 1, Nom = "Circuit test",
            Etapes = [new EtapeCircuit { Id = 1, Ordre = 1, RoleRequis = "Responsable", EstDerniereEtape = true }]
        }
    };

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_CategorieExistante_CreeBesoinEtRetourneDTO()
    {
        // Arrange
        var dto = new CreateBesoinDTO { Titre = "Nouveau besoin", Description = "Desc", NiveauImportance = "NORMAL", CategorieId = 1 };
        var besoinCree = BesoinBrouillon();

        _repoMock.Setup(r => r.GetCategorieByIdAsync(1)).ReturnsAsync(CategorieAvecCircuit());
        _repoMock.Setup(r => r.GetCategorieWithCircuitAsync(1)).ReturnsAsync(CategorieAvecCircuit());
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Besoin>())).ReturnsAsync(besoinCree);
        _repoMock.Setup(r => r.AddHistoriqueAsync(It.IsAny<Historique>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.GetByIdAsync(besoinCree.Id)).ReturnsAsync(besoinCree);
        _hubMock.Setup(h => h.NotifierHistoriqueAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        var service = CreateService();

        // Act — rôle "Agent" ne fait pas partie du circuit (qui requiert "Responsable")
        var result = await service.CreateAsync(dto, utilisateurId: 10, roleCode: "Agent");

        // Assert
        result.Should().NotBeNull();
        result.Titre.Should().Be("Test besoin");
        result.Statut.Should().Be(WorkflowEngine.BROUILLON);
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<Besoin>()), Times.Once);
        _repoMock.Verify(r => r.AddHistoriqueAsync(It.Is<Historique>(h => h.Action == "CREATION")), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_CategorieInexistante_LeveKeyNotFoundException()
    {
        // Arrange
        _repoMock.Setup(r => r.GetCategorieByIdAsync(99)).ReturnsAsync((Categorie?)null);
        var service = CreateService();

        // Act
        var act = async () => await service.CreateAsync(
            new CreateBesoinDTO { Titre = "X", Description = "X", NiveauImportance = "NORMAL", CategorieId = 99 }, 10, "Agent");

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*99*");
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_BesoinBrouillon_MettreAJourTitre()
    {
        // Arrange
        var besoin = BesoinBrouillon();
        var dto = new UpdateBesoinDTO { Titre = "Titre modifié" };

        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(besoin);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Besoin>())).ReturnsAsync(besoin);
        _repoMock.Setup(r => r.AddHistoriqueAsync(It.IsAny<Historique>())).Returns(Task.CompletedTask);
        _hubMock.Setup(h => h.NotifierHistoriqueAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        var result = await service.UpdateAsync(1, dto, utilisateurId: 10);

        // Assert
        _repoMock.Verify(r => r.UpdateAsync(It.Is<Besoin>(b => b.Titre == "Titre modifié")), Times.Once);
        _repoMock.Verify(r => r.AddHistoriqueAsync(It.Is<Historique>(h => h.Action == "MODIFICATION")), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_BesoinNonBrouillon_LeveInvalidOperation()
    {
        // Arrange
        var besoin = BesoinBrouillon();
        besoin.Statut = WorkflowEngine.ENREGISTRE;

        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(besoin);
        var service = CreateService();

        // Act
        var act = async () => await service.UpdateAsync(1, new UpdateBesoinDTO { Titre = "X" }, 10);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*BROUILLON*");
    }

    [Fact]
    public async Task UpdateAsync_AutreUtilisateur_LeveUnauthorized()
    {
        // Arrange
        var besoin = BesoinBrouillon(userId: 10);
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(besoin);
        var service = CreateService();

        // Act — userId 99 tente de modifier le besoin de userId 10
        var act = async () => await service.UpdateAsync(1, new UpdateBesoinDTO { Titre = "X" }, utilisateurId: 99);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    // ── EnregistrerAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task EnregistrerAsync_BesoinBrouillon_PasseEnEnregistre()
    {
        // Arrange
        var besoin = BesoinBrouillon();
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(besoin);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Besoin>())).ReturnsAsync(besoin);
        _repoMock.Setup(r => r.AddHistoriqueAsync(It.IsAny<Historique>())).Returns(Task.CompletedTask);
        _hubMock.Setup(h => h.NotifierHistoriqueAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        await service.EnregistrerAsync(1, utilisateurId: 10);

        // Assert
        _repoMock.Verify(r => r.UpdateAsync(It.Is<Besoin>(b => b.Statut == WorkflowEngine.ENREGISTRE)), Times.Once);
        _repoMock.Verify(r => r.AddHistoriqueAsync(It.Is<Historique>(h => h.Action == "ENREGISTREMENT")), Times.Once);
    }

    [Fact]
    public async Task EnregistrerAsync_BesoinDejaEnregistre_LeveInvalidOperation()
    {
        // Arrange
        var besoin = BesoinBrouillon();
        besoin.Statut = WorkflowEngine.ENREGISTRE;
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(besoin);
        var service = CreateService();

        // Act
        var act = async () => await service.EnregistrerAsync(1, 10);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*BROUILLON*");
    }

    // ── SoumettreAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task SoumettreAsync_BesoinEnregistre_PasseEnAttente()
    {
        // Arrange
        var besoin = BesoinEnregistre();
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(besoin);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Besoin>())).ReturnsAsync(besoin);
        _repoMock.Setup(r => r.AddHistoriqueAsync(It.IsAny<Historique>())).Returns(Task.CompletedTask);
        _hubMock.Setup(h => h.NotifierHistoriqueAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        _notifMock.Setup(n => n.NotifierSoumissionAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        await service.SoumettreAsync(1, utilisateurId: 10);

        // Assert
        _repoMock.Verify(r => r.UpdateAsync(It.Is<Besoin>(b =>
            b.Statut.StartsWith("EN_ATTENTE_") && b.EtapeCouranteOrdre == 1)), Times.Once);
        _repoMock.Verify(r => r.AddHistoriqueAsync(It.Is<Historique>(h => h.Action == "SOUMISSION")), Times.Once);
    }

    [Fact]
    public async Task SoumettreAsync_BesoinNonEnregistre_LeveInvalidOperation()
    {
        // Arrange
        var besoin = BesoinBrouillon(); // statut BROUILLON, pas ENREGISTRE
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(besoin);
        var service = CreateService();

        // Act
        var act = async () => await service.SoumettreAsync(1, 10);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*enregistré*");
    }

    // ── GetAllCategoriesAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllCategoriesAsync_RetourneListeCategories()
    {
        // Arrange
        var categories = new List<Categorie>
        {
            new() { Id = 1, Nom = "Informatique", DateCreation = DateTime.UtcNow },
            new() { Id = 2, Nom = "RH", DateCreation = DateTime.UtcNow }
        };
        _repoMock.Setup(r => r.GetAllCategoriesAsync()).ReturnsAsync(categories);
        var service = CreateService();

        // Act
        var result = (await service.GetAllCategoriesAsync()).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Select(c => c.Nom).Should().Contain(["Informatique", "RH"]);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_BesoinEnAttente_LeveInvalidOperation()
    {
        // Arrange
        var besoin = BesoinBrouillon();
        besoin.Statut = "EN_ATTENTE_RESPONSABLE";
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(besoin);
        var service = CreateService();

        // Act
        var act = async () => await service.DeleteAsync(1);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*validation*");
    }

    [Fact]
    public async Task DeleteAsync_BesoinBrouillon_SupprimeSansErreur()
    {
        // Arrange
        var besoin = BesoinBrouillon();
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(besoin);
        _repoMock.Setup(r => r.DeleteBesoinAsync(It.IsAny<Besoin>())).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        await service.DeleteAsync(1);

        // Assert
        _repoMock.Verify(r => r.DeleteBesoinAsync(It.IsAny<Besoin>()), Times.Once);
    }
}
