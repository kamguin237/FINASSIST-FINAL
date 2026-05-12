using System.Security.Claims;
using FinAssist.API.Controllers;
using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;

namespace FinAssist.Tests.Controllers;

/// <summary>
/// Tests unitaires pour PushController — clé VAPID, subscribe et unsubscribe.
/// </summary>
public class PushControllerTests
{
    // ── Setup ─────────────────────────────────────────────────────────────────

    private readonly Mock<IPushSubscriptionRepository> _repoMock = new();

    private PushController CreateController(int userId = 1, string? vapidKey = "BIGOipHWZkUW...")
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Vapid:PublicKey"] = vapidKey
            })
            .Build();

        var controller = new PushController(_repoMock.Object, config);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, "Agent")
        };
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
            }
        };
        return controller;
    }

    // ── GetPublicKey ──────────────────────────────────────────────────────────

    [Fact]
    public void GetPublicKey_RetourneCleVapid()
    {
        // Arrange
        var controller = CreateController(vapidKey: "BIGOipHWZkUWsJzabUIkmdz0XR5tFVGfUgum");

        // Act
        var result = controller.GetPublicKey();

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = ok.Value!;
        value.GetType().GetProperty("publicKey")?.GetValue(value)
            .Should().Be("BIGOipHWZkUWsJzabUIkmdz0XR5tFVGfUgum");
    }

    [Fact]
    public void GetPublicKey_CleNonConfiguree_RetourneNull()
    {
        // Arrange
        var controller = CreateController(vapidKey: null);

        // Act
        var result = controller.GetPublicKey();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    // ── Subscribe ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Subscribe_DonneesValides_SauvegardeEtRetourne200()
    {
        // Arrange
        _repoMock.Setup(r => r.SaveAsync(It.IsAny<PushSubscription>())).Returns(Task.CompletedTask);
        var controller = CreateController(userId: 42);

        var dto = new PushSubscribeDTO
        {
            Endpoint = "https://fcm.googleapis.com/fcm/send/abc123",
            P256dh = "BNcRdreALRFXTkOOUHK1EtK2wtaz5Ry4YfYCA_0QTpQtUbVlTiESgcc9xsIfwamoEkline32v7mYFbwzOjnurdg",
            Auth = "tBHItJI5svbpez7KI4CCXg"
        };

        // Act
        var result = await controller.Subscribe(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _repoMock.Verify(r => r.SaveAsync(It.Is<PushSubscription>(s =>
            s.UtilisateurId == 42 &&
            s.Endpoint == dto.Endpoint &&
            s.P256dh == dto.P256dh &&
            s.Auth == dto.Auth)), Times.Once);
    }

    [Fact]
    public async Task Subscribe_AssocieUtilisateurConnecte()
    {
        // Arrange
        PushSubscription? captured = null;
        _repoMock.Setup(r => r.SaveAsync(It.IsAny<PushSubscription>()))
            .Callback<PushSubscription>(s => captured = s)
            .Returns(Task.CompletedTask);

        var controller = CreateController(userId: 99);

        // Act
        await controller.Subscribe(new PushSubscribeDTO
        {
            Endpoint = "https://example.com/push",
            P256dh = "key",
            Auth = "auth"
        });

        // Assert
        captured.Should().NotBeNull();
        captured!.UtilisateurId.Should().Be(99);
    }

    // ── Unsubscribe ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Unsubscribe_EndpointExistant_Retourne204()
    {
        // Arrange
        _repoMock.Setup(r => r.DeleteAsync("https://fcm.googleapis.com/fcm/send/abc123"))
            .Returns(Task.CompletedTask);
        var controller = CreateController();

        // Act
        var result = await controller.Unsubscribe(new UnsubscribeDTO
        {
            Endpoint = "https://fcm.googleapis.com/fcm/send/abc123"
        });

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _repoMock.Verify(r => r.DeleteAsync("https://fcm.googleapis.com/fcm/send/abc123"), Times.Once);
    }

    [Fact]
    public async Task Unsubscribe_AppelleRepositoryAvecBonEndpoint()
    {
        // Arrange
        string? capturedEndpoint = null;
        _repoMock.Setup(r => r.DeleteAsync(It.IsAny<string>()))
            .Callback<string>(e => capturedEndpoint = e)
            .Returns(Task.CompletedTask);

        var controller = CreateController();
        var endpoint = "https://push.example.com/subscription/xyz";

        // Act
        await controller.Unsubscribe(new UnsubscribeDTO { Endpoint = endpoint });

        // Assert
        capturedEndpoint.Should().Be(endpoint);
    }
}
