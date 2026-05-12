using System.Security.Claims;
using FinAssist.API.Controllers;
using FinAssist.Core.DTOs.Workflow;
using FinAssist.Core.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FinAssist.Tests.Controllers;

/// <summary>
/// Tests unitaires pour WorkflowController — validation, transmission et gestion des circuits.
/// </summary>
public class WorkflowControllerTests
{
    // ── Setup ─────────────────────────────────────────────────────────────────

    private static WorkflowController CreateController(Mock<IWorkflowService> serviceMock,
        int userId = 1, string role = "Responsable", string nom = "Jean Dupont")
    {
        var controller = new WorkflowController(serviceMock.Object);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, role),
            new(ClaimTypes.Name, nom)
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

    private static ValidationDTO ValidationDTO() => new()
    {
        Id = 1, Niveau = 1, Decision = "APPROUVE",
        DateDecision = DateTime.UtcNow, ValidateurId = 1, BesoinId = 1
    };

    private static WorkflowCircuitDTO CircuitDTO() => new()
    {
        Id = 1, Nom = "Circuit test", NomCreateur = "Admin",
        DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow,
        Etapes = []
    };

    // ── Valider ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Valider_DecisionValide_Retourne200()
    {
        // Arrange
        var serviceMock = new Mock<IWorkflowService>();
        serviceMock.Setup(s => s.ValiderAsync(1, It.IsAny<ValiderBesoinDTO>(), 1, "Responsable", "Jean Dupont"))
            .ReturnsAsync(ValidationDTO());
        var controller = CreateController(serviceMock);

        // Act
        var result = await controller.Valider(1, new ValiderBesoinDTO { Decision = "APPROUVE" });

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Valider_BesoinInexistant_Retourne404()
    {
        // Arrange
        var serviceMock = new Mock<IWorkflowService>();
        serviceMock.Setup(s => s.ValiderAsync(99, It.IsAny<ValiderBesoinDTO>(), 1, "Responsable", "Jean Dupont"))
            .ThrowsAsync(new KeyNotFoundException("Besoin 99 introuvable."));
        var controller = CreateController(serviceMock);

        // Act
        var result = await controller.Valider(99, new ValiderBesoinDTO { Decision = "APPROUVE" });

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Valider_DoubleValidation_Retourne409()
    {
        // Arrange
        var serviceMock = new Mock<IWorkflowService>();
        serviceMock.Setup(s => s.ValiderAsync(1, It.IsAny<ValiderBesoinDTO>(), 1, "Responsable", "Jean Dupont"))
            .ThrowsAsync(new InvalidOperationException("Double validation non autorisée."));
        var controller = CreateController(serviceMock);

        // Act
        var result = await controller.Valider(1, new ValiderBesoinDTO { Decision = "APPROUVE" });

        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Valider_MauvaisRole_Retourne403()
    {
        // Arrange
        var serviceMock = new Mock<IWorkflowService>();
        serviceMock.Setup(s => s.ValiderAsync(1, It.IsAny<ValiderBesoinDTO>(), 1, "Agent", "Jean Dupont"))
            .ThrowsAsync(new UnauthorizedAccessException("Rôle incorrect."));
        var controller = CreateController(serviceMock, role: "Agent");

        // Act
        var result = await controller.Valider(1, new ValiderBesoinDTO { Decision = "APPROUVE" });

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Valider_DecisionInvalide_Retourne400()
    {
        // Arrange
        var serviceMock = new Mock<IWorkflowService>();
        serviceMock.Setup(s => s.ValiderAsync(1, It.IsAny<ValiderBesoinDTO>(), 1, "Responsable", "Jean Dupont"))
            .ThrowsAsync(new ArgumentException("La décision doit être APPROUVE ou REJETE."));
        var controller = CreateController(serviceMock);

        // Act
        var result = await controller.Valider(1, new ValiderBesoinDTO { Decision = "INVALIDE" });

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── Transmettre ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Transmettre_BesoinValide_Retourne200()
    {
        // Arrange
        var serviceMock = new Mock<IWorkflowService>();
        serviceMock.Setup(s => s.TransmettreAsync(1, 1, "Jean Dupont"))
            .ReturnsAsync(ValidationDTO());
        var controller = CreateController(serviceMock);

        // Act
        var result = await controller.Transmettre(1);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Transmettre_BesoinInexistant_Retourne404()
    {
        // Arrange
        var serviceMock = new Mock<IWorkflowService>();
        serviceMock.Setup(s => s.TransmettreAsync(99, 1, "Jean Dupont"))
            .ThrowsAsync(new KeyNotFoundException("Besoin 99 introuvable."));
        var controller = CreateController(serviceMock);

        // Act
        var result = await controller.Transmettre(99);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ── GetCircuits ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetCircuits_RetourneListeCircuits()
    {
        // Arrange
        var serviceMock = new Mock<IWorkflowService>();
        serviceMock.Setup(s => s.GetAllCircuitsAsync())
            .ReturnsAsync(new List<WorkflowCircuitDTO> { CircuitDTO(), CircuitDTO() });
        var controller = CreateController(serviceMock);

        // Act
        var result = await controller.GetCircuits();

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IEnumerable<WorkflowCircuitDTO>>()
            .Which.Should().HaveCount(2);
    }

    // ── GetCircuit ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetCircuit_CircuitExistant_Retourne200()
    {
        // Arrange
        var serviceMock = new Mock<IWorkflowService>();
        serviceMock.Setup(s => s.GetCircuitByIdAsync(1)).ReturnsAsync(CircuitDTO());
        var controller = CreateController(serviceMock);

        // Act
        var result = await controller.GetCircuit(1);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetCircuit_CircuitInexistant_Retourne404()
    {
        // Arrange
        var serviceMock = new Mock<IWorkflowService>();
        serviceMock.Setup(s => s.GetCircuitByIdAsync(99))
            .ThrowsAsync(new KeyNotFoundException("Circuit 99 introuvable."));
        var controller = CreateController(serviceMock);

        // Act
        var result = await controller.GetCircuit(99);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ── CreateCircuit ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateCircuit_CircuitValide_Retourne201()
    {
        // Arrange
        var serviceMock = new Mock<IWorkflowService>();
        serviceMock.Setup(s => s.CreateCircuitAsync(It.IsAny<CreateWorkflowCircuitDTO>(), "Jean Dupont"))
            .ReturnsAsync(CircuitDTO());
        var controller = CreateController(serviceMock);

        // Act
        var result = await controller.CreateCircuit(new CreateWorkflowCircuitDTO
        {
            Nom = "Nouveau circuit", Etapes = []
        });

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task CreateCircuit_NomDuplique_Retourne409()
    {
        // Arrange
        var serviceMock = new Mock<IWorkflowService>();
        serviceMock.Setup(s => s.CreateCircuitAsync(It.IsAny<CreateWorkflowCircuitDTO>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Un circuit nommé 'X' existe déjà."));
        var controller = CreateController(serviceMock);

        // Act
        var result = await controller.CreateCircuit(new CreateWorkflowCircuitDTO { Nom = "X", Etapes = [] });

        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task CreateCircuit_SansEtapes_Retourne400()
    {
        // Arrange
        var serviceMock = new Mock<IWorkflowService>();
        serviceMock.Setup(s => s.CreateCircuitAsync(It.IsAny<CreateWorkflowCircuitDTO>(), It.IsAny<string>()))
            .ThrowsAsync(new ArgumentException("Le circuit doit contenir au moins une étape."));
        var controller = CreateController(serviceMock);

        // Act
        var result = await controller.CreateCircuit(new CreateWorkflowCircuitDTO { Nom = "X", Etapes = [] });

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── DeleteCircuit ─────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteCircuit_CircuitNonUtilise_Retourne204()
    {
        // Arrange
        var serviceMock = new Mock<IWorkflowService>();
        serviceMock.Setup(s => s.DeleteCircuitAsync(1)).Returns(Task.CompletedTask);
        var controller = CreateController(serviceMock);

        // Act
        var result = await controller.DeleteCircuit(1);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteCircuit_CircuitUtilise_Retourne409()
    {
        // Arrange
        var serviceMock = new Mock<IWorkflowService>();
        serviceMock.Setup(s => s.DeleteCircuitAsync(1))
            .ThrowsAsync(new InvalidOperationException("Des catégories utilisent ce circuit."));
        var controller = CreateController(serviceMock);

        // Act
        var result = await controller.DeleteCircuit(1);

        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
    }
}
