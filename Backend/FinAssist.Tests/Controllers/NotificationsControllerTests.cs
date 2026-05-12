using System.Security.Claims;
using FinAssist.API.Controllers;
using FinAssist.Core.DTOs.Notifications;
using FinAssist.Core.DTOs.Signatures;
using FinAssist.Core.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FinAssist.Tests.Controllers;

/// <summary>
/// Tests unitaires pour NotificationsController et MaSignatureController.
/// </summary>
public class NotificationsControllerTests
{
    // ── Setup ─────────────────────────────────────────────────────────────────

    private static NotificationsController CreateController(Mock<INotificationService> serviceMock, int userId = 1)
    {
        var controller = new NotificationsController(serviceMock.Object);
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

    // ── GetMesNotifications ───────────────────────────────────────────────────

    [Fact]
    public async Task GetMesNotifications_Retourne200AvecListe()
    {
        // Arrange
        var serviceMock = new Mock<INotificationService>();
        serviceMock.Setup(s => s.GetMesNotificationsAsync(1)).ReturnsAsync(new List<NotificationDTO>
        {
            new() { Id = 1, Message = "Test", Type = "VALIDATION", Lu = false, DateEnvoi = DateTime.UtcNow },
            new() { Id = 2, Message = "Test 2", Type = "REJET", Lu = true, DateEnvoi = DateTime.UtcNow }
        });
        var controller = CreateController(serviceMock);

        // Act
        var result = await controller.GetMesNotifications();

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IEnumerable<NotificationDTO>>()
            .Which.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetMesNotifications_AucuneNotif_RetourneListeVide()
    {
        // Arrange
        var serviceMock = new Mock<INotificationService>();
        serviceMock.Setup(s => s.GetMesNotificationsAsync(1)).ReturnsAsync(new List<NotificationDTO>());
        var controller = CreateController(serviceMock);

        // Act
        var result = await controller.GetMesNotifications();

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IEnumerable<NotificationDTO>>()
            .Which.Should().BeEmpty();
    }

    // ── Creer ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Creer_TypeValide_Retourne200()
    {
        // Arrange
        var serviceMock = new Mock<INotificationService>();
        var dto = new CreateNotificationDTO { Message = "Test", Type = "VALIDATION", DestinataireIds = [1, 2] };
        serviceMock.Setup(s => s.CreerEtEnvoyerAsync(dto)).ReturnsAsync(new NotificationDTO
        {
            Id = 1, Message = "Test", Type = "VALIDATION", Lu = false, DateEnvoi = DateTime.UtcNow
        });
        var controller = CreateController(serviceMock);

        // Act
        var result = await controller.Creer(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Creer_TypeInvalide_Retourne400()
    {
        // Arrange
        var serviceMock = new Mock<INotificationService>();
        var dto = new CreateNotificationDTO { Message = "Test", Type = "INVALIDE", DestinataireIds = [1] };
        serviceMock.Setup(s => s.CreerEtEnvoyerAsync(dto))
            .ThrowsAsync(new ArgumentException("Type invalide : 'INVALIDE'."));
        var controller = CreateController(serviceMock);

        // Act
        var result = await controller.Creer(dto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── MarquerLu ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task MarquerLu_NotifExistante_Retourne204()
    {
        // Arrange
        var serviceMock = new Mock<INotificationService>();
        serviceMock.Setup(s => s.MarquerLuAsync(5, 1)).Returns(Task.CompletedTask);
        var controller = CreateController(serviceMock);

        // Act
        var result = await controller.MarquerLu(5);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        serviceMock.Verify(s => s.MarquerLuAsync(5, 1), Times.Once);
    }

    [Fact]
    public async Task MarquerLu_NotifInexistante_Retourne404()
    {
        // Arrange
        var serviceMock = new Mock<INotificationService>();
        serviceMock.Setup(s => s.MarquerLuAsync(99, 1))
            .ThrowsAsync(new KeyNotFoundException("Notification 99 introuvable."));
        var controller = CreateController(serviceMock);

        // Act
        var result = await controller.MarquerLu(99);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }
}

/// <summary>
/// Tests unitaires pour MaSignatureController.
/// </summary>
public class MaSignatureControllerTests
{
    private static MaSignatureController CreateController(Mock<ISignatureUtilisateurService> serviceMock, int userId = 1)
    {
        var controller = new MaSignatureController(serviceMock.Object);
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

    // ── Get ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_SignatureExistante_Retourne200()
    {
        // Arrange
        var serviceMock = new Mock<ISignatureUtilisateurService>();
        serviceMock.Setup(s => s.GetMaSignatureAsync(1)).ReturnsAsync(new SignatureUtilisateurDTO
        {
            Id = 1, Type = "manuscrite", ImageBase64 = "data:image/png;base64,abc",
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow
        });
        var controller = CreateController(serviceMock);

        // Act
        var result = await controller.Get();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Get_AucuneSignature_Retourne204()
    {
        // Arrange
        var serviceMock = new Mock<ISignatureUtilisateurService>();
        serviceMock.Setup(s => s.GetMaSignatureAsync(1)).ReturnsAsync((SignatureUtilisateurDTO?)null);
        var controller = CreateController(serviceMock);

        // Act
        var result = await controller.Get();

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    // ── Save ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Save_SignatureValide_Retourne200()
    {
        // Arrange
        var serviceMock = new Mock<ISignatureUtilisateurService>();
        var dto = new SaveSignatureUtilisateurDTO { Type = "manuscrite", ImageBase64 = "data:image/png;base64,abc" };
        serviceMock.Setup(s => s.SauvegarderAsync(1, dto)).ReturnsAsync(new SignatureUtilisateurDTO
        {
            Id = 1, Type = "manuscrite", ImageBase64 = dto.ImageBase64,
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow
        });
        var controller = CreateController(serviceMock);

        // Act
        var result = await controller.Save(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Save_TypeInvalide_Retourne400()
    {
        // Arrange
        var serviceMock = new Mock<ISignatureUtilisateurService>();
        var dto = new SaveSignatureUtilisateurDTO { Type = "invalide", ImageBase64 = "abc" };
        serviceMock.Setup(s => s.SauvegarderAsync(1, dto))
            .ThrowsAsync(new ArgumentException("Type invalide."));
        var controller = CreateController(serviceMock);

        // Act
        var result = await controller.Save(dto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_Retourne204()
    {
        // Arrange
        var serviceMock = new Mock<ISignatureUtilisateurService>();
        serviceMock.Setup(s => s.SupprimerAsync(1)).Returns(Task.CompletedTask);
        var controller = CreateController(serviceMock);

        // Act
        var result = await controller.Delete();

        // Assert
        result.Should().BeOfType<NoContentResult>();
        serviceMock.Verify(s => s.SupprimerAsync(1), Times.Once);
    }
}
