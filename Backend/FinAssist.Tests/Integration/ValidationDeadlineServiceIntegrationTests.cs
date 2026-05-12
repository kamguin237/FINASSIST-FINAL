using FinAssist.Application.Services;
using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;
using FinAssist.Infrastructure.Data;
using FinAssist.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FinAssist.Tests.Integration;

/// <summary>
/// Tests d'intégration pour ValidationDeadlineService + BesoinsRepository + AppDbContext (InMemory).
/// Vérifie que le service de traitement des deadlines envoie les bons rappels
/// et rejette automatiquement les besoins expirés.
/// Note : DelaiMaxJours stocke en réalité des minutes dans ce contexte de test.
/// </summary>
public class ValidationDeadlineServiceIntegrationTests
{
    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    private static (ValidationDeadlineService service, Mock<INotificationService> notifMock, Mock<IEmailService> emailMock, Mock<ILogService> logMock)
        CreateService(AppDbContext db)
    {
        var repo = new BesoinsRepository(db);
        var notifMock = new Mock<INotificationService>();
        notifMock.Setup(n => n.EnvoyerRappelAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
                 .Returns(Task.CompletedTask);
        notifMock.Setup(n => n.NotifierRejetAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
                 .Returns(Task.CompletedTask);
        var emailMock = new Mock<IEmailService>();
        emailMock.Setup(e => e.EnvoyerRappelDelaiAsync(It.IsAny<Utilisateur>(), It.IsAny<Besoin>()))
                 .Returns(Task.CompletedTask);
        var logMock = new Mock<ILogService>();
        logMock.Setup(l => l.LoggerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
               .Returns(Task.CompletedTask);

        var hubMock = new Mock<IBesoinsHubService>();
        hubMock.Setup(h => h.NotifierStatutBesoinAsync(It.IsAny<int>(), It.IsAny<string>()))
               .Returns(Task.CompletedTask);

        var service = new ValidationDeadlineService(
            repo, notifMock.Object, emailMock.Object, logMock.Object,
            hubMock.Object, NullLogger<ValidationDeadlineService>.Instance);

        return (service, notifMock, emailMock, logMock);
    }

    /// <summary>
    /// Crée un besoin en attente avec un délai déjà partiellement écoulé.
    /// delaiMinutes : délai total configuré
    /// minutesEcoulees : temps déjà passé depuis l'entrée en attente
    /// </summary>
    private static async Task<(Utilisateur validateur, Besoin besoin)> SeedBesoinEnAttenteAsync(
        AppDbContext db,
        int delaiMinutes,
        double minutesEcoulees,
        string roleCode = "Responsable")
    {
        var role = new Role { Code = roleCode, DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow };
        db.Roles.Add(role);

        var validateur = new Utilisateur
        {
            Nom = "Martin", Prenom = "Paul", Email = $"paul_{Guid.NewGuid():N}@finstar-cm.com",
            MotDePasse = "hash", RoleId = role.Id, Actif = true,
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow
        };
        db.Utilisateurs.Add(validateur);

        var circuit = new WorkflowCircuit
        {
            Nom = $"Circuit_{Guid.NewGuid():N}",
            NomCreateur = "Admin",
            DateCreation = DateTime.UtcNow,
            DateModification = DateTime.UtcNow,
            Etapes =
            [
                new EtapeCircuit
                {
                    Ordre = 1,
                    RoleRequis = roleCode,
                    DelaiMaxJours = delaiMinutes, // stocké en minutes
                    EstDerniereEtape = true
                }
            ]
        };
        db.WorkflowCircuits.Add(circuit);

        var cat = new Categorie
        {
            Nom = $"Cat_{Guid.NewGuid():N}",
            WorkflowCircuitId = circuit.Id,
            DateCreation = DateTime.UtcNow
        };
        db.Categories.Add(cat);
        await db.SaveChangesAsync();

        var besoin = new Besoin
        {
            Titre = "Besoin deadline test",
            Description = "Desc",
            Statut = $"EN_ATTENTE_{roleCode.ToUpperInvariant()}",
            NiveauImportance = "MOYEN",
            UtilisateurId = validateur.Id,
            CategorieId = cat.Id,
            EtapeCouranteOrdre = 1,
            DateEntreeEnAttente = DateTime.UtcNow.AddMinutes(-minutesEcoulees),
            DateCreation = DateTime.UtcNow.AddMinutes(-minutesEcoulees),
            DateModification = DateTime.UtcNow.AddMinutes(-minutesEcoulees),
            RejeteAutomatiquement = false,
            Rappel1Envoye = false,
            Rappel2Envoye = false,
            EmailRappelEnvoye = false
        };
        db.Besoins.Add(besoin);
        await db.SaveChangesAsync();

        return (validateur, besoin);
    }

    // ── Aucun besoin en attente ───────────────────────────────────────────────

    [Fact]
    public async Task ProcessDeadlinesAsync_AucunBesoinEnAttente_NEnvoieRienEtNeLancePasException()
    {
        // Arrange
        using var db = CreateDb(nameof(ProcessDeadlinesAsync_AucunBesoinEnAttente_NEnvoieRienEtNeLancePasException));
        var (service, notifMock, emailMock, _) = CreateService(db);

        // Act
        var act = async () => await service.ProcessDeadlinesAsync();

        // Assert
        await act.Should().NotThrowAsync();
        notifMock.Verify(n => n.EnvoyerRappelAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        emailMock.Verify(e => e.EnvoyerRappelDelaiAsync(It.IsAny<Utilisateur>(), It.IsAny<Besoin>()), Times.Never);
    }

    // ── Rappel 1 à 50% ───────────────────────────────────────────────────────

    [Fact]
    public async Task ProcessDeadlinesAsync_50PourcentEcoule_EnvoieRappel1()
    {
        // Arrange
        using var db = CreateDb(nameof(ProcessDeadlinesAsync_50PourcentEcoule_EnvoieRappel1));
        // Délai = 100 min, 55 min écoulées → 55% → doit déclencher rappel 1
        var (_, besoin) = await SeedBesoinEnAttenteAsync(db, delaiMinutes: 100, minutesEcoulees: 55);
        var (service, notifMock, _, _) = CreateService(db);

        // Act
        await service.ProcessDeadlinesAsync();

        // Assert — rappel 1 envoyé
        notifMock.Verify(n => n.EnvoyerRappelAsync(
            It.IsAny<int>(), besoin.Id, besoin.Titre, 1, It.IsAny<string>()), Times.Once);

        // Vérifier que le flag est mis à jour en base
        var inDb = await db.Besoins.FindAsync(besoin.Id);
        inDb!.Rappel1Envoye.Should().BeTrue();
        inDb.Rappel2Envoye.Should().BeFalse();
    }

    // ── Rappel 2 à 80% ───────────────────────────────────────────────────────

    [Fact]
    public async Task ProcessDeadlinesAsync_80PourcentEcoule_EnvoieRappel1Et2()
    {
        // Arrange
        using var db = CreateDb(nameof(ProcessDeadlinesAsync_80PourcentEcoule_EnvoieRappel1Et2));
        // Délai = 100 min, 85 min écoulées → 85% → rappels 1 et 2
        var (_, besoin) = await SeedBesoinEnAttenteAsync(db, delaiMinutes: 100, minutesEcoulees: 85);
        var (service, notifMock, _, _) = CreateService(db);

        // Act
        await service.ProcessDeadlinesAsync();

        // Assert — rappels 1 et 2 envoyés
        notifMock.Verify(n => n.EnvoyerRappelAsync(
            It.IsAny<int>(), besoin.Id, besoin.Titre, 1, It.IsAny<string>()), Times.Once);
        notifMock.Verify(n => n.EnvoyerRappelAsync(
            It.IsAny<int>(), besoin.Id, besoin.Titre, 2, It.IsAny<string>()), Times.Once);

        var inDb = await db.Besoins.FindAsync(besoin.Id);
        inDb!.Rappel1Envoye.Should().BeTrue();
        inDb.Rappel2Envoye.Should().BeTrue();
    }

    // ── Email à 100% ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ProcessDeadlinesAsync_100PourcentEcoule_EnvoieEmail()
    {
        // Arrange
        using var db = CreateDb(nameof(ProcessDeadlinesAsync_100PourcentEcoule_EnvoieEmail));
        // Délai = 100 min, 105 min écoulées → 105% → email envoyé
        var (validateur, besoin) = await SeedBesoinEnAttenteAsync(db, delaiMinutes: 100, minutesEcoulees: 105);
        var (service, _, emailMock, _) = CreateService(db);

        // Act
        await service.ProcessDeadlinesAsync();

        // Assert — email envoyé au validateur
        emailMock.Verify(e => e.EnvoyerRappelDelaiAsync(
            It.Is<Utilisateur>(u => u.Id == validateur.Id),
            It.Is<Besoin>(b => b.Id == besoin.Id)), Times.Once);

        var inDb = await db.Besoins.FindAsync(besoin.Id);
        inDb!.EmailRappelEnvoye.Should().BeTrue();
    }

    // ── Rejet automatique à 100% + 60 min ────────────────────────────────────

    [Fact]
    public async Task ProcessDeadlinesAsync_DelaiDepasse60Min_RejeteAutomatiquement()
    {
        // Arrange
        using var db = CreateDb(nameof(ProcessDeadlinesAsync_DelaiDepasse60Min_RejeteAutomatiquement));
        // Délai = 100 min, 165 min écoulées → 100% + 65 min → rejet automatique
        var (_, besoin) = await SeedBesoinEnAttenteAsync(db, delaiMinutes: 100, minutesEcoulees: 165);
        var (service, notifMock, _, logMock) = CreateService(db);

        // Act
        await service.ProcessDeadlinesAsync();

        // Assert — besoin rejeté automatiquement
        var inDb = await db.Besoins.FindAsync(besoin.Id);
        inDb!.RejeteAutomatiquement.Should().BeTrue();
        inDb.Statut.Should().StartWith("REJETE_PAR_");

        // Historique de rejet créé
        var historique = await db.Historiques
            .Where(h => h.BesoinId == besoin.Id && h.Action == "REJET_AUTOMATIQUE")
            .FirstOrDefaultAsync();
        historique.Should().NotBeNull();

        // Validation système créée
        var validation = await db.Validations
            .Where(v => v.BesoinId == besoin.Id && v.Decision == DecisionValidation.REJETE)
            .FirstOrDefaultAsync();
        validation.Should().NotBeNull();
        validation!.ValidateurId.Should().BeNull(); // rejet système

        // Notification envoyée au créateur
        notifMock.Verify(n => n.NotifierRejetAsync(
            besoin.Id, besoin.Titre, besoin.UtilisateurId, It.IsAny<string>()), Times.Once);
    }

    // ── Besoin déjà rejeté automatiquement — ignoré ───────────────────────────

    [Fact]
    public async Task ProcessDeadlinesAsync_BesoinDejaRejete_EstIgnore()
    {
        // Arrange
        using var db = CreateDb(nameof(ProcessDeadlinesAsync_BesoinDejaRejete_EstIgnore));
        var (_, besoin) = await SeedBesoinEnAttenteAsync(db, delaiMinutes: 100, minutesEcoulees: 200);

        // Marquer comme déjà rejeté
        besoin.RejeteAutomatiquement = true;
        db.Besoins.Update(besoin);
        await db.SaveChangesAsync();

        var (service, notifMock, emailMock, _) = CreateService(db);

        // Act
        await service.ProcessDeadlinesAsync();

        // Assert — aucune action supplémentaire
        notifMock.Verify(n => n.NotifierRejetAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        emailMock.Verify(e => e.EnvoyerRappelDelaiAsync(It.IsAny<Utilisateur>(), It.IsAny<Besoin>()), Times.Never);
    }

    // ── Rappel déjà envoyé — pas de doublon ───────────────────────────────────

    [Fact]
    public async Task ProcessDeadlinesAsync_Rappel1DejaEnvoye_NEnvoiePasDeDoublon()
    {
        // Arrange
        using var db = CreateDb(nameof(ProcessDeadlinesAsync_Rappel1DejaEnvoye_NEnvoiePasDeDoublon));
        var (_, besoin) = await SeedBesoinEnAttenteAsync(db, delaiMinutes: 100, minutesEcoulees: 55);

        // Marquer rappel 1 déjà envoyé
        besoin.Rappel1Envoye = true;
        db.Besoins.Update(besoin);
        await db.SaveChangesAsync();

        var (service, notifMock, _, _) = CreateService(db);

        // Act
        await service.ProcessDeadlinesAsync();

        // Assert — rappel 1 ne doit pas être renvoyé
        notifMock.Verify(n => n.EnvoyerRappelAsync(
            It.IsAny<int>(), besoin.Id, besoin.Titre, 1, It.IsAny<string>()), Times.Never);
    }

    // ── Besoin non en attente — ignoré ────────────────────────────────────────

    [Fact]
    public async Task ProcessDeadlinesAsync_BesoinBrouillon_EstIgnore()
    {
        // Arrange
        using var db = CreateDb(nameof(ProcessDeadlinesAsync_BesoinBrouillon_EstIgnore));

        // Créer un besoin en BROUILLON (pas en attente)
        var role = new Role { Code = "Agent", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow };
        db.Roles.Add(role);
        var user = new Utilisateur
        {
            Nom = "Test", Prenom = "User", Email = "test@finstar-cm.com",
            MotDePasse = "hash", RoleId = role.Id, Actif = true,
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow
        };
        db.Utilisateurs.Add(user);
        var cat = new Categorie { Nom = "Cat", DateCreation = DateTime.UtcNow };
        db.Categories.Add(cat);
        await db.SaveChangesAsync();

        db.Besoins.Add(new Besoin
        {
            Titre = "Brouillon", Description = "Desc",
            Statut = "BROUILLON", NiveauImportance = "FAIBLE",
            UtilisateurId = user.Id, CategorieId = cat.Id,
            DateCreation = DateTime.UtcNow.AddDays(-10),
            DateModification = DateTime.UtcNow.AddDays(-10)
        });
        await db.SaveChangesAsync();

        var (service, notifMock, emailMock, _) = CreateService(db);

        // Act
        await service.ProcessDeadlinesAsync();

        // Assert — aucune notification
        notifMock.Verify(n => n.EnvoyerRappelAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        emailMock.Verify(e => e.EnvoyerRappelDelaiAsync(It.IsAny<Utilisateur>(), It.IsAny<Besoin>()), Times.Never);
    }
}
