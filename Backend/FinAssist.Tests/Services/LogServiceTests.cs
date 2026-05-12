using FinAssist.Application.Services;
using FinAssist.Core.DTOs.Logs;
using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;
using FluentAssertions;
using Moq;

namespace FinAssist.Tests.Services;

/// <summary>
/// Tests unitaires pour LogService — journalisation et consultation des logs.
/// </summary>
public class LogServiceTests
{
    private readonly Mock<ILogRepository> _repoMock = new();

    private LogService CreateService() => new(_repoMock.Object);

    // ── LoggerAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task LoggerAsync_ParametresMinimaux_AjouteLog()
    {
        // Arrange
        _repoMock.Setup(r => r.AjouterAsync(It.IsAny<LogActivite>())).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        await service.LoggerAsync("CREATION", "Besoin", entiteId: 1);

        // Assert
        _repoMock.Verify(r => r.AjouterAsync(It.Is<LogActivite>(l =>
            l.Action == "CREATION" &&
            l.EntiteType == "Besoin" &&
            l.EntiteId == 1)), Times.Once);
    }

    [Fact]
    public async Task LoggerAsync_TousParametres_AjouteLogComplet()
    {
        // Arrange
        _repoMock.Setup(r => r.AjouterAsync(It.IsAny<LogActivite>())).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        await service.LoggerAsync(
            action: "MODIFICATION",
            entiteType: "Utilisateur",
            entiteId: 5,
            ancienneValeur: "ancien",
            nouvelleValeur: "nouveau",
            utilisateurId: 10,
            adresseIp: "192.168.1.1",
            systemeExploitation: "Windows",
            navigateur: "Chrome",
            pays: "Cameroun",
            ville: "Yaoundé");

        // Assert
        _repoMock.Verify(r => r.AjouterAsync(It.Is<LogActivite>(l =>
            l.Action == "MODIFICATION" &&
            l.EntiteType == "Utilisateur" &&
            l.EntiteId == 5 &&
            l.AncienneValeur == "ancien" &&
            l.NouvelleValeur == "nouveau" &&
            l.UtilisateurId == 10 &&
            l.AdresseIp == "192.168.1.1" &&
            l.SystemeExploitation == "Windows" &&
            l.Navigateur == "Chrome" &&
            l.Pays == "Cameroun" &&
            l.Ville == "Yaoundé")), Times.Once);
    }

    [Fact]
    public async Task LoggerAsync_DateEstUTC()
    {
        // Arrange
        LogActivite? captured = null;
        _repoMock.Setup(r => r.AjouterAsync(It.IsAny<LogActivite>()))
            .Callback<LogActivite>(l => captured = l)
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        await service.LoggerAsync("TEST", "Entite");

        // Assert
        captured.Should().NotBeNull();
        captured!.Date.Kind.Should().Be(DateTimeKind.Utc);
        captured.Date.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task LoggerAsync_SansParametresOptionnels_AjouteLogAvecNulls()
    {
        // Arrange
        _repoMock.Setup(r => r.AjouterAsync(It.IsAny<LogActivite>())).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        await service.LoggerAsync("SUPPRESSION", "Besoin");

        // Assert
        _repoMock.Verify(r => r.AjouterAsync(It.Is<LogActivite>(l =>
            l.EntiteId == null &&
            l.UtilisateurId == null &&
            l.AdresseIp == null &&
            l.Pays == null)), Times.Once);
    }

    // ── GetLogsAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetLogsAsync_RetourneLogsAvecNomUtilisateur()
    {
        // Arrange
        var logs = new List<LogActivite>
        {
            new()
            {
                Id = 1, Action = "CREATION", EntiteType = "Besoin", EntiteId = 5,
                Date = DateTime.UtcNow, UtilisateurId = 10,
                Utilisateur = new Utilisateur { Id = 10, Nom = "Dupont", Prenom = "Jean",
                    Email = "jean@test.com", MotDePasse = "x" }
            },
            new()
            {
                Id = 2, Action = "MODIFICATION", EntiteType = "Utilisateur",
                Date = DateTime.UtcNow, UtilisateurId = null, Utilisateur = null
            }
        };

        _repoMock.Setup(r => r.GetPagedAsync(It.IsAny<FiltreLogsDTO>()))
            .ReturnsAsync((logs, 2));

        var service = CreateService();

        // Act
        var (items, total) = await service.GetLogsAsync(new FiltreLogsDTO());

        // Assert
        total.Should().Be(2);
        var list = items.ToList();
        list.Should().HaveCount(2);
        list[0].NomUtilisateur.Should().Be("Jean Dupont");
        list[1].NomUtilisateur.Should().BeNull();
    }

    [Fact]
    public async Task GetLogsAsync_DateEstUTC()
    {
        // Arrange
        var dateTest = new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Unspecified);
        var logs = new List<LogActivite>
        {
            new() { Id = 1, Action = "TEST", EntiteType = "X", Date = dateTest }
        };

        _repoMock.Setup(r => r.GetPagedAsync(It.IsAny<FiltreLogsDTO>()))
            .ReturnsAsync((logs, 1));

        var service = CreateService();

        // Act
        var (items, _) = await service.GetLogsAsync(new FiltreLogsDTO());

        // Assert
        items.First().Date.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task GetLogsAsync_AucunLog_RetourneListeVide()
    {
        // Arrange
        _repoMock.Setup(r => r.GetPagedAsync(It.IsAny<FiltreLogsDTO>()))
            .ReturnsAsync((new List<LogActivite>(), 0));

        var service = CreateService();

        // Act
        var (items, total) = await service.GetLogsAsync(new FiltreLogsDTO());

        // Assert
        total.Should().Be(0);
        items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLogsAsync_MappeTousLesChamps()
    {
        // Arrange
        var log = new LogActivite
        {
            Id = 42, Action = "CONNEXION", EntiteType = "Auth", EntiteId = 7,
            AncienneValeur = "old", NouvelleValeur = "new",
            UtilisateurId = 3, Date = DateTime.UtcNow,
            AdresseIp = "10.0.0.1", SystemeExploitation = "Linux",
            Navigateur = "Firefox", Pays = "France", Ville = "Paris"
        };

        _repoMock.Setup(r => r.GetPagedAsync(It.IsAny<FiltreLogsDTO>()))
            .ReturnsAsync((new List<LogActivite> { log }, 1));

        var service = CreateService();

        // Act
        var (items, _) = await service.GetLogsAsync(new FiltreLogsDTO());
        var dto = items.First();

        // Assert
        dto.Id.Should().Be(42);
        dto.Action.Should().Be("CONNEXION");
        dto.EntiteType.Should().Be("Auth");
        dto.EntiteId.Should().Be(7);
        dto.AncienneValeur.Should().Be("old");
        dto.NouvelleValeur.Should().Be("new");
        dto.AdresseIp.Should().Be("10.0.0.1");
        dto.SystemeExploitation.Should().Be("Linux");
        dto.Navigateur.Should().Be("Firefox");
        dto.Pays.Should().Be("France");
        dto.Ville.Should().Be("Paris");
    }
}
