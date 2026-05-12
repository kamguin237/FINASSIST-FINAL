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
/// Tests unitaires pour QrSignatureController — sessions QR et soumission de signature mobile.
/// </summary>
public class QrSignatureControllerTests
{
    // ── Setup ─────────────────────────────────────────────────────────────────

    private readonly Mock<IQrSessionRepository>            _sessionRepoMock = new();
    private readonly Mock<ISignatureUtilisateurRepository> _sigRepoMock     = new();
    private readonly Mock<IUsersService>                   _usersMock       = new();

    private QrSignatureController CreateController(int userId = 1)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["App:BaseUrl"] = "https://test.finassist.com"
            })
            .Build();

        var controller = new QrSignatureController(
            _sessionRepoMock.Object, _sigRepoMock.Object, _usersMock.Object, config);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, "Responsable")
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

    private static QrSignatureSession Session(string token, int userId = 1,
        bool completed = false, bool expired = false) => new()
    {
        Id = 1, Token = token, UtilisateurId = userId,
        CreeLe = DateTime.UtcNow,
        Expiration = expired ? DateTime.UtcNow.AddMinutes(-5) : DateTime.UtcNow.AddMinutes(10),
        Completed = completed,
        Utilisateur = new Utilisateur
        {
            Id = userId, Nom = "Dupont", Prenom = "Jean",
            Email = "jean@test.com", MotDePasse = "x",
            Role = new Role { Id = 1, Code = "Responsable" }
        }
    };

    // ── CreateSession ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateSession_CreeSesionEtRetourneToken()
    {
        // Arrange
        _sessionRepoMock.Setup(r => r.CreateAsync(It.IsAny<QrSignatureSession>())).ReturnsAsync(new QrSignatureSession());
        var controller = CreateController(userId: 1);

        // Act
        var result = await controller.CreateSession();

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = ok.Value!;
        var type = value.GetType();

        var token = type.GetProperty("token")?.GetValue(value)?.ToString();
        var urlMobile = type.GetProperty("urlMobile")?.GetValue(value)?.ToString();

        token.Should().NotBeNullOrEmpty();
        token!.Length.Should().Be(32); // Guid.NewGuid().ToString("N")
        urlMobile.Should().StartWith("https://test.finassist.com/sign-mobile/");
        urlMobile.Should().Contain(token);
    }

    [Fact]
    public async Task CreateSession_AssocieUtilisateurConnecte()
    {
        // Arrange
        QrSignatureSession? captured = null;
        _sessionRepoMock.Setup(r => r.CreateAsync(It.IsAny<QrSignatureSession>()))
            .Callback<QrSignatureSession>(s => captured = s)
            .ReturnsAsync(new QrSignatureSession());

        var controller = CreateController(userId: 42);

        // Act
        await controller.CreateSession();

        // Assert
        captured.Should().NotBeNull();
        captured!.UtilisateurId.Should().Be(42);
        captured.Completed.Should().BeFalse();
        captured.Expiration.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(10), TimeSpan.FromSeconds(10));
    }

    // ── GetStatus ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStatus_SessionEnCours_RetourneNonComplete()
    {
        // Arrange
        var session = Session("abc123", userId: 1, completed: false);
        _sessionRepoMock.Setup(r => r.GetByTokenAsync("abc123")).ReturnsAsync(session);
        var controller = CreateController(userId: 1);

        // Act
        var result = await controller.GetStatus("abc123");

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = ok.Value!;
        var type = value.GetType();
        type.GetProperty("completed")?.GetValue(value).Should().Be(false);
        type.GetProperty("expired")?.GetValue(value).Should().Be(false);
    }

    [Fact]
    public async Task GetStatus_SessionComplete_RetourneComplete()
    {
        // Arrange
        var session = Session("abc123", userId: 1, completed: true);
        _sessionRepoMock.Setup(r => r.GetByTokenAsync("abc123")).ReturnsAsync(session);
        var controller = CreateController(userId: 1);

        // Act
        var result = await controller.GetStatus("abc123");

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = ok.Value!;
        value.GetType().GetProperty("completed")?.GetValue(value).Should().Be(true);
    }

    [Fact]
    public async Task GetStatus_SessionExpiree_RetourneExpire()
    {
        // Arrange
        var session = Session("abc123", userId: 1, expired: true, completed: false);
        _sessionRepoMock.Setup(r => r.GetByTokenAsync("abc123")).ReturnsAsync(session);
        var controller = CreateController(userId: 1);

        // Act
        var result = await controller.GetStatus("abc123");

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = ok.Value!;
        value.GetType().GetProperty("expired")?.GetValue(value).Should().Be(true);
    }

    [Fact]
    public async Task GetStatus_SessionIntrouvable_Retourne404()
    {
        // Arrange
        _sessionRepoMock.Setup(r => r.GetByTokenAsync("inconnu")).ReturnsAsync((QrSignatureSession?)null);
        var controller = CreateController();

        // Act
        var result = await controller.GetStatus("inconnu");

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetStatus_AutreUtilisateur_Retourne403()
    {
        // Arrange — session appartient à userId 99, mais userId 1 tente d'y accéder
        var session = Session("abc123", userId: 99);
        _sessionRepoMock.Setup(r => r.GetByTokenAsync("abc123")).ReturnsAsync(session);
        var controller = CreateController(userId: 1);

        // Act
        var result = await controller.GetStatus("abc123");

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    // ── GetSession (mobile) ───────────────────────────────────────────────────

    [Fact]
    public async Task GetSession_SessionValide_RetourneInfosUtilisateur()
    {
        // Arrange
        var session = Session("abc123");
        _sessionRepoMock.Setup(r => r.GetByTokenAsync("abc123")).ReturnsAsync(session);
        var controller = CreateController();

        // Act
        var result = await controller.GetSession("abc123");

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = ok.Value!;
        var type = value.GetType();
        type.GetProperty("nom")?.GetValue(value).Should().Be("Dupont");
        type.GetProperty("prenom")?.GetValue(value).Should().Be("Jean");
        type.GetProperty("role")?.GetValue(value).Should().Be("Responsable");
    }

    [Fact]
    public async Task GetSession_SessionExpiree_Retourne400()
    {
        // Arrange
        var session = Session("abc123", expired: true);
        _sessionRepoMock.Setup(r => r.GetByTokenAsync("abc123")).ReturnsAsync(session);
        var controller = CreateController();

        // Act
        var result = await controller.GetSession("abc123");

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetSession_SessionDejaUtilisee_Retourne400()
    {
        // Arrange
        var session = Session("abc123", completed: true);
        _sessionRepoMock.Setup(r => r.GetByTokenAsync("abc123")).ReturnsAsync(session);
        var controller = CreateController();

        // Act
        var result = await controller.GetSession("abc123");

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── Submit (mobile) ───────────────────────────────────────────────────────

    [Fact]
    public async Task Submit_SignatureValide_SauvegardeEtRetourne200()
    {
        // Arrange
        var session = Session("abc123");
        _sessionRepoMock.Setup(r => r.GetByTokenAsync("abc123")).ReturnsAsync(session);
        _sigRepoMock.Setup(r => r.SaveAsync(It.IsAny<SignatureUtilisateur>()))
            .ReturnsAsync(new SignatureUtilisateur { Id = 1 });
        _sessionRepoMock.Setup(r => r.UpdateAsync(It.IsAny<QrSignatureSession>())).Returns(Task.CompletedTask);

        var controller = CreateController();

        // Act
        var result = await controller.Submit("abc123", new SubmitSignatureDTO
        {
            ImageBase64 = "data:image/png;base64,abc123"
        });

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _sigRepoMock.Verify(r => r.SaveAsync(It.Is<SignatureUtilisateur>(s =>
            s.Type == "qrcode" &&
            s.ImageBase64 == "data:image/png;base64,abc123")), Times.Once);
        _sessionRepoMock.Verify(r => r.UpdateAsync(It.Is<QrSignatureSession>(s =>
            s.Completed && s.SignatureBase64 == "data:image/png;base64,abc123")), Times.Once);
    }

    [Fact]
    public async Task Submit_SignatureVide_Retourne400()
    {
        // Arrange
        var session = Session("abc123");
        _sessionRepoMock.Setup(r => r.GetByTokenAsync("abc123")).ReturnsAsync(session);
        var controller = CreateController();

        // Act
        var result = await controller.Submit("abc123", new SubmitSignatureDTO { ImageBase64 = "" });

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Submit_SessionExpiree_Retourne400()
    {
        // Arrange
        var session = Session("abc123", expired: true);
        _sessionRepoMock.Setup(r => r.GetByTokenAsync("abc123")).ReturnsAsync(session);
        var controller = CreateController();

        // Act
        var result = await controller.Submit("abc123", new SubmitSignatureDTO
        {
            ImageBase64 = "data:image/png;base64,abc"
        });

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Submit_SessionDejaUtilisee_Retourne400()
    {
        // Arrange
        var session = Session("abc123", completed: true);
        _sessionRepoMock.Setup(r => r.GetByTokenAsync("abc123")).ReturnsAsync(session);
        var controller = CreateController();

        // Act
        var result = await controller.Submit("abc123", new SubmitSignatureDTO
        {
            ImageBase64 = "data:image/png;base64,abc"
        });

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Submit_SessionIntrouvable_Retourne404()
    {
        // Arrange
        _sessionRepoMock.Setup(r => r.GetByTokenAsync("inconnu")).ReturnsAsync((QrSignatureSession?)null);
        var controller = CreateController();

        // Act
        var result = await controller.Submit("inconnu", new SubmitSignatureDTO
        {
            ImageBase64 = "data:image/png;base64,abc"
        });

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
