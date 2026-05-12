using System.Security.Claims;
using FinAssist.API.Controllers;
using FinAssist.Core.DTOs.Besoins;
using FinAssist.Core.DTOs.Workflow;
using FinAssist.Core.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FinAssist.Tests.Controllers;

/// <summary>
/// Tests unitaires pour CategoriesController.GetDisponibles —
/// vérifie le filtrage des catégories selon le rôle JWT de l'utilisateur.
/// </summary>
public class CategoriesControllerTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static CategoriesController CreateController(Mock<IBesoinsService> serviceMock, string roleCode)
    {
        var controller = new CategoriesController(serviceMock.Object);

        // Simuler un utilisateur authentifié avec le rôle donné
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "1"),
            new(ClaimTypes.Role, roleCode)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        return controller;
    }

    private static CategorieDTO Cat(int id, string nom, int? circuitId = null) => new()
    {
        Id = id, Nom = nom, WorkflowCircuitId = circuitId, DateCreation = DateTime.UtcNow
    };

    private static CategorieDetailDTO Detail(int id, string nom, params string[] roles) => new()
    {
        Id = id, Nom = nom, WorkflowCircuitId = roles.Length > 0 ? 1 : null,
        Circuit = roles.Length > 0 ? new WorkflowCircuitDTO
        {
            Id = 1, Nom = "Circuit test",
            Etapes = roles.Select((r, i) => new EtapeCircuitDTO
            {
                Id = i + 1, Ordre = i + 1, RoleRequis = r,
                ApprobationRequise = true, EstDerniereEtape = i == roles.Length - 1
            }).ToList()
        } : null
    };

    // ── Tests GetDisponibles ──────────────────────────────────────────────────

    [Fact]
    public async Task GetDisponibles_ExclutCategoriesDontCircuitContientRoleUtilisateur()
    {
        // Arrange
        // Catégorie 1 : circuit avec Responsable (doit être exclue pour un Responsable)
        // Catégorie 2 : circuit avec Direction (doit être incluse pour un Responsable)
        // Catégorie 3 : pas de circuit (toujours incluse)
        var serviceMock = new Mock<IBesoinsService>();

        serviceMock.Setup(s => s.GetAllCategoriesAsync()).ReturnsAsync(new List<CategorieDTO>
        {
            Cat(1, "Informatique", circuitId: 1),
            Cat(2, "RH", circuitId: 2),
            Cat(3, "Logistique", circuitId: null)
        });

        serviceMock.Setup(s => s.GetCategorieByIdAsync(1))
            .ReturnsAsync(Detail(1, "Informatique", "Responsable"));

        serviceMock.Setup(s => s.GetCategorieByIdAsync(2))
            .ReturnsAsync(Detail(2, "RH", "Direction"));

        var controller = CreateController(serviceMock, "Responsable");

        // Act
        var result = await controller.GetDisponibles();

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var disponibles = ok.Value.Should().BeAssignableTo<IEnumerable<CategorieDTO>>().Subject.ToList();

        disponibles.Should().HaveCount(2);
        disponibles.Select(c => c.Nom).Should().Contain("RH");
        disponibles.Select(c => c.Nom).Should().Contain("Logistique");
        disponibles.Select(c => c.Nom).Should().NotContain("Informatique");
    }

    [Fact]
    public async Task GetDisponibles_CategoriesSansCircuit_ToujoursIncluses()
    {
        // Arrange
        var serviceMock = new Mock<IBesoinsService>();

        serviceMock.Setup(s => s.GetAllCategoriesAsync()).ReturnsAsync(new List<CategorieDTO>
        {
            Cat(1, "Sans circuit", circuitId: null),
            Cat(2, "Aussi sans circuit", circuitId: null)
        });

        var controller = CreateController(serviceMock, "Responsable");

        // Act
        var result = await controller.GetDisponibles();

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var disponibles = ok.Value.Should().BeAssignableTo<IEnumerable<CategorieDTO>>().Subject.ToList();
        disponibles.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetDisponibles_ToutesCategories_ExcluesSiRolePresent()
    {
        // Arrange — un Administrateur dont le rôle est dans tous les circuits
        var serviceMock = new Mock<IBesoinsService>();

        serviceMock.Setup(s => s.GetAllCategoriesAsync()).ReturnsAsync(new List<CategorieDTO>
        {
            Cat(1, "Cat1", circuitId: 1),
            Cat(2, "Cat2", circuitId: 2)
        });

        serviceMock.Setup(s => s.GetCategorieByIdAsync(1))
            .ReturnsAsync(Detail(1, "Cat1", "Administrateur"));

        serviceMock.Setup(s => s.GetCategorieByIdAsync(2))
            .ReturnsAsync(Detail(2, "Cat2", "Administrateur"));

        var controller = CreateController(serviceMock, "Administrateur");

        // Act
        var result = await controller.GetDisponibles();

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var disponibles = ok.Value.Should().BeAssignableTo<IEnumerable<CategorieDTO>>().Subject.ToList();
        disponibles.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDisponibles_ComparaisonInsensibleCasse()
    {
        // Arrange — rôle "responsable" en minuscule dans le circuit, "Responsable" dans le JWT
        var serviceMock = new Mock<IBesoinsService>();

        serviceMock.Setup(s => s.GetAllCategoriesAsync()).ReturnsAsync(new List<CategorieDTO>
        {
            Cat(1, "Informatique", circuitId: 1)
        });

        serviceMock.Setup(s => s.GetCategorieByIdAsync(1))
            .ReturnsAsync(Detail(1, "Informatique", "responsable")); // minuscule

        var controller = CreateController(serviceMock, "Responsable"); // majuscule

        // Act
        var result = await controller.GetDisponibles();

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var disponibles = ok.Value.Should().BeAssignableTo<IEnumerable<CategorieDTO>>().Subject.ToList();
        disponibles.Should().BeEmpty(); // exclue car même rôle (insensible à la casse)
    }

    [Fact]
    public async Task GetDisponibles_CircuitMultiEtapes_ExclutSiRolePresentDansUneEtape()
    {
        // Arrange — circuit avec Responsable ET Direction
        // Un Responsable ne doit pas voir cette catégorie
        var serviceMock = new Mock<IBesoinsService>();

        serviceMock.Setup(s => s.GetAllCategoriesAsync()).ReturnsAsync(new List<CategorieDTO>
        {
            Cat(1, "Multi-étapes", circuitId: 1)
        });

        serviceMock.Setup(s => s.GetCategorieByIdAsync(1))
            .ReturnsAsync(Detail(1, "Multi-étapes", "Responsable", "Direction"));

        var controller = CreateController(serviceMock, "Responsable");

        // Act
        var result = await controller.GetDisponibles();

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var disponibles = ok.Value.Should().BeAssignableTo<IEnumerable<CategorieDTO>>().Subject.ToList();
        disponibles.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDisponibles_AucuneCategorie_RetourneListeVide()
    {
        // Arrange
        var serviceMock = new Mock<IBesoinsService>();
        serviceMock.Setup(s => s.GetAllCategoriesAsync()).ReturnsAsync(new List<CategorieDTO>());

        var controller = CreateController(serviceMock, "Responsable");

        // Act
        var result = await controller.GetDisponibles();

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var disponibles = ok.Value.Should().BeAssignableTo<IEnumerable<CategorieDTO>>().Subject.ToList();
        disponibles.Should().BeEmpty();
    }
}
