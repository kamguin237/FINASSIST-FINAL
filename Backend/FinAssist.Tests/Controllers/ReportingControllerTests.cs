using System.Security.Claims;
using FinAssist.API.Controllers;
using FinAssist.Core.DTOs.Reporting;
using FinAssist.Core.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FinAssist.Tests.Controllers;

/// <summary>
/// Tests unitaires pour ReportingController — statistiques, dashboard et export.
/// </summary>
public class ReportingControllerTests
{
    // ── Setup ─────────────────────────────────────────────────────────────────

    private static ReportingController CreateController(Mock<IReportingService> serviceMock,
        int userId = 1, string role = "Administrateur")
    {
        var controller = new ReportingController(serviceMock.Object);
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

    // ── GetStatistiques ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetStatistiques_Retourne200AvecStats()
    {
        // Arrange
        var serviceMock = new Mock<IReportingService>();
        serviceMock.Setup(s => s.GetStatistiquesAsync()).ReturnsAsync(new StatistiquesDTO
        {
            TotalBesoins = 10, TotalUtilisateurs = 5, SignaturesApposees = 3
        });
        var controller = CreateController(serviceMock);

        // Act
        var result = await controller.GetStatistiques();

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<StatistiquesDTO>()
            .Which.TotalBesoins.Should().Be(10);
    }

    // ── GetDashboard ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetDashboard_Retourne200AvecDashboard()
    {
        // Arrange
        var serviceMock = new Mock<IReportingService>();
        serviceMock.Setup(s => s.GetDashboardAsync(1, "Administrateur"))
            .ReturnsAsync(new DashboardDTO { BesoinsEnAttente = 3, BesoinsApprouves = 7 });
        var controller = CreateController(serviceMock);

        // Act
        var result = await controller.GetDashboard();

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<DashboardDTO>()
            .Which.BesoinsEnAttente.Should().Be(3);
    }

    [Fact]
    public async Task GetDashboard_PasseRoleCorrect()
    {
        // Arrange
        var serviceMock = new Mock<IReportingService>();
        serviceMock.Setup(s => s.GetDashboardAsync(1, "Responsable"))
            .ReturnsAsync(new DashboardDTO());
        var controller = CreateController(serviceMock, userId: 1, role: "Responsable");

        // Act
        await controller.GetDashboard();

        // Assert
        serviceMock.Verify(s => s.GetDashboardAsync(1, "Responsable"), Times.Once);
    }

    // ── GetEvolution ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetEvolution_PeriodeJours_Retourne200()
    {
        // Arrange
        var serviceMock = new Mock<IReportingService>();
        serviceMock.Setup(s => s.GetEvolutionBesoinsAsync("jours", 1, "Administrateur"))
            .ReturnsAsync(new List<EvolutionPointDTO>
            {
                new() { Date = "2026-04-01", Recus = 2, Approuves = 1 }
            });
        var controller = CreateController(serviceMock);

        // Act
        var result = await controller.GetEvolution("jours");

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IEnumerable<EvolutionPointDTO>>()
            .Which.Should().HaveCount(1);
    }

    // ── GetRapportBesoins ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetRapportBesoins_SansFiltres_Retourne200()
    {
        // Arrange
        var serviceMock = new Mock<IReportingService>();
        serviceMock.Setup(s => s.GetRapportBesoinsAsync(null, 1))
            .ReturnsAsync(new RapportDTO { Type = "RAPPORT_BESOINS", GenerateurId = 1 });
        var controller = CreateController(serviceMock);

        // Act
        var result = await controller.GetRapportBesoins(null);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    // ── Exporter ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Exporter_FormatPDF_RetourneFileResult()
    {
        // Arrange
        var serviceMock = new Mock<IReportingService>();
        serviceMock.Setup(s => s.ExporterAsync(It.IsAny<ExportRequestDTO>(), 1))
            .ReturnsAsync((new byte[] { 0x25, 0x50, 0x44, 0x46 }, "application/pdf", "rapport.pdf"));
        var controller = CreateController(serviceMock);

        // Act
        var result = await controller.Exporter(new ExportRequestDTO { Format = "PDF" });

        // Assert
        var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
        fileResult.ContentType.Should().Be("application/pdf");
        fileResult.FileDownloadName.Should().Be("rapport.pdf");
    }
}
