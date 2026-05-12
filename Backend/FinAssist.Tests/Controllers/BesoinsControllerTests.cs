using System.Security.Claims;
using FinAssist.API.Controllers;
using FinAssist.Core.DTOs.Besoins;
using FinAssist.Core.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FinAssist.Tests.Controllers;

/// <summary>
/// Tests unitaires pour BesoinsController — vérification des codes HTTP retournés.
/// </summary>
public class BesoinsControllerTests
{
    // ── Setup ─────────────────────────────────────────────────────────────────

    private static BesoinsController CreateController(Mock<IBesoinsService> serviceMock,
        int userId = 1, string role = "Agent")
    {
        var controller = new BesoinsController(serviceMock.Object);
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

    private static BesoinDTO BesoinDTO(int id = 1) => new()
    {
        Id = id, Titre = "Test", Description = "Desc",
        Statut = "BROUILLON", NiveauImportance = "NORMAL"
    };

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_BesoinExistant_Retourne200()
    {
        // Arrange
        var serviceMock = new Mock<IBesoinsService>();
        serviceMock.Setup(s => s.GetByIdAsync(1, 1, "Agent")).ReturnsAsync(BesoinDTO());
        var controller = CreateController(serviceMock);

        // Act
        var result = await controller.GetById(1);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeAssignableTo<BesoinDTO>()
            .Which.Id.Should().Be(1);
    }

    [Fact]
    public async Task GetById_BesoinInexistant_Retourne404()
    {
        // Arrange
        var serviceMock = new Mock<IBesoinsService>();
        serviceMock.Setup(s => s.GetByIdAsync(99, 1, "Agent"))
            .ThrowsAsync(new KeyNotFoundException("Besoin 99 introuvable."));
        var controller = CreateController(serviceMock);

        // Act
        var result = await controller.GetById(99);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetById_AccesRefuse_Retourne403()
    {
        // Arrange
        var serviceMock = new Mock<IBesoinsService>();
        serviceMock.Setup(s => s.GetByIdAsync(1, 1, "Agent"))
            .ThrowsAsync(new UnauthorizedAccessException());
        var controller = CreateController(serviceMock);

        // Act
        var result = await controller.GetById(1);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_BesoinValide_Retourne200()
    {
        // Arrange
        var serviceMock = new Mock<IBesoinsService>();
        var dto = new UpdateBesoinDTO { Titre = "Nouveau titre" };
        serviceMock.Setup(s => s.UpdateAsync(1, dto, 1)).ReturnsAsync(BesoinDTO());
        var controller = CreateController(serviceMock);

        // Act
        var result = await controller.Update(1, dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Update_BesoinNonBrouillon_Retourne409()
    {
        // Arrange
        var serviceMock = new Mock<IBesoinsService>();
        serviceMock.Setup(s => s.UpdateAsync(1, It.IsAny<UpdateBesoinDTO>(), 1))
            .ThrowsAsync(new InvalidOperationException("Seuls les besoins BROUILLON peuvent être modifiés."));
        var controller = CreateController(serviceMock);

        // Act
        var result = await controller.Update(1, new UpdateBesoinDTO());

        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
    }

    // ── Enregistrer ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Enregistrer_BesoinBrouillon_Retourne200()
    {
        // Arrange
        var serviceMock = new Mock<IBesoinsService>();
        serviceMock.Setup(s => s.EnregistrerAsync(1, 1)).ReturnsAsync(BesoinDTO());
        var controller = CreateController(serviceMock);

        // Act
        var result = await controller.Enregistrer(1);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Enregistrer_BesoinInexistant_Retourne404()
    {
        // Arrange
        var serviceMock = new Mock<IBesoinsService>();
        serviceMock.Setup(s => s.EnregistrerAsync(99, 1))
            .ThrowsAsync(new KeyNotFoundException("Besoin 99 introuvable."));
        var controller = CreateController(serviceMock);

        // Act
        var result = await controller.Enregistrer(99);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ── Soumettre ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Soumettre_BesoinEnregistre_Retourne200()
    {
        // Arrange
        var serviceMock = new Mock<IBesoinsService>();
        serviceMock.Setup(s => s.SoumettreAsync(1, 1)).ReturnsAsync(BesoinDTO());
        var controller = CreateController(serviceMock);

        // Act
        var result = await controller.Soumettre(1);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Soumettre_BesoinNonEnregistre_Retourne409()
    {
        // Arrange
        var serviceMock = new Mock<IBesoinsService>();
        serviceMock.Setup(s => s.SoumettreAsync(1, 1))
            .ThrowsAsync(new InvalidOperationException("Le besoin doit être enregistré avant d'être soumis."));
        var controller = CreateController(serviceMock);

        // Act
        var result = await controller.Soumettre(1);

        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_BesoinBrouillon_Retourne204()
    {
        // Arrange
        var serviceMock = new Mock<IBesoinsService>();
        serviceMock.Setup(s => s.DeleteAsync(1)).Returns(Task.CompletedTask);
        var controller = CreateController(serviceMock);

        // Act
        var result = await controller.Delete(1);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_BesoinEnValidation_Retourne409()
    {
        // Arrange
        var serviceMock = new Mock<IBesoinsService>();
        serviceMock.Setup(s => s.DeleteAsync(1))
            .ThrowsAsync(new InvalidOperationException("Impossible de supprimer un besoin en cours de validation."));
        var controller = CreateController(serviceMock);

        // Act
        var result = await controller.Delete(1);

        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
    }

    // ── GetHistorique ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetHistorique_BesoinExistant_Retourne200AvecListe()
    {
        // Arrange
        var serviceMock = new Mock<IBesoinsService>();
        var historique = new List<HistoriqueDTO>
        {
            new() { Id = 1, Action = "CREATION", Description = "Créé", DateAction = DateTime.UtcNow },
            new() { Id = 2, Action = "SOUMISSION", Description = "Soumis", DateAction = DateTime.UtcNow }
        };
        serviceMock.Setup(s => s.GetHistoriqueAsync(1)).ReturnsAsync(historique);
        var controller = CreateController(serviceMock);

        // Act
        var result = await controller.GetHistorique(1);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IEnumerable<HistoriqueDTO>>()
            .Which.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetHistorique_BesoinInexistant_Retourne404()
    {
        // Arrange
        var serviceMock = new Mock<IBesoinsService>();
        serviceMock.Setup(s => s.GetHistoriqueAsync(99))
            .ThrowsAsync(new KeyNotFoundException("Besoin 99 introuvable."));
        var controller = CreateController(serviceMock);

        // Act
        var result = await controller.GetHistorique(99);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_RetourneListeBesoins()
    {
        // Arrange
        var serviceMock = new Mock<IBesoinsService>();
        var besoins = new List<BesoinDTO> { BesoinDTO(1), BesoinDTO(2) };
        serviceMock.Setup(s => s.GetAllAsync(1, "Agent")).ReturnsAsync(besoins);
        var controller = CreateController(serviceMock);

        // Act
        var result = await controller.GetAll();

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IEnumerable<BesoinDTO>>()
            .Which.Should().HaveCount(2);
    }
}
