using FinAssist.Application.Services;
using FinAssist.Core.DTOs.Auth;
using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;
using FinAssist.Infrastructure.Data;
using FinAssist.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FinAssist.Tests.Integration;

/// <summary>
/// Tests d'intégration pour AuthService + AuthRepository + AppDbContext (InMemory).
/// </summary>
public class AuthRepositoryIntegrationTests
{
    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    private static AuthService CreateService(AppDbContext db, bool passwordValid = true)
    {
        var repo = new AuthRepository(db);
        var pwdMock = new Mock<IPasswordService>();
        pwdMock.Setup(p => p.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(passwordValid);
        pwdMock.Setup(p => p.Hash(It.IsAny<string>())).Returns("hashed_new");
        var jwtMock = new Mock<IJwtTokenService>();
        jwtMock.Setup(j => j.GenerateAccessToken(It.IsAny<Utilisateur>(), It.IsAny<IEnumerable<string>>()))
               .Returns("jwt_token_test");
        var permsMock = new Mock<IPermissionsRepository>();
        permsMock.Setup(p => p.GetPermissionsEffectivesAsync(It.IsAny<int>()))
                 .ReturnsAsync(new List<string> { "BESOIN_CONSULTER" });
        return new AuthService(repo, pwdMock.Object, jwtMock.Object, permsMock.Object);
    }

    private static async Task<(Role role, Utilisateur user)> SeedUserAsync(AppDbContext db, bool actif = true)
    {
        var role = new Role { Code = "Agent", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow };
        db.Roles.Add(role);
        var user = new Utilisateur
        {
            Nom = "Dupont", Prenom = "Jean",
            Email = "jean@finstar-cm.com",
            MotDePasse = "hashed_password",
            RoleId = role.Id, Actif = actif,
            DoitChangerMotDePasse = false,
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow
        };
        db.Utilisateurs.Add(user);
        await db.SaveChangesAsync();
        return (role, user);
    }

    // ── LoginAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_CredentialsValides_RetourneToken()
    {
        // Arrange
        using var db = CreateDb(nameof(LoginAsync_CredentialsValides_RetourneToken));
        await SeedUserAsync(db);
        var service = CreateService(db, passwordValid: true);

        // Act
        var result = await service.LoginAsync(new LoginRequestDTO
        {
            Email = "jean@finstar-cm.com",
            MotDePasse = "password123"
        });

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("jwt_token_test");
        result.Utilisateur.Email.Should().Be("jean@finstar-cm.com");
        result.Utilisateur.Role.Should().Be("Agent");
    }

    [Fact]
    public async Task LoginAsync_EmailInexistant_LeveUnauthorized()
    {
        // Arrange
        using var db = CreateDb(nameof(LoginAsync_EmailInexistant_LeveUnauthorized));
        var service = CreateService(db);

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
    public async Task LoginAsync_CompteDesactive_LeveUnauthorized()
    {
        // Arrange
        using var db = CreateDb(nameof(LoginAsync_CompteDesactive_LeveUnauthorized));
        await SeedUserAsync(db, actif: false);
        var service = CreateService(db, passwordValid: true);

        // Act
        var act = async () => await service.LoginAsync(new LoginRequestDTO
        {
            Email = "jean@finstar-cm.com",
            MotDePasse = "password"
        });

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*désactivé*");
    }

    [Fact]
    public async Task LoginAsync_MotDePasseIncorrect_LeveUnauthorized()
    {
        // Arrange
        using var db = CreateDb(nameof(LoginAsync_MotDePasseIncorrect_LeveUnauthorized));
        await SeedUserAsync(db);
        var service = CreateService(db, passwordValid: false); // mot de passe invalide

        // Act
        var act = async () => await service.LoginAsync(new LoginRequestDTO
        {
            Email = "jean@finstar-cm.com",
            MotDePasse = "mauvais_mdp"
        });

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*incorrect*");
    }

    // ── ChangerMotDePasseAsync ────────────────────────────────────────────────

    [Fact]
    public async Task ChangerMotDePasseAsync_AncienMdpCorrect_MettreAJour()
    {
        // Arrange
        using var db = CreateDb(nameof(ChangerMotDePasseAsync_AncienMdpCorrect_MettreAJour));
        var (_, user) = await SeedUserAsync(db);
        var service = CreateService(db, passwordValid: true);

        // Act
        await service.ChangerMotDePasseAsync(user.Id, new ChangePasswordDTO
        {
            AncienMotDePasse = "ancien",
            NouveauMotDePasse = "nouveau_mdp_12"
        });

        // Assert
        var inDb = await db.Utilisateurs.FindAsync(user.Id);
        inDb!.MotDePasse.Should().Be("hashed_new");
        inDb.DoitChangerMotDePasse.Should().BeFalse();
    }

    // ── AuthRepository.GetByEmailAsync ────────────────────────────────────────

    [Fact]
    public async Task GetByEmailAsync_EmailExistant_RetourneUtilisateurAvecRole()
    {
        // Arrange
        using var db = CreateDb(nameof(GetByEmailAsync_EmailExistant_RetourneUtilisateurAvecRole));
        await SeedUserAsync(db);
        var repo = new AuthRepository(db);

        // Act
        var result = await repo.GetByEmailAsync("jean@finstar-cm.com");

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("jean@finstar-cm.com");
        result.Role.Should().NotBeNull();
        result.Role.Code.Should().Be("Agent");
    }

    [Fact]
    public async Task GetByEmailAsync_EmailInexistant_RetourneNull()
    {
        // Arrange
        using var db = CreateDb(nameof(GetByEmailAsync_EmailInexistant_RetourneNull));
        var repo = new AuthRepository(db);

        // Act
        var result = await repo.GetByEmailAsync("inconnu@test.com");

        // Assert
        result.Should().BeNull();
    }
}
