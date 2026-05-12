using FinAssist.Application.Services;
using FinAssist.Core.DTOs.Auth;
using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;
using FluentAssertions;
using Moq;

namespace FinAssist.Tests.Services;

/// <summary>
/// Tests unitaires pour AuthService — login, logout et changement de mot de passe.
/// </summary>
public class AuthServiceTests
{
    // ── Setup ─────────────────────────────────────────────────────────────────

    private readonly Mock<IAuthRepository>       _authRepoMock   = new();
    private readonly Mock<IPasswordService>      _passwordMock   = new();
    private readonly Mock<IJwtTokenService>      _jwtMock        = new();
    private readonly Mock<IPermissionsRepository> _permsMock     = new();

    private AuthService CreateService() =>
        new(_authRepoMock.Object, _passwordMock.Object, _jwtMock.Object, _permsMock.Object);

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Utilisateur UtilisateurActif(int id = 1) => new()
    {
        Id = id, Nom = "Dupont", Prenom = "Jean",
        Email = "jean@finstar-cm.com",
        MotDePasse = "hashed_password",
        Actif = true,
        DoitChangerMotDePasse = false,
        Role = new Role { Id = 1, Code = "Agent" },
        RoleId = 1
    };

    // ── LoginAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_CredentialsValides_RetourneToken()
    {
        // Arrange
        var user = UtilisateurActif();
        _authRepoMock.Setup(r => r.GetByEmailAsync("jean@finstar-cm.com")).ReturnsAsync(user);
        _passwordMock.Setup(p => p.Verify("password123", "hashed_password")).Returns(true);
        _permsMock.Setup(p => p.GetPermissionsEffectivesAsync(1)).ReturnsAsync(new List<string> { "BESOIN_CONSULTER" });
        _jwtMock.Setup(j => j.GenerateAccessToken(user, It.IsAny<IEnumerable<string>>())).Returns("jwt_token_abc");

        var service = CreateService();

        // Act
        var result = await service.LoginAsync(new LoginRequestDTO
        {
            Email = "jean@finstar-cm.com",
            MotDePasse = "password123"
        });

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("jwt_token_abc");
        result.Utilisateur.Email.Should().Be("jean@finstar-cm.com");
        result.Utilisateur.Role.Should().Be("Agent");
    }

    [Fact]
    public async Task LoginAsync_EmailInexistant_LeveUnauthorized()
    {
        // Arrange
        _authRepoMock.Setup(r => r.GetByEmailAsync("inconnu@test.com")).ReturnsAsync((Utilisateur?)null);
        var service = CreateService();

        // Act
        var act = async () => await service.LoginAsync(new LoginRequestDTO
        {
            Email = "inconnu@test.com",
            MotDePasse = "password"
        });

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*incorrect*");
    }

    [Fact]
    public async Task LoginAsync_MotDePasseIncorrect_LeveUnauthorized()
    {
        // Arrange
        var user = UtilisateurActif();
        _authRepoMock.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        _passwordMock.Setup(p => p.Verify("mauvais_mdp", "hashed_password")).Returns(false);
        var service = CreateService();

        // Act
        var act = async () => await service.LoginAsync(new LoginRequestDTO
        {
            Email = user.Email,
            MotDePasse = "mauvais_mdp"
        });

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*incorrect*");
    }

    [Fact]
    public async Task LoginAsync_CompteDesactive_LeveUnauthorized()
    {
        // Arrange
        var user = UtilisateurActif();
        user.Actif = false;
        _authRepoMock.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        var service = CreateService();

        // Act
        var act = async () => await service.LoginAsync(new LoginRequestDTO
        {
            Email = user.Email,
            MotDePasse = "password"
        });

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*désactivé*");
    }

    [Fact]
    public async Task LoginAsync_DoitChangerMotDePasse_RetourneFlag()
    {
        // Arrange
        var user = UtilisateurActif();
        user.DoitChangerMotDePasse = true;
        _authRepoMock.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        _passwordMock.Setup(p => p.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        _permsMock.Setup(p => p.GetPermissionsEffectivesAsync(1)).ReturnsAsync(new List<string>());
        _jwtMock.Setup(j => j.GenerateAccessToken(It.IsAny<Utilisateur>(), It.IsAny<IEnumerable<string>>())).Returns("token");

        var service = CreateService();

        // Act
        var result = await service.LoginAsync(new LoginRequestDTO { Email = user.Email, MotDePasse = "pwd" });

        // Assert
        result.DoitChangerMotDePasse.Should().BeTrue();
    }

    // ── ChangerMotDePasseAsync ────────────────────────────────────────────────

    [Fact]
    public async Task ChangerMotDePasseAsync_AncienMdpCorrect_MettreAJour()
    {
        // Arrange
        var user = UtilisateurActif();
        _authRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _passwordMock.Setup(p => p.Verify("ancien_mdp", "hashed_password")).Returns(true);
        _passwordMock.Setup(p => p.Hash("nouveau_mdp_12")).Returns("new_hashed");
        _authRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Utilisateur>())).Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        await service.ChangerMotDePasseAsync(1, new ChangePasswordDTO
        {
            AncienMotDePasse = "ancien_mdp",
            NouveauMotDePasse = "nouveau_mdp_12"
        });

        // Assert
        _authRepoMock.Verify(r => r.UpdateAsync(It.Is<Utilisateur>(u =>
            u.MotDePasse == "new_hashed" && !u.DoitChangerMotDePasse)), Times.Once);
    }

    [Fact]
    public async Task ChangerMotDePasseAsync_AncienMdpIncorrect_LeveUnauthorized()
    {
        // Arrange
        var user = UtilisateurActif();
        _authRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _passwordMock.Setup(p => p.Verify("mauvais", "hashed_password")).Returns(false);
        var service = CreateService();

        // Act
        var act = async () => await service.ChangerMotDePasseAsync(1, new ChangePasswordDTO
        {
            AncienMotDePasse = "mauvais",
            NouveauMotDePasse = "nouveau_mdp_12"
        });

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*actuel incorrect*");
    }

    [Fact]
    public async Task ChangerMotDePasseAsync_NouveauMdpTropCourt_LeveArgumentException()
    {
        // Arrange
        var user = UtilisateurActif();
        _authRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _passwordMock.Setup(p => p.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        var service = CreateService();

        // Act
        var act = async () => await service.ChangerMotDePasseAsync(1, new ChangePasswordDTO
        {
            AncienMotDePasse = "ancien",
            NouveauMotDePasse = "court" // < 8 caractères
        });

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*8 caractères*");
    }

    [Fact]
    public async Task ChangerMotDePasseAsync_UtilisateurInexistant_LeveUnauthorized()
    {
        // Arrange
        _authRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Utilisateur?)null);
        var service = CreateService();

        // Act
        var act = async () => await service.ChangerMotDePasseAsync(99, new ChangePasswordDTO
        {
            AncienMotDePasse = "ancien",
            NouveauMotDePasse = "nouveau_mdp_12"
        });

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    // ── LogoutAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task LogoutAsync_CompleteSansErreur()
    {
        // Arrange
        var service = CreateService();

        // Act
        var act = async () => await service.LogoutAsync(1);

        // Assert
        await act.Should().NotThrowAsync();
    }
}
