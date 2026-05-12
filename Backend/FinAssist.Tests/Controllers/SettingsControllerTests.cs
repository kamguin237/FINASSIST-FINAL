using System.Security.Claims;
using FinAssist.API.Controllers;
using FinAssist.Core.DTOs.Settings;
using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FinAssist.Tests.Controllers;

/// <summary>
/// Tests unitaires pour SettingsController — lecture et sauvegarde des préférences utilisateur.
/// </summary>
public class SettingsControllerTests
{
    // ── Setup ─────────────────────────────────────────────────────────────────

    private static SettingsController CreateController(Mock<IUserPreferencesRepository> repoMock, int userId = 1)
    {
        var controller = new SettingsController(repoMock.Object);
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

    private static UserPreferences Prefs(int userId = 1) => new()
    {
        Id = 1,
        UtilisateurId = userId,
        NotifApp = true,
        NotifEmail = false,
        AlertNouveauBesoin = true,
        AlertValidation = true,
        AlertEnAttente = false,
        Langue = "fr",
        FormatDate = "dd/MM/yyyy",
        FuseauHoraire = "Africa/Douala",
        ItemsParPage = 20,
        PageAccueil = "dashboard",
        TriDefaut = "date_desc",
        DateModification = DateTime.UtcNow
    };

    // ── Get ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_PreferencesExistantes_RetourneDTO()
    {
        // Arrange
        var repoMock = new Mock<IUserPreferencesRepository>();
        repoMock.Setup(r => r.GetByUtilisateurIdAsync(1)).ReturnsAsync(Prefs());
        var controller = CreateController(repoMock);

        // Act
        var result = await controller.Get();

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeAssignableTo<UserPreferencesDTO>().Subject;
        dto.NotifApp.Should().BeTrue();
        dto.Langue.Should().Be("fr");
        dto.PageAccueil.Should().Be("dashboard");
    }

    [Fact]
    public async Task Get_AucunePreference_RetourneValeursParDefaut()
    {
        // Arrange
        var repoMock = new Mock<IUserPreferencesRepository>();
        repoMock.Setup(r => r.GetByUtilisateurIdAsync(1)).ReturnsAsync((UserPreferences?)null);
        var controller = CreateController(repoMock);

        // Act
        var result = await controller.Get();

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<UserPreferencesDTO>();
        // Les valeurs par défaut sont retournées (new UserPreferencesDTO())
    }

    // ── Save ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Save_PreferencesValides_SauvegardeEtRetourne200()
    {
        // Arrange
        var repoMock = new Mock<IUserPreferencesRepository>();
        var dto = new UserPreferencesDTO
        {
            NotifApp = true, NotifEmail = true,
            AlertNouveauBesoin = false, AlertValidation = true, AlertEnAttente = true,
            Langue = "en", FormatDate = "MM/dd/yyyy",
            FuseauHoraire = "UTC", ItemsParPage = 50,
            PageAccueil = "besoins", TriDefaut = "titre_asc"
        };
        var saved = Prefs();
        saved.NotifEmail = true;
        saved.Langue = "en";

        repoMock.Setup(r => r.SaveAsync(It.IsAny<UserPreferences>())).ReturnsAsync(saved);
        var controller = CreateController(repoMock);

        // Act
        var result = await controller.Save(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        repoMock.Verify(r => r.SaveAsync(It.Is<UserPreferences>(p =>
            p.UtilisateurId == 1 &&
            p.Langue == "en" &&
            p.ItemsParPage == 50)), Times.Once);
    }

    [Fact]
    public async Task Save_AssocieCorrectementLUtilisateurConnecte()
    {
        // Arrange — userId = 42
        var repoMock = new Mock<IUserPreferencesRepository>();
        UserPreferences? captured = null;
        repoMock.Setup(r => r.SaveAsync(It.IsAny<UserPreferences>()))
            .Callback<UserPreferences>(p => captured = p)
            .ReturnsAsync(Prefs(42));

        var controller = CreateController(repoMock, userId: 42);

        // Act
        await controller.Save(new UserPreferencesDTO { Langue = "fr" });

        // Assert
        captured.Should().NotBeNull();
        captured!.UtilisateurId.Should().Be(42);
    }

    [Fact]
    public async Task Save_DateModificationEstDefinie()
    {
        // Arrange
        var repoMock = new Mock<IUserPreferencesRepository>();
        UserPreferences? captured = null;
        repoMock.Setup(r => r.SaveAsync(It.IsAny<UserPreferences>()))
            .Callback<UserPreferences>(p => captured = p)
            .ReturnsAsync(Prefs());

        var controller = CreateController(repoMock);

        // Act
        await controller.Save(new UserPreferencesDTO());

        // Assert
        captured!.DateModification.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Save_MappeTousLesChamps()
    {
        // Arrange
        var repoMock = new Mock<IUserPreferencesRepository>();
        UserPreferences? captured = null;
        repoMock.Setup(r => r.SaveAsync(It.IsAny<UserPreferences>()))
            .Callback<UserPreferences>(p => captured = p)
            .ReturnsAsync(Prefs());

        var controller = CreateController(repoMock);
        var dto = new UserPreferencesDTO
        {
            NotifApp = true, NotifEmail = false,
            AlertNouveauBesoin = true, AlertValidation = false, AlertEnAttente = true,
            Langue = "en", FormatDate = "yyyy-MM-dd",
            FuseauHoraire = "Europe/Paris", ItemsParPage = 10,
            PageAccueil = "reporting", TriDefaut = "statut_asc"
        };

        // Act
        await controller.Save(dto);

        // Assert
        captured!.NotifApp.Should().BeTrue();
        captured.NotifEmail.Should().BeFalse();
        captured.AlertNouveauBesoin.Should().BeTrue();
        captured.AlertValidation.Should().BeFalse();
        captured.AlertEnAttente.Should().BeTrue();
        captured.Langue.Should().Be("en");
        captured.FormatDate.Should().Be("yyyy-MM-dd");
        captured.FuseauHoraire.Should().Be("Europe/Paris");
        captured.ItemsParPage.Should().Be(10);
        captured.PageAccueil.Should().Be("reporting");
        captured.TriDefaut.Should().Be("statut_asc");
    }
}
