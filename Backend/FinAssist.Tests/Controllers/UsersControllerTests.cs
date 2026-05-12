using System.Security.Claims;
using FinAssist.API.Controllers;
using FinAssist.Core.DTOs.Users;
using FinAssist.Core.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FinAssist.Tests.Controllers;

/// <summary>
/// Tests unitaires pour UsersController — CRUD utilisateurs et gestion des mots de passe.
/// </summary>
public class UsersControllerTests
{
    // ── Setup ─────────────────────────────────────────────────────────────────

    private readonly Mock<IUsersService>          _usersMock = new();
    private readonly Mock<IPermissionsRepository> _permsMock = new();
    private readonly Mock<IPermissionService>     _permSvcMock = new();

    private UsersController CreateController(int userId = 1, string role = "Administrateur")
    {
        var controller = new UsersController(_usersMock.Object, _permsMock.Object, _permSvcMock.Object);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, role)
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

    private static UtilisateurDTO UserDTO(int id = 1) => new()
    {
        Id = id, Nom = "Dupont", Prenom = "Jean",
        Email = $"jean{id}@finstar-cm.com", Role = "Agent", Actif = true
    };

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_Retourne200AvecListe()
    {
        // Arrange
        _usersMock.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<UtilisateurDTO> { UserDTO(1), UserDTO(2) });
        var controller = CreateController();

        // Act
        var result = await controller.GetAll();

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IEnumerable<UtilisateurDTO>>()
            .Which.Should().HaveCount(2);
    }

    // ── GetMe ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMe_UtilisateurConnecte_Retourne200()
    {
        // Arrange
        _usersMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(UserDTO());
        var controller = CreateController(userId: 1);

        // Act
        var result = await controller.GetMe();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_UtilisateurExistant_Retourne200()
    {
        // Arrange
        _usersMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(UserDTO());
        var controller = CreateController();

        // Act
        var result = await controller.GetById(1);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_UtilisateurInexistant_Retourne404()
    {
        // Arrange
        _usersMock.Setup(s => s.GetByIdAsync(99)).ThrowsAsync(new KeyNotFoundException("Utilisateur 99 introuvable."));
        var controller = CreateController();

        // Act
        var result = await controller.GetById(99);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_EmailValide_Retourne201()
    {
        // Arrange
        var dto = new CreateUtilisateurDTO { Nom = "Martin", Prenom = "Paul", Email = "paul@finstar-cm.com", RoleId = 1 };
        _usersMock.Setup(s => s.CreateAsync(dto)).ReturnsAsync(UserDTO(2));
        var controller = CreateController();

        // Act
        var result = await controller.Create(dto);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task Create_EmailDomainInvalide_Retourne400()
    {
        // Arrange
        var dto = new CreateUtilisateurDTO { Nom = "Martin", Prenom = "Paul", Email = "paul@gmail.com", RoleId = 1 };
        var controller = CreateController();

        // Act
        var result = await controller.Create(dto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Create_EmailDuplique_Retourne409()
    {
        // Arrange
        var dto = new CreateUtilisateurDTO { Nom = "Martin", Prenom = "Paul", Email = "paul@finstar-cm.com", RoleId = 1 };
        _usersMock.Setup(s => s.CreateAsync(dto)).ThrowsAsync(new InvalidOperationException("Email déjà utilisé."));
        var controller = CreateController();

        // Act
        var result = await controller.Create(dto);

        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
    }

    // ── Deactivate ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Deactivate_CompteActif_Retourne204()
    {
        // Arrange
        _usersMock.Setup(s => s.DeactivateAsync(1)).Returns(Task.CompletedTask);
        var controller = CreateController();

        // Act
        var result = await controller.Deactivate(1);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Deactivate_CompteDejaDesactive_Retourne409()
    {
        // Arrange
        _usersMock.Setup(s => s.DeactivateAsync(1)).ThrowsAsync(new InvalidOperationException("Déjà désactivé."));
        var controller = CreateController();

        // Act
        var result = await controller.Deactivate(1);

        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
    }

    // ── Activate ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Activate_CompteDesactive_Retourne204()
    {
        // Arrange
        _usersMock.Setup(s => s.ActivateAsync(1)).Returns(Task.CompletedTask);
        var controller = CreateController();

        // Act
        var result = await controller.Activate(1);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_Administrateur_SansActions_Retourne204()
    {
        // Arrange
        _usersMock.Setup(s => s.DeleteAsync(1)).Returns(Task.CompletedTask);
        var controller = CreateController(role: "Administrateur");

        // Act
        var result = await controller.Delete(1);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_NonAdministrateur_Retourne403()
    {
        // Arrange
        var controller = CreateController(role: "Responsable");

        // Act
        var result = await controller.Delete(1);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Delete_AvecActions_Retourne409()
    {
        // Arrange
        _usersMock.Setup(s => s.DeleteAsync(1)).ThrowsAsync(new InvalidOperationException("A des actions."));
        var controller = CreateController(role: "Administrateur");

        // Act
        var result = await controller.Delete(1);

        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
    }

    // ── ChangePassword ────────────────────────────────────────────────────────

    [Fact]
    public async Task ChangePassword_MdpValide_Retourne200()
    {
        // Arrange
        _usersMock.Setup(s => s.ChangePasswordAsync(1, "ancien", "nouveau_mdp_12")).Returns(Task.CompletedTask);
        var controller = CreateController(userId: 1);

        // Act
        var result = await controller.ChangePassword(new ChangePasswordDTO
        {
            AncienMotDePasse = "ancien",
            NouveauMotDePasse = "nouveau_mdp_12"
        });

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ChangePassword_MdpTropCourt_Retourne400()
    {
        // Arrange
        var controller = CreateController(userId: 1);

        // Act
        var result = await controller.ChangePassword(new ChangePasswordDTO
        {
            AncienMotDePasse = "ancien",
            NouveauMotDePasse = "court" // < 8 chars
        });

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
