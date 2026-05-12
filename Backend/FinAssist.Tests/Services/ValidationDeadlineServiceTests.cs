using FinAssist.Application.Services;
using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace FinAssist.Tests.Services;

/// <summary>
/// Tests unitaires pour ValidationDeadlineService — logique de rappels et rejet automatique.
/// </summary>
public class ValidationDeadlineServiceTests
{
    // ── Setup ─────────────────────────────────────────────────────────────────

    private readonly Mock<IBesoinsRepository>   _repoMock   = new();
    private readonly Mock<INotificationService> _notifMock  = new();
    private readonly Mock<IEmailService>        _emailMock  = new();
    private readonly Mock<ILogService>          _logMock    = new();
    private readonly Mock<IBesoinsHubService>   _hubMock    = new();
    private readonly Mock<ILogger<ValidationDeadlineService>> _loggerMock = new();

    private ValidationDeadlineService CreateService()
    {
        _hubMock.Setup(h => h.NotifierStatutBesoinAsync(It.IsAny<int>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
        return new(_repoMock.Object, _notifMock.Object, _emailMock.Object,
            _logMock.Object, _hubMock.Object, _loggerMock.Object);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Besoin BesoinEnAttente(
        int id = 1,
        int delaiMinutes = 100,
        double minutesEcoulees = 0,
        bool rappel1 = false,
        bool rappel2 = false,
        bool emailEnvoye = false,
        bool rejeteAuto = false)
    {
        var dateRef = DateTime.UtcNow.AddMinutes(-minutesEcoulees);
        return new Besoin
        {
            Id = id,
            Titre = $"Besoin {id}",
            Description = "Desc",
            Statut = "EN_ATTENTE_RESPONSABLE",
            UtilisateurId = 10,
            CategorieId = 1,
            EtapeCouranteOrdre = 1,
            DateEntreeEnAttente = dateRef,
            DateModification = dateRef,
            Rappel1Envoye = rappel1,
            Rappel2Envoye = rappel2,
            EmailRappelEnvoye = emailEnvoye,
            RejeteAutomatiquement = rejeteAuto,
            Categorie = new Categorie
            {
                Id = 1, Nom = "Informatique",
                WorkflowCircuit = new WorkflowCircuit
                {
                    Id = 1, Nom = "Circuit",
                    Etapes = [new EtapeCircuit { Id = 1, Ordre = 1, RoleRequis = "Responsable", DelaiMaxJours = delaiMinutes, EstDerniereEtape = true }]
                }
            }
        };
    }

    private void SetupMocks(int besoinId, int validateurId = 20)
    {
        _repoMock.Setup(r => r.GetUtilisateurIdsByRoleAsync("Responsable"))
            .ReturnsAsync(new List<int> { validateurId });
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Besoin>())).ReturnsAsync(new Besoin());
        _repoMock.Setup(r => r.AddHistoriqueAsync(It.IsAny<Historique>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.AddValidationAsync(It.IsAny<Validation>())).Returns(Task.CompletedTask);
        _notifMock.Setup(n => n.EnvoyerRappelAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        _notifMock.Setup(n => n.NotifierRejetAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        _emailMock.Setup(e => e.EnvoyerRappelDelaiAsync(It.IsAny<Utilisateur>(), It.IsAny<Besoin>())).Returns(Task.CompletedTask);
        _logMock.Setup(l => l.LoggerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>())).Returns(Task.CompletedTask);
    }

    // ── ProcessDeadlinesAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task ProcessDeadlines_AucunBesoinEnAttente_NeFaitRien()
    {
        // Arrange
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Besoin>());
        var service = CreateService();

        // Act
        await service.ProcessDeadlinesAsync();

        // Assert
        _notifMock.Verify(n => n.EnvoyerRappelAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ProcessDeadlines_BesoinDejaRejete_EstIgnore()
    {
        // Arrange — besoin déjà rejeté automatiquement
        var besoin = BesoinEnAttente(delaiMinutes: 100, minutesEcoulees: 200, rejeteAuto: true);
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Besoin> { besoin });
        var service = CreateService();

        // Act
        await service.ProcessDeadlinesAsync();

        // Assert
        _notifMock.Verify(n => n.EnvoyerRappelAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ProcessDeadlines_50PourcentEcoule_EnvoieRappel1()
    {
        // Arrange — 55 minutes écoulées sur 100 = 55%
        var besoin = BesoinEnAttente(delaiMinutes: 100, minutesEcoulees: 55);
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Besoin> { besoin });
        SetupMocks(besoin.Id);

        var service = CreateService();

        // Act
        await service.ProcessDeadlinesAsync();

        // Assert
        _notifMock.Verify(n => n.EnvoyerRappelAsync(20, 1, "Besoin 1", 1, It.IsAny<string>()), Times.Once);
        _repoMock.Verify(r => r.UpdateAsync(It.Is<Besoin>(b => b.Rappel1Envoye)), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessDeadlines_Rappel1DejaEnvoye_NePasRenvoyer()
    {
        // Arrange — rappel 1 déjà envoyé
        var besoin = BesoinEnAttente(delaiMinutes: 100, minutesEcoulees: 55, rappel1: true);
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Besoin> { besoin });
        SetupMocks(besoin.Id);

        var service = CreateService();

        // Act
        await service.ProcessDeadlinesAsync();

        // Assert — rappel 1 ne doit pas être renvoyé
        _notifMock.Verify(n => n.EnvoyerRappelAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), 1, It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ProcessDeadlines_80PourcentEcoule_EnvoieRappel2()
    {
        // Arrange — 85 minutes écoulées sur 100 = 85%
        var besoin = BesoinEnAttente(delaiMinutes: 100, minutesEcoulees: 85, rappel1: true);
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Besoin> { besoin });
        SetupMocks(besoin.Id);

        var service = CreateService();

        // Act
        await service.ProcessDeadlinesAsync();

        // Assert
        _notifMock.Verify(n => n.EnvoyerRappelAsync(20, 1, "Besoin 1", 2, It.IsAny<string>()), Times.Once);
        _repoMock.Verify(r => r.UpdateAsync(It.Is<Besoin>(b => b.Rappel2Envoye)), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessDeadlines_100PourcentEcoule_EnvoieEmail()
    {
        // Arrange — 105 minutes écoulées sur 100 = 105%
        var besoin = BesoinEnAttente(delaiMinutes: 100, minutesEcoulees: 105, rappel1: true, rappel2: true);
        var validateur = new Utilisateur { Id = 20, Nom = "Dupont", Prenom = "Jean", Email = "jean@test.com" };

        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Besoin> { besoin });
        _repoMock.Setup(r => r.GetUtilisateurIdsByRoleAsync("Responsable")).ReturnsAsync(new List<int> { 20 });
        _repoMock.Setup(r => r.GetUtilisateurByIdAsync(20)).ReturnsAsync(validateur);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Besoin>())).ReturnsAsync(new Besoin());
        _emailMock.Setup(e => e.EnvoyerRappelDelaiAsync(It.IsAny<Utilisateur>(), It.IsAny<Besoin>())).Returns(Task.CompletedTask);
        _notifMock.Setup(n => n.EnvoyerRappelAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        _notifMock.Setup(n => n.EnvoyerAlertExpirationAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        _notifMock.Setup(n => n.NotifierRejetAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.AddHistoriqueAsync(It.IsAny<Historique>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.AddValidationAsync(It.IsAny<Validation>())).Returns(Task.CompletedTask);
        _logMock.Setup(l => l.LoggerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>())).Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        await service.ProcessDeadlinesAsync();

        // Assert
        _emailMock.Verify(e => e.EnvoyerRappelDelaiAsync(
            It.Is<Utilisateur>(u => u.Id == 20),
            It.Is<Besoin>(b => b.Id == 1)), Times.Once);
        _repoMock.Verify(r => r.UpdateAsync(It.Is<Besoin>(b => b.EmailRappelEnvoye)), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessDeadlines_DelaiDepasse60Min_RejeteAutomatiquement()
    {
        // Arrange — 165 minutes écoulées sur 100 = 165% (> 100% + 60 min)
        var besoin = BesoinEnAttente(
            delaiMinutes: 100, minutesEcoulees: 165,
            rappel1: true, rappel2: true, emailEnvoye: true);

        var validateur = new Utilisateur { Id = 20, Nom = "Dupont", Prenom = "Jean", Email = "jean@test.com" };

        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Besoin> { besoin });
        _repoMock.Setup(r => r.GetUtilisateurIdsByRoleAsync("Responsable")).ReturnsAsync(new List<int> { 20 });
        _repoMock.Setup(r => r.GetUtilisateurByIdAsync(20)).ReturnsAsync(validateur);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Besoin>())).ReturnsAsync(new Besoin());
        _repoMock.Setup(r => r.AddHistoriqueAsync(It.IsAny<Historique>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.AddValidationAsync(It.IsAny<Validation>())).Returns(Task.CompletedTask);
        _notifMock.Setup(n => n.NotifierRejetAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        _notifMock.Setup(n => n.EnvoyerRappelAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        _notifMock.Setup(n => n.EnvoyerAlertExpirationAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        _emailMock.Setup(e => e.EnvoyerRappelDelaiAsync(It.IsAny<Utilisateur>(), It.IsAny<Besoin>())).Returns(Task.CompletedTask);
        _logMock.Setup(l => l.LoggerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>())).Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        await service.ProcessDeadlinesAsync();

        // Assert — le besoin doit être rejeté automatiquement
        _repoMock.Verify(r => r.UpdateAsync(It.Is<Besoin>(b =>
            b.RejeteAutomatiquement &&
            b.Statut == "REJETE_PAR_RESPONSABLE")), Times.AtLeastOnce);

        _repoMock.Verify(r => r.AddHistoriqueAsync(It.Is<Historique>(h =>
            h.Action == "REJET_AUTOMATIQUE")), Times.Once);

        _notifMock.Verify(n => n.NotifierRejetAsync(1, "Besoin 1", 10, "Responsable"), Times.Once);
    }

    [Fact]
    public async Task ProcessDeadlines_BesoinSansEtape_EstIgnore()
    {
        // Arrange — besoin sans catégorie/circuit
        var besoin = new Besoin
        {
            Id = 1, Titre = "Sans circuit", Statut = "EN_ATTENTE_RESPONSABLE",
            UtilisateurId = 10, CategorieId = 1, EtapeCouranteOrdre = 1,
            DateEntreeEnAttente = DateTime.UtcNow.AddMinutes(-200),
            DateModification = DateTime.UtcNow.AddMinutes(-200),
            Categorie = null // pas de catégorie
        };

        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Besoin> { besoin });
        var service = CreateService();

        // Act — ne doit pas lever d'exception
        var act = async () => await service.ProcessDeadlinesAsync();

        // Assert
        await act.Should().NotThrowAsync();
        _notifMock.Verify(n => n.EnvoyerRappelAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }
}
