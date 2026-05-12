using FinAssist.Application.Services;
using FinAssist.Core.DTOs.Notifications;
using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;
using FinAssist.Infrastructure.Data;
using FinAssist.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FinAssist.Tests.Integration;

/// <summary>
/// Tests d'intégration pour NotificationService + NotificationRepository + AppDbContext (InMemory).
/// Couvre GetMesNotificationsAsync, MarquerLuAsync, CreerEtEnvoyerAsync,
/// NotifierSoumissionAsync, NotifierTransmissionAsync, NotifierRejetAsync,
/// EnvoyerRappelAsync — avec filtrage par préférences utilisateur.
/// </summary>
public class NotificationServiceIntegrationTests
{
    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    private static (NotificationService service, Mock<IFirebaseNotificationService> firebase, Mock<IWebPushService> webPush)
        CreateService(AppDbContext db)
    {
        var notifRepo = new NotificationRepository(db);
        var prefsRepo = new UserPreferencesRepository(db);
        var firebase = new Mock<IFirebaseNotificationService>();
        firebase.Setup(f => f.SendAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
        var webPush = new Mock<IWebPushService>();
        webPush.Setup(w => w.SendToUserAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
               .Returns(Task.CompletedTask);
        var hubMock = new Mock<IBesoinsHubService>();
        hubMock.Setup(h => h.NotifierUtilisateurAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
               .Returns(Task.CompletedTask);
        var service = new NotificationService(notifRepo, prefsRepo, firebase.Object, webPush.Object, hubMock.Object);
        return (service, firebase, webPush);
    }

    private static async Task<(Role role, Utilisateur user)> SeedUserAsync(
        AppDbContext db, string roleCode = "Agent", string email = "jean@finstar-cm.com")
    {
        var role = new Role { Code = roleCode, DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow };
        db.Roles.Add(role);
        var user = new Utilisateur
        {
            Nom = "Dupont", Prenom = "Jean", Email = email,
            MotDePasse = "hash", RoleId = role.Id, Actif = true,
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow
        };
        db.Utilisateurs.Add(user);
        await db.SaveChangesAsync();
        return (role, user);
    }

    // ── GetMesNotificationsAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetMesNotificationsAsync_SansNotifications_RetourneListeVide()
    {
        // Arrange
        using var db = CreateDb(nameof(GetMesNotificationsAsync_SansNotifications_RetourneListeVide));
        var (_, user) = await SeedUserAsync(db);
        var (service, _, _) = CreateService(db);

        // Act
        var result = (await service.GetMesNotificationsAsync(user.Id)).ToList();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMesNotificationsAsync_ApresCreation_RetourneNotification()
    {
        // Arrange
        using var db = CreateDb(nameof(GetMesNotificationsAsync_ApresCreation_RetourneNotification));
        var (_, user) = await SeedUserAsync(db);
        var (service, _, _) = CreateService(db);

        await service.CreerEtEnvoyerAsync(new CreateNotificationDTO
        {
            Message = "Test notification",
            Type = "VALIDATION",
            DestinataireIds = [user.Id]
        });

        // Act
        var result = (await service.GetMesNotificationsAsync(user.Id)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].Message.Should().Be("Test notification");
        result[0].Type.Should().Be("VALIDATION");
        result[0].Lu.Should().BeFalse();
    }

    [Fact]
    public async Task GetMesNotificationsAsync_PlusieursDestinataires_ChacunVoitLaSienne()
    {
        // Arrange
        using var db = CreateDb(nameof(GetMesNotificationsAsync_PlusieursDestinataires_ChacunVoitLaSienne));
        var (_, user1) = await SeedUserAsync(db, email: "user1@finstar-cm.com");
        var role2 = new Role { Code = "Resp", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow };
        db.Roles.Add(role2);
        var user2 = new Utilisateur
        {
            Nom = "Martin", Prenom = "Paul", Email = "user2@finstar-cm.com",
            MotDePasse = "hash", RoleId = role2.Id, Actif = true,
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow
        };
        db.Utilisateurs.Add(user2);
        await db.SaveChangesAsync();

        var (service, _, _) = CreateService(db);

        await service.CreerEtEnvoyerAsync(new CreateNotificationDTO
        {
            Message = "Notif pour tous",
            Type = "RAPPEL",
            DestinataireIds = [user1.Id, user2.Id]
        });

        // Act
        var notifs1 = (await service.GetMesNotificationsAsync(user1.Id)).ToList();
        var notifs2 = (await service.GetMesNotificationsAsync(user2.Id)).ToList();

        // Assert
        notifs1.Should().HaveCount(1);
        notifs2.Should().HaveCount(1);
    }

    // ── MarquerLuAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task MarquerLuAsync_NotificationNonLue_PasseLue()
    {
        // Arrange
        using var db = CreateDb(nameof(MarquerLuAsync_NotificationNonLue_PasseLue));
        var (_, user) = await SeedUserAsync(db);
        var (service, _, _) = CreateService(db);

        var notif = await service.CreerEtEnvoyerAsync(new CreateNotificationDTO
        {
            Message = "À lire",
            Type = "VALIDATION",
            DestinataireIds = [user.Id]
        });

        // Act
        await service.MarquerLuAsync(notif.Id, user.Id);

        // Assert
        var inDb = await db.UtilisateurNotifications
            .FirstOrDefaultAsync(un => un.NotificationId == notif.Id && un.UtilisateurId == user.Id);
        inDb!.Lu.Should().BeTrue();
    }

    // ── CreerEtEnvoyerAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task CreerEtEnvoyerAsync_TypeInvalide_LeveArgumentException()
    {
        // Arrange
        using var db = CreateDb(nameof(CreerEtEnvoyerAsync_TypeInvalide_LeveArgumentException));
        var (_, user) = await SeedUserAsync(db);
        var (service, _, _) = CreateService(db);

        // Act
        var act = async () => await service.CreerEtEnvoyerAsync(new CreateNotificationDTO
        {
            Message = "Test",
            Type = "TYPE_INEXISTANT",
            DestinataireIds = [user.Id]
        });

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*invalide*");
    }

    [Fact]
    public async Task CreerEtEnvoyerAsync_AppelleFirebaseEtWebPush()
    {
        // Arrange
        using var db = CreateDb(nameof(CreerEtEnvoyerAsync_AppelleFirebaseEtWebPush));
        var (_, user) = await SeedUserAsync(db);
        var (service, firebase, webPush) = CreateService(db);

        // Act
        await service.CreerEtEnvoyerAsync(new CreateNotificationDTO
        {
            Message = "Push test",
            Type = "RAPPEL",
            DestinataireIds = [user.Id]
        });

        // Assert
        firebase.Verify(f => f.SendAsync(
            It.Is<IEnumerable<int>>(ids => ids.Contains(user.Id)),
            It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        webPush.Verify(w => w.SendToUserAsync(user.Id, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    // ── NotifierSoumissionAsync ───────────────────────────────────────────────

    [Fact]
    public async Task NotifierSoumissionAsync_UtilisateurDuBonRole_ReceitNotification()
    {
        // Arrange
        using var db = CreateDb(nameof(NotifierSoumissionAsync_UtilisateurDuBonRole_ReceitNotification));
        var (_, agent) = await SeedUserAsync(db, roleCode: "Agent", email: "agent@finstar-cm.com");
        var (_, resp) = await SeedUserAsync(db, roleCode: "Responsable", email: "resp@finstar-cm.com");
        var (service, _, _) = CreateService(db);

        // Act
        await service.NotifierSoumissionAsync(1, "Besoin Test", agent.Id, "Responsable", "Jean Dupont");

        // Assert — le responsable doit avoir reçu la notification
        var notifs = (await service.GetMesNotificationsAsync(resp.Id)).ToList();
        notifs.Should().HaveCount(1);
        notifs[0].Message.Should().Contain("Besoin Test");
        notifs[0].Type.Should().Be("ACCUSE_RECEPTION");
    }

    [Fact]
    public async Task NotifierSoumissionAsync_UtilisateurAvecNotifDesactivee_NePasEnvoyer()
    {
        // Arrange
        using var db = CreateDb(nameof(NotifierSoumissionAsync_UtilisateurAvecNotifDesactivee_NePasEnvoyer));
        var (_, agent) = await SeedUserAsync(db, roleCode: "Agent", email: "agent@finstar-cm.com");
        var (_, resp) = await SeedUserAsync(db, roleCode: "Responsable", email: "resp@finstar-cm.com");

        // Désactiver les notifications pour le responsable
        db.UserPreferences.Add(new UserPreferences
        {
            UtilisateurId = resp.Id,
            NotifApp = false,
            AlertNouveauBesoin = true,
            AlertValidation = true,
            AlertEnAttente = true,
            DateModification = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var (service, _, _) = CreateService(db);

        // Act
        await service.NotifierSoumissionAsync(1, "Besoin Test", agent.Id, "Responsable", "Jean Dupont");

        // Assert — pas de notification (notifApp désactivée)
        var notifs = (await service.GetMesNotificationsAsync(resp.Id)).ToList();
        notifs.Should().BeEmpty();
    }

    // ── NotifierRejetAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task NotifierRejetAsync_CreateurAvecNotifActive_ReceitNotification()
    {
        // Arrange
        using var db = CreateDb(nameof(NotifierRejetAsync_CreateurAvecNotifActive_ReceitNotification));
        var (_, agent) = await SeedUserAsync(db, roleCode: "Agent");
        var (service, _, _) = CreateService(db);

        // Act
        await service.NotifierRejetAsync(1, "Besoin Rejeté", agent.Id, "Responsable");

        // Assert
        var notifs = (await service.GetMesNotificationsAsync(agent.Id)).ToList();
        notifs.Should().HaveCount(1);
        notifs[0].Type.Should().Be("REJET");
        notifs[0].Message.Should().Contain("Besoin Rejeté");
    }

    // ── EnvoyerRappelAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task EnvoyerRappelAsync_UtilisateurAvecNotifActive_ReceitRappel()
    {
        // Arrange
        using var db = CreateDb(nameof(EnvoyerRappelAsync_UtilisateurAvecNotifActive_ReceitRappel));
        var (_, resp) = await SeedUserAsync(db, roleCode: "Responsable");
        var (service, _, _) = CreateService(db);

        // Act
        await service.EnvoyerRappelAsync(resp.Id, 1, "Besoin Urgent", 1, "Rappel : délai bientôt dépassé");

        // Assert
        var notifs = (await service.GetMesNotificationsAsync(resp.Id)).ToList();
        notifs.Should().HaveCount(1);
        notifs[0].Type.Should().Be("RAPPEL");
        notifs[0].Message.Should().Contain("délai bientôt dépassé");
    }

    [Fact]
    public async Task EnvoyerRappelAsync_UtilisateurAvecAlertDesactivee_NePasEnvoyer()
    {
        // Arrange
        using var db = CreateDb(nameof(EnvoyerRappelAsync_UtilisateurAvecAlertDesactivee_NePasEnvoyer));
        var (_, resp) = await SeedUserAsync(db, roleCode: "Responsable");

        // Désactiver alertEnAttente
        db.UserPreferences.Add(new UserPreferences
        {
            UtilisateurId = resp.Id,
            NotifApp = true,
            AlertNouveauBesoin = true,
            AlertValidation = true,
            AlertEnAttente = false, // désactivé
            DateModification = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var (service, _, _) = CreateService(db);

        // Act
        await service.EnvoyerRappelAsync(resp.Id, 1, "Besoin", 1, "Rappel");

        // Assert — pas de notification
        var notifs = (await service.GetMesNotificationsAsync(resp.Id)).ToList();
        notifs.Should().BeEmpty();
    }

    // ── NotifierTransmissionAsync ─────────────────────────────────────────────

    [Fact]
    public async Task NotifierTransmissionAsync_UtilisateurDuBonRole_ReceitNotification()
    {
        // Arrange
        using var db = CreateDb(nameof(NotifierTransmissionAsync_UtilisateurDuBonRole_ReceitNotification));
        var (_, direction) = await SeedUserAsync(db, roleCode: "Direction", email: "dir@finstar-cm.com");
        var (service, _, _) = CreateService(db);

        // Act
        await service.NotifierTransmissionAsync(1, "Besoin Transmis", "Direction", "Paul Martin");

        // Assert
        var notifs = (await service.GetMesNotificationsAsync(direction.Id)).ToList();
        notifs.Should().HaveCount(1);
        notifs[0].Type.Should().Be("VALIDATION");
        notifs[0].Message.Should().Contain("Besoin Transmis");
    }
}
