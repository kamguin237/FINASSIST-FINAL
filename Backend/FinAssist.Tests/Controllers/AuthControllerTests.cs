using System.Security.Claims;
using FinAssist.API.Controllers;
using FinAssist.Core.DTOs.Auth;
using FinAssist.Core.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FinAssist.Tests.Controllers;

/// <summary>
/// Tests unitaires pour AuthController — login, logout et changement de mot de passe.
/// </summary>
public class AuthControllerTests
{
    // ── Setup ─────────────────────────────────────────────────────────────────

    private readonly Mock<IAuthService>    _authMock    = new();
    private readonly Mock<ILogService>     _logMock     = new();
    private readonly Mock<IUserAgentParser> _uaMock     = new();
    private readonly Mock<IGeoIpService>   _geoMock     = new();

    private AuthController CreateController(int? userId = null)
    {
        var controller = new AuthController(
            _authMock.Object, _logMock.Object, _uaMock.Object, _geoMock.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0)";

        if (userId.HasValue)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.Value.ToString()),
                new(ClaimTypes.Role, "Agent")
            };
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        }

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    private void SetupDefaultMocks()
    {
        _uaMock.Setup(u => u.Parse(It.IsAny<string>())).Returns(("Windows", "Chrome"));
        _geoMock.Setup(g => g.GetLocalisationAsync(It.IsAny<string>())).ReturnsAsync(("Cameroun", "Yaoundé"));
        _logMock.Setup(l => l.LoggerAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_CredentialsValides_Retourne200AvecToken()
    {
        // Arrange
        SetupDefaultMocks();
        var loginResponse = new LoginResponseDTO
        {
            AccessToken = "jwt_token",
            Expiration = DateTime.UtcNow.AddMinutes(60),
            DoitChangerMotDePasse = false,
            Utilisateur = new UtilisateurInfoDTO { Id = 1, Email = "jean@test.com", Role = "Agent" }
        };
        _authMock.Setup(a => a.LoginAsync(It.IsAny<LoginRequestDTO>())).ReturnsAsync(loginResponse);

        var controller = CreateController();

        // Act
        var result = await controller.Login(new LoginRequestDTO
        {
            Email = "jean@test.com", MotDePasse = "password"
        });

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<LoginResponseDTO>()
            .Which.AccessToken.Should().Be("jwt_token");
    }

    [Fact]
    public async Task Login_CredentialsInvalides_Retourne401()
    {
        // Arrange
        _authMock.Setup(a => a.LoginAsync(It.IsAny<LoginRequestDTO>()))
            .ThrowsAsync(new UnauthorizedAccessException("Email ou mot de passe incorrect."));

        var controller = CreateController();

        // Act
        var result = await controller.Login(new LoginRequestDTO
        {
            Email = "inconnu@test.com", MotDePasse = "mauvais"
        });

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_LogueConnexionApresSucces()
    {
        // Arrange
        SetupDefaultMocks();
        var loginResponse = new LoginResponseDTO
        {
            AccessToken = "token",
            Utilisateur = new UtilisateurInfoDTO { Id = 5, Email = "jean@test.com", Role = "Agent" }
        };
        _authMock.Setup(a => a.LoginAsync(It.IsAny<LoginRequestDTO>())).ReturnsAsync(loginResponse);

        var controller = CreateController();

        // Act
        await controller.Login(new LoginRequestDTO { Email = "jean@test.com", MotDePasse = "pwd" });

        // Assert — le log doit être créé avec l'action login
        _logMock.Verify(l => l.LoggerAsync(
            It.Is<string>(s => s.Contains("login")),
            It.IsAny<string>(),
            It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.Is<int?>(id => id == 5),
            It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    // ── Logout ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Logout_UtilisateurConnecte_Retourne204()
    {
        // Arrange
        _authMock.Setup(a => a.LogoutAsync(1)).Returns(Task.CompletedTask);
        var controller = CreateController(userId: 1);

        // Act
        var result = await controller.Logout();

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _authMock.Verify(a => a.LogoutAsync(1), Times.Once);
    }

    // ── ChangePassword ────────────────────────────────────────────────────────

    [Fact]
    public async Task ChangePassword_MdpValide_Retourne204()
    {
        // Arrange
        _authMock.Setup(a => a.ChangerMotDePasseAsync(1, It.IsAny<ChangePasswordDTO>()))
            .Returns(Task.CompletedTask);
        var controller = CreateController(userId: 1);

        // Act
        var result = await controller.ChangePassword(new ChangePasswordDTO
        {
            AncienMotDePasse = "ancien",
            NouveauMotDePasse = "nouveau_mdp_12"
        });

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task ChangePassword_AncienMdpIncorrect_Retourne401()
    {
        // Arrange
        _authMock.Setup(a => a.ChangerMotDePasseAsync(1, It.IsAny<ChangePasswordDTO>()))
            .ThrowsAsync(new UnauthorizedAccessException("Mot de passe actuel incorrect."));
        var controller = CreateController(userId: 1);

        // Act
        var result = await controller.ChangePassword(new ChangePasswordDTO
        {
            AncienMotDePasse = "mauvais",
            NouveauMotDePasse = "nouveau_mdp_12"
        });

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task ChangePassword_NouveauMdpTropCourt_Retourne400()
    {
        // Arrange
        _authMock.Setup(a => a.ChangerMotDePasseAsync(1, It.IsAny<ChangePasswordDTO>()))
            .ThrowsAsync(new ArgumentException("Le nouveau mot de passe doit contenir au moins 8 caractères."));
        var controller = CreateController(userId: 1);

        // Act
        var result = await controller.ChangePassword(new ChangePasswordDTO
        {
            AncienMotDePasse = "ancien",
            NouveauMotDePasse = "court"
        });

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
