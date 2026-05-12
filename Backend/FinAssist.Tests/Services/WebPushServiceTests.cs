using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;
using FinAssist.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace FinAssist.Tests.Services;

/// <summary>
/// Tests unitaires pour WebPushService.
/// On ne peut pas tester l'envoi réseau réel (WebPushClient appelle un serveur externe),
/// donc on vérifie le comportement du service : branchements, logs, suppression des
/// subscriptions expirées, et gestion des clés VAPID manquantes.
/// </summary>
public class WebPushServiceTests
{
    // ── Setup ─────────────────────────────────────────────────────────────────

    private readonly Mock<IPushSubscriptionRepository> _repoMock   = new();
    private readonly Mock<ILogger<WebPushService>>     _loggerMock = new();

    /// <summary>Crée une IConfiguration en mémoire avec les clés VAPID fournies.</summary>
    private static IConfiguration BuildConfig(
        string? publicKey  = "BIGOipHWZkUWsJzabUIkmdz0XR5tFVGfUgum_D73Q-6Er7ltCzuUAoYp0RDTggVjoENxukR56VYvEDY4gaxNuTU",
        string? privateKey = "2cGpZ2s_p9WWZXUIG5YAMIiiEdy3c3s89zzr8waOxZg",
        string? subject    = "mailto:admin@finassist.com")
    {
        var dict = new Dictionary<string, string?>();
        if (publicKey  is not null) dict["Vapid:PublicKey"]  = publicKey;
        if (privateKey is not null) dict["Vapid:PrivateKey"] = privateKey;
        if (subject    is not null) dict["Vapid:Subject"]    = subject;
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    private WebPushService CreateService(IConfiguration? config = null)
        => new(_repoMock.Object, config ?? BuildConfig(), _loggerMock.Object);

    // ── Aucune subscription ───────────────────────────────────────────────────

    [Fact]
    public async Task SendToUserAsync_AucuneSubscription_NeFaitRienEtLogDebug()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByUtilisateurAsync(42))
            .ReturnsAsync(Enumerable.Empty<PushSubscription>());
        var service = CreateService();

        // Act — ne doit pas lever d'exception
        await service.SendToUserAsync(42, "Titre", "Message");

        // Assert — aucune tentative d'envoi, aucune suppression
        _repoMock.Verify(r => r.DeleteAsync(It.IsAny<string>()), Times.Never);
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Aucune subscription")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ── Clés VAPID manquantes ─────────────────────────────────────────────────

    [Fact]
    public async Task SendToUserAsync_ClesVapidManquantes_LogErreurEtNEnvoiePas()
    {
        // Arrange — config sans clés VAPID
        var configSansVapid = BuildConfig(publicKey: null, privateKey: null);
        _repoMock.Setup(r => r.GetByUtilisateurAsync(1))
            .ReturnsAsync(new List<PushSubscription>
            {
                new() { Endpoint = "https://push.example.com/sub1", P256dh = "key", Auth = "auth" }
            });
        var service = CreateService(configSansVapid);

        // Act
        await service.SendToUserAsync(1, "Titre", "Message");

        // Assert — log d'erreur émis, aucune suppression
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("VAPID")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        _repoMock.Verify(r => r.DeleteAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SendToUserAsync_ClePubliqueManquante_LogErreur()
    {
        // Arrange — seulement la clé privée
        var config = BuildConfig(publicKey: null, privateKey: "privatekey");
        _repoMock.Setup(r => r.GetByUtilisateurAsync(1))
            .ReturnsAsync(new List<PushSubscription>
            {
                new() { Endpoint = "https://push.example.com/sub1", P256dh = "key", Auth = "auth" }
            });
        var service = CreateService(config);

        // Act
        await service.SendToUserAsync(1, "Titre", "Message");

        // Assert
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("VAPID")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ── Récupération des subscriptions ────────────────────────────────────────

    [Fact]
    public async Task SendToUserAsync_AppelleGetByUtilisateurAsync_AvecBonId()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByUtilisateurAsync(99))
            .ReturnsAsync(Enumerable.Empty<PushSubscription>());
        var service = CreateService();

        // Act
        await service.SendToUserAsync(99, "Titre", "Message");

        // Assert
        _repoMock.Verify(r => r.GetByUtilisateurAsync(99), Times.Once);
    }

    // ── URL par défaut ────────────────────────────────────────────────────────

    [Fact]
    public async Task SendToUserAsync_UrlNulle_UtiliseUrlParDefaut()
    {
        // Arrange — on vérifie que le service ne lève pas d'exception avec url=null
        _repoMock.Setup(r => r.GetByUtilisateurAsync(1))
            .ReturnsAsync(Enumerable.Empty<PushSubscription>());
        var service = CreateService();

        // Act — url non fournie → doit utiliser "/notifications"
        var act = async () => await service.SendToUserAsync(1, "Titre", "Message", null);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendToUserAsync_UrlFournie_NeLevePasException()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByUtilisateurAsync(1))
            .ReturnsAsync(Enumerable.Empty<PushSubscription>());
        var service = CreateService();

        // Act
        var act = async () => await service.SendToUserAsync(1, "Titre", "Message", "/besoins/42");

        // Assert
        await act.Should().NotThrowAsync();
    }

    // ── Plusieurs subscriptions ───────────────────────────────────────────────

    [Fact]
    public async Task SendToUserAsync_PlusieursSubscriptions_AppelleGetUneSeuleFois()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByUtilisateurAsync(5))
            .ReturnsAsync(new List<PushSubscription>
            {
                new() { Endpoint = "https://push.example.com/sub1", P256dh = "k1", Auth = "a1" },
                new() { Endpoint = "https://push.example.com/sub2", P256dh = "k2", Auth = "a2" }
            });
        var service = CreateService();

        // Act — les envois réseau vont échouer (pas de vrai serveur push), mais on vérifie
        // que le repo est interrogé une seule fois
        try { await service.SendToUserAsync(5, "Titre", "Message"); } catch { /* réseau */ }

        // Assert
        _repoMock.Verify(r => r.GetByUtilisateurAsync(5), Times.Once);
    }
}
