using FinAssist.Application.Services;
using FinAssist.Core.DTOs.Notifications;
using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;
using FluentAssertions;
using Moq;

namespace FinAssist.Tests.Services;

/// <summary>
/// Tests unitaires pour NotificationService — envoi, filtrage par préférences et rappels.
/// </summary>
public class NotificationServiceTests
{
    // ── Setup ─────────────────────────────────────────────────────────────────

    private readonly Mock<INotificationRepository>      _notifRepoMock = new();
    private readonly Mock<IUserPreferencesRepository>   _prefsMock     = new();
    private readonly Mock<IFirebaseNotificationService> _firebaseMock  = new();
    private readonly Mock<IWebPushService>              _webPushMock   = new();
    private readonly Mock<IBesoinsHubService>           _hubMock       = new();

    private NotificationService CreateService() =>
        new(_notifRepoMock.Object, _prefsMock.Object, _firebaseMock.Object, _webPushMock.Object, _hubMock.Object);

    private void SetupDefaultMocks()
    {
        _notifRepoMock.Setup(r => r.CreateAsync(It.IsAny<Notification>(), It.IsAny<IEnumerable<int>>())).ReturnsAsync(new Notification());
        _firebaseMock.Setup(f => f.SendAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        _webPushMock.Setup(w => w.SendToUserAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        _hubMock.Setup(h => h.NotifierUtilisateurAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
    }

    private void SetupPrefs(int userId, bool notifApp = true, bool alertNouveauBesoin = true,
        bool alertValidation = true, bool alertEnAttente = true)
    {
        _prefsMock.Setup(p => p.GetByUtilisateurIdAsync(userId)).ReturnsAsync(new UserPreferences
        {
            UtilisateurId      = userId,
            NotifApp           = notifApp,
            AlertNouveauBesoin = alertNouveauBesoin,
            AlertValidation    = alertValidation,
            AlertEnAttente     = alertEnAttente
        });
    }

    // ── GetMesNotificationsAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetMesNotificationsAsync_RetourneNotificationsUtilisateur()
    {
        var notifs = new List<UtilisateurNotification>
        {
            new()
            {
                NotificationId = 1, Lu = false,
                Notification = new Notification
                {
                    Id = 1, Message = "Test notif",
                    Type = TypeNotification.VALIDATION,
                    DateEnvoi = DateTime.UtcNow
                }
            }
        };
        _notifRepoMock.Setup(r => r.GetByUtilisateurAsync(10)).ReturnsAsync(notifs);
        var service = CreateService();

        var result = (await service.GetMesNotificationsAsync(10)).ToList();

        result.Should().HaveCount(1);
        result[0].Message.Should().Be("Test notif");
        result[0].Lu.Should().BeFalse();
        result[0].Type.Should().Be("VALIDATION");
    }

    // ── CreerEtEnvoyerAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task CreerEtEnvoyerAsync_TypeValide_CreeeEtEnvoie()
    {
        SetupDefaultMocks();
        var dto = new CreateNotificationDTO
        {
            Message = "Nouvelle notification",
            Type = "VALIDATION",
            DestinataireIds = [1, 2]
        };
        var service = CreateService();

        var result = await service.CreerEtEnvoyerAsync(dto);

        result.Message.Should().Be("Nouvelle notification");
        result.Type.Should().Be("VALIDATION");
        result.Lu.Should().BeFalse();
        _notifRepoMock.Verify(r => r.CreateAsync(It.IsAny<Notification>(), It.Is<List<int>>(l => l.Count == 2)), Times.Once);
        _firebaseMock.Verify(f => f.SendAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task CreerEtEnvoyerAsync_TypeInvalide_LeveArgumentException()
    {
        var dto = new CreateNotificationDTO
        {
            Message = "Test", Type = "TYPE_INEXISTANT", DestinataireIds = [1]
        };
        var service = CreateService();

        var act = async () => await service.CreerEtEnvoyerAsync(dto);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*TYPE_INEXISTANT*");
    }

    [Fact]
    public async Task CreerEtEnvoyerAsync_EnvoieWebPushEtSignalR()
    {
        SetupDefaultMocks();
        var dto = new CreateNotificationDTO
        {
            Message = "Test push",
            Type = "RAPPEL",
            DestinataireIds = [5]
        };
        var service = CreateService();

        await service.CreerEtEnvoyerAsync(dto);

        _webPushMock.Verify(w => w.SendToUserAsync(5, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _hubMock.Verify(h => h.NotifierUtilisateurAsync(5, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task CreerEtEnvoyerAsync_PlusieursDestinataires_EnvoieWebPushPourChacun()
    {
        SetupDefaultMocks();
        var dto = new CreateNotificationDTO
        {
            Message = "Broadcast",
            Type = "VALIDATION",
            DestinataireIds = [1, 2, 3]
        };
        var service = CreateService();

        await service.CreerEtEnvoyerAsync(dto);

        _webPushMock.Verify(w => w.SendToUserAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(3));
        _hubMock.Verify(h => h.NotifierUtilisateurAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(3));
    }

    // ── NotifierSoumissionAsync ───────────────────────────────────────────────

    [Fact]
    public async Task NotifierSoumissionAsync_UtilisateursAvecPrefs_EnvoieNotification()
    {
        SetupDefaultMocks();
        _notifRepoMock.Setup(r => r.GetUtilisateurIdsByRoleAsync("Responsable"))
            .ReturnsAsync(new List<int> { 20, 21 });
        SetupPrefs(20, notifApp: true, alertNouveauBesoin: true);
        SetupPrefs(21, notifApp: true, alertNouveauBesoin: true);
        var service = CreateService();

        await service.NotifierSoumissionAsync(1, "Besoin test", 10, "Responsable", "Jean Dupont");

        _notifRepoMock.Verify(r => r.CreateAsync(
            It.Is<Notification>(n => n.Type == TypeNotification.ACCUSE_RECEPTION),
            It.Is<List<int>>(l => l.Count == 2)), Times.Once);
    }

    [Fact]
    public async Task NotifierSoumissionAsync_EnvoieWebPushAuxDestinataires()
    {
        SetupDefaultMocks();
        _notifRepoMock.Setup(r => r.GetUtilisateurIdsByRoleAsync("Responsable"))
            .ReturnsAsync(new List<int> { 20, 21 });
        SetupPrefs(20, notifApp: true, alertNouveauBesoin: true);
        SetupPrefs(21, notifApp: true, alertNouveauBesoin: true);
        var service = CreateService();

        await service.NotifierSoumissionAsync(1, "Besoin test", 10, "Responsable", "Jean");

        _webPushMock.Verify(w => w.SendToUserAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
    }

    [Fact]
    public async Task NotifierSoumissionAsync_PrefsDesactivees_NEnvoiePas()
    {
        _notifRepoMock.Setup(r => r.GetUtilisateurIdsByRoleAsync("Responsable"))
            .ReturnsAsync(new List<int> { 20 });
        SetupPrefs(20, notifApp: false, alertNouveauBesoin: false);
        var service = CreateService();

        await service.NotifierSoumissionAsync(1, "Besoin test", 10, "Responsable", "Jean");

        _notifRepoMock.Verify(r => r.CreateAsync(It.IsAny<Notification>(), It.IsAny<IEnumerable<int>>()), Times.Never);
        _webPushMock.Verify(w => w.SendToUserAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task NotifierSoumissionAsync_AucunUtilisateurDuRole_NeFaitRien()
    {
        _notifRepoMock.Setup(r => r.GetUtilisateurIdsByRoleAsync("Responsable"))
            .ReturnsAsync(new List<int>());
        var service = CreateService();

        await service.NotifierSoumissionAsync(1, "Besoin", 10, "Responsable", "Jean");

        _notifRepoMock.Verify(r => r.CreateAsync(It.IsAny<Notification>(), It.IsAny<IEnumerable<int>>()), Times.Never);
        _webPushMock.Verify(w => w.SendToUserAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // ── NotifierTransmissionAsync ─────────────────────────────────────────────

    [Fact]
    public async Task NotifierTransmissionAsync_UtilisateursAvecPrefs_EnvoieNotification()
    {
        SetupDefaultMocks();
        _notifRepoMock.Setup(r => r.GetUtilisateurIdsByRoleAsync("Direction"))
            .ReturnsAsync(new List<int> { 30 });
        SetupPrefs(30, notifApp: true, alertEnAttente: true);
        var service = CreateService();

        await service.NotifierTransmissionAsync(1, "Besoin test", "Direction", "Jean Dupont");

        _notifRepoMock.Verify(r => r.CreateAsync(
            It.Is<Notification>(n => n.Type == TypeNotification.VALIDATION),
            It.Is<List<int>>(l => l.Count == 1)), Times.Once);
    }

    [Fact]
    public async Task NotifierTransmissionAsync_EnvoieWebPushAuDestinataire()
    {
        SetupDefaultMocks();
        _notifRepoMock.Setup(r => r.GetUtilisateurIdsByRoleAsync("Direction"))
            .ReturnsAsync(new List<int> { 30 });
        SetupPrefs(30, notifApp: true, alertEnAttente: true);
        var service = CreateService();

        await service.NotifierTransmissionAsync(1, "Besoin test", "Direction", "Jean");

        _webPushMock.Verify(w => w.SendToUserAsync(30, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task NotifierTransmissionAsync_AlertEnAttenteDesactivee_NEnvoiePas()
    {
        _notifRepoMock.Setup(r => r.GetUtilisateurIdsByRoleAsync("Direction"))
            .ReturnsAsync(new List<int> { 30 });
        SetupPrefs(30, notifApp: true, alertEnAttente: false);
        var service = CreateService();

        await service.NotifierTransmissionAsync(1, "Besoin test", "Direction", "Jean");

        _notifRepoMock.Verify(r => r.CreateAsync(It.IsAny<Notification>(), It.IsAny<IEnumerable<int>>()), Times.Never);
        _webPushMock.Verify(w => w.SendToUserAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task NotifierTransmissionAsync_AucunUtilisateurDuRole_NeFaitRien()
    {
        _notifRepoMock.Setup(r => r.GetUtilisateurIdsByRoleAsync("Direction"))
            .ReturnsAsync(new List<int>());
        var service = CreateService();

        await service.NotifierTransmissionAsync(1, "Besoin", "Direction", "Jean");

        _notifRepoMock.Verify(r => r.CreateAsync(It.IsAny<Notification>(), It.IsAny<IEnumerable<int>>()), Times.Never);
    }

    // ── NotifierRejetAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task NotifierRejetAsync_PrefsActives_EnvoieNotification()
    {
        SetupDefaultMocks();
        SetupPrefs(10, notifApp: true, alertValidation: true);
        var service = CreateService();

        await service.NotifierRejetAsync(1, "Besoin test", 10, "Responsable");

        _notifRepoMock.Verify(r => r.CreateAsync(
            It.Is<Notification>(n => n.Type == TypeNotification.REJET),
            It.IsAny<IEnumerable<int>>()), Times.Once);
    }

    [Fact]
    public async Task NotifierRejetAsync_EnvoieWebPushAuCreateur()
    {
        SetupDefaultMocks();
        SetupPrefs(10, notifApp: true, alertValidation: true);
        var service = CreateService();

        await service.NotifierRejetAsync(1, "Besoin test", 10, "Responsable");

        _webPushMock.Verify(w => w.SendToUserAsync(10, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task NotifierRejetAsync_PrefsDesactivees_NEnvoiePas()
    {
        SetupPrefs(10, notifApp: false, alertValidation: false);
        var service = CreateService();

        await service.NotifierRejetAsync(1, "Besoin test", 10, "Responsable");

        _notifRepoMock.Verify(r => r.CreateAsync(It.IsAny<Notification>(), It.IsAny<IEnumerable<int>>()), Times.Never);
        _webPushMock.Verify(w => w.SendToUserAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // ── NotifierSignatureAsync ────────────────────────────────────────────────

    [Fact]
    public async Task NotifierSignatureAsync_PrefsActives_EnvoieNotification()
    {
        SetupDefaultMocks();
        SetupPrefs(10, notifApp: true, alertValidation: true);
        var service = CreateService();

        await service.NotifierSignatureAsync(1, "Besoin test", 10, "Direction");

        _notifRepoMock.Verify(r => r.CreateAsync(
            It.Is<Notification>(n => n.Type == TypeNotification.SIGNATURE_REQUISE),
            It.IsAny<IEnumerable<int>>()), Times.Once);
    }

    [Fact]
    public async Task NotifierSignatureAsync_EnvoieWebPushALAgent()
    {
        SetupDefaultMocks();
        SetupPrefs(10, notifApp: true, alertValidation: true);
        var service = CreateService();

        await service.NotifierSignatureAsync(1, "Besoin test", 10, "Direction");

        _webPushMock.Verify(w => w.SendToUserAsync(10, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task NotifierSignatureAsync_PrefsDesactivees_NEnvoiePas()
    {
        SetupPrefs(10, notifApp: false, alertValidation: false);
        var service = CreateService();

        await service.NotifierSignatureAsync(1, "Besoin test", 10, "Direction");

        _notifRepoMock.Verify(r => r.CreateAsync(It.IsAny<Notification>(), It.IsAny<IEnumerable<int>>()), Times.Never);
        _webPushMock.Verify(w => w.SendToUserAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // ── EnvoyerRappelAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task EnvoyerRappelAsync_PrefsActives_EnvoieRappel()
    {
        SetupDefaultMocks();
        SetupPrefs(20, notifApp: true, alertEnAttente: true);
        var service = CreateService();

        await service.EnvoyerRappelAsync(20, 1, "Besoin test", 1, "Rappel : validez ce besoin.");

        _notifRepoMock.Verify(r => r.CreateAsync(
            It.Is<Notification>(n => n.Type == TypeNotification.RAPPEL),
            It.IsAny<IEnumerable<int>>()), Times.Once);
    }

    [Fact]
    public async Task EnvoyerRappelAsync_EnvoieWebPushALUtilisateur()
    {
        SetupDefaultMocks();
        SetupPrefs(20, notifApp: true, alertEnAttente: true);
        var service = CreateService();

        await service.EnvoyerRappelAsync(20, 1, "Besoin test", 1, "Rappel");

        _webPushMock.Verify(w => w.SendToUserAsync(20, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task EnvoyerRappelAsync_PrefsDesactivees_NEnvoiePas()
    {
        SetupPrefs(20, notifApp: false, alertEnAttente: false);
        var service = CreateService();

        await service.EnvoyerRappelAsync(20, 1, "Besoin test", 1, "Rappel");

        _notifRepoMock.Verify(r => r.CreateAsync(It.IsAny<Notification>(), It.IsAny<IEnumerable<int>>()), Times.Never);
        _webPushMock.Verify(w => w.SendToUserAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task EnvoyerRappelAsync_Niveau1_EnvoieAvecTitreRappel()
    {
        SetupDefaultMocks();
        SetupPrefs(20, notifApp: true, alertEnAttente: true);
        var service = CreateService();

        await service.EnvoyerRappelAsync(20, 1, "Besoin test", 1, "Rappel : validez ce besoin.");

        _firebaseMock.Verify(f => f.SendAsync(
            It.IsAny<IEnumerable<int>>(),
            It.Is<string>(t => t.Contains("Rappel")),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task EnvoyerRappelAsync_Niveau2_EnvoieAvecTitreUrgent()
    {
        SetupDefaultMocks();
        SetupPrefs(20, notifApp: true, alertEnAttente: true);
        var service = CreateService();

        await service.EnvoyerRappelAsync(20, 1, "Besoin test", 2, "Urgent : validez ce besoin !");

        _firebaseMock.Verify(f => f.SendAsync(
            It.IsAny<IEnumerable<int>>(),
            It.Is<string>(t => t.Contains("Urgent")),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task EnvoyerRappelAsync_Niveau3_EnvoieAvecTitreExpiration()
    {
        SetupDefaultMocks();
        SetupPrefs(20, notifApp: true, alertEnAttente: true);
        var service = CreateService();

        await service.EnvoyerRappelAsync(20, 1, "Besoin test", 3, "🚨 Expiration imminente !");

        _firebaseMock.Verify(f => f.SendAsync(
            It.IsAny<IEnumerable<int>>(),
            It.Is<string>(t => t.Contains("Expiration")),
            It.IsAny<string>()), Times.Once);
    }

    // ── EnvoyerAlertExpirationAsync ───────────────────────────────────────────

    [Fact]
    public async Task EnvoyerAlertExpirationAsync_PrefsActives_EnvoieViaSignalRUniquement()
    {
        SetupDefaultMocks();
        SetupPrefs(20, notifApp: true, alertEnAttente: true);
        var service = CreateService();

        await service.EnvoyerAlertExpirationAsync(20, 1, "Besoin test", "⏰ Délai expiré");

        // SignalR appelé
        _hubMock.Verify(h => h.NotifierUtilisateurAsync(20, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        // WebPush NON appelé (comportement voulu — email géré séparément)
        _webPushMock.Verify(w => w.SendToUserAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task EnvoyerAlertExpirationAsync_PrefsDesactivees_NEnvoiePas()
    {
        SetupPrefs(20, notifApp: false, alertEnAttente: false);
        var service = CreateService();

        await service.EnvoyerAlertExpirationAsync(20, 1, "Besoin test", "⏰ Délai expiré");

        _notifRepoMock.Verify(r => r.CreateAsync(It.IsAny<Notification>(), It.IsAny<IEnumerable<int>>()), Times.Never);
        _hubMock.Verify(h => h.NotifierUtilisateurAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // ── MarquerLuAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task MarquerLuAsync_AppelleRepository()
    {
        _notifRepoMock.Setup(r => r.MarquerLuAsync(5, 10)).Returns(Task.CompletedTask);
        var service = CreateService();

        await service.MarquerLuAsync(5, 10);

        _notifRepoMock.Verify(r => r.MarquerLuAsync(5, 10), Times.Once);
    }
}
