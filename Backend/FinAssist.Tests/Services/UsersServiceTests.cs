using FinAssist.Application.Services;
using FinAssist.Core.DTOs.Users;
using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;
using FluentAssertions;
using Moq;

namespace FinAssist.Tests.Services;

/// <summary>
/// Tests unitaires pour UsersService — CRUD utilisateurs, activation/désactivation.
/// </summary>
public class UsersServiceTests
{
    // ── Setup ─────────────────────────────────────────────────────────────────

    private readonly Mock<IUsersRepository>      _repoMock    = new();
    private readonly Mock<IPasswordService>      _pwdMock     = new();
    private readonly Mock<IPermissionsRepository> _permsMock  = new();
    private readonly Mock<IEmailService>         _emailMock   = new();

    private UsersService CreateService() =>
        new(_repoMock.Object, _pwdMock.Object, _permsMock.Object, _emailMock.Object);

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Utilisateur User(int id = 1, bool actif = true) => new()
    {
        Id = id, Nom = "Dupont", Prenom = "Jean",
        Email = $"jean{id}@finstar-cm.com",
        MotDePasse = "hashed",
        Actif = actif,
        RoleId = 1,
        Role = new Role { Id = 1, Code = "Agent" },
        DateCreation = DateTime.UtcNow,
        DateModification = DateTime.UtcNow
    };

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_EmailDomainValide_CreeLUtilisateur()
    {
        // Arrange
        var dto = new CreateUtilisateurDTO
        {
            Nom = "Martin", Prenom = "Paul",
            Email = "paul@finstar-cm.com", RoleId = 1
        };
        var created = User(2);
        created.Email = dto.Email;

        _repoMock.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync((Utilisateur?)null);
        _pwdMock.Setup(p => p.Hash(It.IsAny<string>())).Returns("hashed_pwd");
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Utilisateur>())).ReturnsAsync(created);
        _repoMock.Setup(r => r.AddLogAsync(It.IsAny<LogUtilisateur>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.GetByIdAsync(created.Id)).ReturnsAsync(created);
        _emailMock.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

        var service = CreateService();

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        _repoMock.Verify(r => r.CreateAsync(It.Is<Utilisateur>(u =>
            u.DoitChangerMotDePasse && u.Actif)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_EmailDomainInvalide_LeveInvalidOperation()
    {
        // Arrange
        var dto = new CreateUtilisateurDTO
        {
            Nom = "Martin", Prenom = "Paul",
            Email = "paul@gmail.com", RoleId = 1
        };
        var service = CreateService();

        // Act
        var act = async () => await service.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*finstar-cm.com*");
    }

    [Fact]
    public async Task CreateAsync_EmailDuplique_LeveInvalidOperation()
    {
        // Arrange
        var dto = new CreateUtilisateurDTO
        {
            Nom = "Martin", Prenom = "Paul",
            Email = "paul@finstar-cm.com", RoleId = 1
        };
        _repoMock.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync(User());

        var service = CreateService();

        // Act
        var act = async () => await service.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*existe déjà*");
    }

    [Fact]
    public async Task CreateAsync_EmailEnvoiEchoue_RetourneWarning()
    {
        // Arrange
        var dto = new CreateUtilisateurDTO
        {
            Nom = "Martin", Prenom = "Paul",
            Email = "paul@finstar-cm.com", RoleId = 1
        };
        var created = User(2);
        created.Email = dto.Email;

        _repoMock.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync((Utilisateur?)null);
        _pwdMock.Setup(p => p.Hash(It.IsAny<string>())).Returns("hashed");
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Utilisateur>())).ReturnsAsync(created);
        _repoMock.Setup(r => r.AddLogAsync(It.IsAny<LogUtilisateur>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.GetByIdAsync(created.Id)).ReturnsAsync(created);
        _emailMock.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false); // échec email

        var service = CreateService();

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        result.EmailWarning.Should().NotBeNullOrEmpty();
        result.EmailWarning.Should().Contain("email");
    }

    // ── DeactivateAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task DeactivateAsync_CompteActif_Desactive()
    {
        // Arrange
        var user = User(actif: true);
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Utilisateur>())).ReturnsAsync(new Utilisateur());
        _repoMock.Setup(r => r.AddLogAsync(It.IsAny<LogUtilisateur>())).Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        await service.DeactivateAsync(1);

        // Assert
        _repoMock.Verify(r => r.UpdateAsync(It.Is<Utilisateur>(u => !u.Actif)), Times.Once);
        _repoMock.Verify(r => r.AddLogAsync(It.Is<LogUtilisateur>(l => l.Action == "DESACTIVATION")), Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_CompteDejaDesactive_LeveInvalidOperation()
    {
        // Arrange
        var user = User(actif: false);
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        var service = CreateService();

        // Act
        var act = async () => await service.DeactivateAsync(1);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*déjà désactivé*");
    }

    // ── ActivateAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ActivateAsync_CompteDesactive_Active()
    {
        // Arrange
        var user = User(actif: false);
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Utilisateur>())).ReturnsAsync(new Utilisateur());
        _repoMock.Setup(r => r.AddLogAsync(It.IsAny<LogUtilisateur>())).Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        await service.ActivateAsync(1);

        // Assert
        _repoMock.Verify(r => r.UpdateAsync(It.Is<Utilisateur>(u => u.Actif)), Times.Once);
        _repoMock.Verify(r => r.AddLogAsync(It.Is<LogUtilisateur>(l => l.Action == "ACTIVATION")), Times.Once);
    }

    [Fact]
    public async Task ActivateAsync_CompteDejaActif_LeveInvalidOperation()
    {
        // Arrange
        var user = User(actif: true);
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        var service = CreateService();

        // Act
        var act = async () => await service.ActivateAsync(1);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*déjà actif*");
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_SansActions_Supprime()
    {
        // Arrange
        var user = User();
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _repoMock.Setup(r => r.HasActionsAsync(1)).ReturnsAsync(false);
        _repoMock.Setup(r => r.DeleteCascadeAsync(1)).Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        await service.DeleteAsync(1);

        // Assert
        _repoMock.Verify(r => r.DeleteCascadeAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_AvecActions_LeveInvalidOperation()
    {
        // Arrange
        var user = User();
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _repoMock.Setup(r => r.HasActionsAsync(1)).ReturnsAsync(true);

        var service = CreateService();

        // Act
        var act = async () => await service.DeleteAsync(1);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*désactiver*");
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ModifieNomEtPrenom_LogueChangements()
    {
        // Arrange
        var user = User();
        var dto = new UpdateUtilisateurDTO { Nom = "Martin", Prenom = "Pierre" };

        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Utilisateur>())).ReturnsAsync(new Utilisateur());
        _repoMock.Setup(r => r.AddLogAsync(It.IsAny<LogUtilisateur>())).Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        await service.UpdateAsync(1, dto);

        // Assert
        _repoMock.Verify(r => r.UpdateAsync(It.Is<Utilisateur>(u =>
            u.Nom == "Martin" && u.Prenom == "Pierre")), Times.Once);
        _repoMock.Verify(r => r.AddLogAsync(It.Is<LogUtilisateur>(l =>
            l.Action == "MODIFICATION")), Times.Once);
    }

    // ── ChangePasswordAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task ChangePasswordAsync_AncienMdpCorrect_MettreAJour()
    {
        // Arrange
        var user = User();
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _pwdMock.Setup(p => p.Verify("ancien", "hashed")).Returns(true);
        _pwdMock.Setup(p => p.Hash("nouveau")).Returns("new_hashed");
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Utilisateur>())).ReturnsAsync(new Utilisateur());
        _repoMock.Setup(r => r.AddLogAsync(It.IsAny<LogUtilisateur>())).Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        await service.ChangePasswordAsync(1, "ancien", "nouveau");

        // Assert
        _repoMock.Verify(r => r.UpdateAsync(It.Is<Utilisateur>(u =>
            u.MotDePasse == "new_hashed")), Times.Once);
        _repoMock.Verify(r => r.AddLogAsync(It.Is<LogUtilisateur>(l =>
            l.Action == "CHANGEMENT_MOT_DE_PASSE")), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_AncienMdpIncorrect_LeveUnauthorized()
    {
        // Arrange
        var user = User();
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _pwdMock.Setup(p => p.Verify("mauvais", "hashed")).Returns(false);

        var service = CreateService();

        // Act
        var act = async () => await service.ChangePasswordAsync(1, "mauvais", "nouveau");

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*actuel incorrect*");
    }
}
