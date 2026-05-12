using FinAssist.Application.Services;
using FinAssist.Core.DTOs.Reporting;
using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;
using FluentAssertions;
using Moq;

namespace FinAssist.Tests.Services;

/// <summary>
/// Tests unitaires pour ReportingService — statistiques et dashboard.
/// </summary>
public class ReportingServiceTests
{
    // ── Setup ─────────────────────────────────────────────────────────────────

    private readonly Mock<IReportingRepository> _reportingRepoMock = new();
    private readonly Mock<IBesoinsRepository>   _besoinsRepoMock   = new();
    private readonly Mock<IExportService>       _exportMock        = new();

    private ReportingService CreateService() =>
        new(_reportingRepoMock.Object, _besoinsRepoMock.Object, _exportMock.Object);

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Besoin Besoin(int id, string statut, int userId = 10, string categorie = "Informatique") => new()
    {
        Id = id, Titre = $"Besoin {id}", Description = "Desc",
        Statut = statut, UtilisateurId = userId, CategorieId = 1,
        DateCreation = DateTime.UtcNow.AddDays(-id),
        DateModification = DateTime.UtcNow.AddDays(-id),
        Categorie = new Categorie { Id = 1, Nom = categorie }
    };

    private static Utilisateur User(int id, bool actif = true) => new()
    {
        Id = id, Nom = "Dupont", Prenom = "Jean",
        Email = $"user{id}@test.com", Actif = actif
    };

    // ── GetStatistiquesAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetStatistiquesAsync_RetourneCompteursCorrcts()
    {
        // Arrange
        var besoins = new List<Besoin>
        {
            Besoin(1, "BROUILLON"),
            Besoin(2, "EN_ATTENTE_RESPONSABLE"),
            Besoin(3, "TERMINE"),
            Besoin(4, "REJETE_PAR_RESPONSABLE"),
            Besoin(5, "EN_ATTENTE_DIRECTION", categorie: "RH")
        };
        var utilisateurs = new List<Utilisateur>
        {
            User(1), User(2), User(3, actif: false)
        };

        _reportingRepoMock.Setup(r => r.GetAllBesoinsAsync()).ReturnsAsync(besoins);
        _reportingRepoMock.Setup(r => r.GetAllUtilisateursAsync()).ReturnsAsync(utilisateurs);
        _reportingRepoMock.Setup(r => r.CountSignaturesAsync()).ReturnsAsync(7);
        _reportingRepoMock.Setup(r => r.CountNotificationsAsync()).ReturnsAsync(15);

        var service = CreateService();

        // Act
        var result = await service.GetStatistiquesAsync();

        // Assert
        result.TotalBesoins.Should().Be(5);
        result.TotalUtilisateurs.Should().Be(3);
        result.TotalActifs.Should().Be(2);
        result.SignaturesApposees.Should().Be(7);
        result.NotificationsEnvoyees.Should().Be(15);
    }

    [Fact]
    public async Task GetStatistiquesAsync_GroupeParStatut()
    {
        // Arrange
        var besoins = new List<Besoin>
        {
            Besoin(1, "BROUILLON"),
            Besoin(2, "BROUILLON"),
            Besoin(3, "EN_ATTENTE_RESPONSABLE"),
            Besoin(4, "TERMINE")
        };

        _reportingRepoMock.Setup(r => r.GetAllBesoinsAsync()).ReturnsAsync(besoins);
        _reportingRepoMock.Setup(r => r.GetAllUtilisateursAsync()).ReturnsAsync(new List<Utilisateur>());
        _reportingRepoMock.Setup(r => r.CountSignaturesAsync()).ReturnsAsync(0);
        _reportingRepoMock.Setup(r => r.CountNotificationsAsync()).ReturnsAsync(0);

        var service = CreateService();

        // Act
        var result = await service.GetStatistiquesAsync();

        // Assert
        result.BesoinsByStatut.Should().ContainKey("BROUILLON").WhoseValue.Should().Be(2);
        result.BesoinsByStatut.Should().ContainKey("EN_ATTENTE_RESPONSABLE").WhoseValue.Should().Be(1);
        result.BesoinsByStatut.Should().ContainKey("TERMINE").WhoseValue.Should().Be(1);
    }

    [Fact]
    public async Task GetStatistiquesAsync_GroupeParCategorie()
    {
        // Arrange
        var besoins = new List<Besoin>
        {
            Besoin(1, "BROUILLON", categorie: "Informatique"),
            Besoin(2, "BROUILLON", categorie: "Informatique"),
            Besoin(3, "BROUILLON", categorie: "RH")
        };

        _reportingRepoMock.Setup(r => r.GetAllBesoinsAsync()).ReturnsAsync(besoins);
        _reportingRepoMock.Setup(r => r.GetAllUtilisateursAsync()).ReturnsAsync(new List<Utilisateur>());
        _reportingRepoMock.Setup(r => r.CountSignaturesAsync()).ReturnsAsync(0);
        _reportingRepoMock.Setup(r => r.CountNotificationsAsync()).ReturnsAsync(0);

        var service = CreateService();

        // Act
        var result = await service.GetStatistiquesAsync();

        // Assert
        result.BesoinsByCategorie["Informatique"].Should().Be(2);
        result.BesoinsByCategorie["RH"].Should().Be(1);
    }

    // ── GetDashboardAsync — Administrateur ────────────────────────────────────

    [Fact]
    public async Task GetDashboardAsync_Administrateur_VoitTousLesBesoins()
    {
        // Arrange
        var besoins = new List<Besoin>
        {
            Besoin(1, "EN_ATTENTE_RESPONSABLE", userId: 5),
            Besoin(2, "TERMINE", userId: 6),
            Besoin(3, "REJETE_PAR_DIRECTION", userId: 7),
            Besoin(4, "BROUILLON", userId: 8),
            Besoin(5, "ENREGISTRE", userId: 9)
        };

        _reportingRepoMock.Setup(r => r.GetAllBesoinsAsync()).ReturnsAsync(besoins);
        _reportingRepoMock.Setup(r => r.CountNotificationsNonLuesAsync(1)).ReturnsAsync(3);
        _besoinsRepoMock.Setup(r => r.GetValidationsParUtilisateurAsync(1)).ReturnsAsync(new List<Validation>());
        _besoinsRepoMock.Setup(r => r.GetCodesPermissionsUtilisateurAsync(1)).ReturnsAsync(new List<string>());

        var service = CreateService();

        // Act
        var result = await service.GetDashboardAsync(utilisateurId: 1, roleCode: "Administrateur");

        // Assert
        result.BesoinsEnAttente.Should().Be(1);  // EN_ATTENTE_RESPONSABLE
        result.BesoinsRejetes.Should().Be(1);    // REJETE_PAR_DIRECTION
        result.BesoinsBrouillons.Should().Be(1); // BROUILLON
        result.BesoinsEnregistres.Should().Be(1); // ENREGISTRE
        result.NotificationsNonLues.Should().Be(3);
    }

    [Fact]
    public async Task GetDashboardAsync_Administrateur_DerniersBesoinsLimites5()
    {
        // Arrange — 8 besoins, on doit n'en avoir que 5 dans DerniersBesoins
        var besoins = Enumerable.Range(1, 8)
            .Select(i => Besoin(i, "BROUILLON", userId: i + 10))
            .ToList();

        _reportingRepoMock.Setup(r => r.GetAllBesoinsAsync()).ReturnsAsync(besoins);
        _reportingRepoMock.Setup(r => r.CountNotificationsNonLuesAsync(1)).ReturnsAsync(0);
        _besoinsRepoMock.Setup(r => r.GetValidationsParUtilisateurAsync(1)).ReturnsAsync(new List<Validation>());
        _besoinsRepoMock.Setup(r => r.GetCodesPermissionsUtilisateurAsync(1)).ReturnsAsync(new List<string>());

        var service = CreateService();

        // Act
        var result = await service.GetDashboardAsync(1, "Administrateur");

        // Assert
        result.DerniersBesoins.Should().HaveCount(5);
    }

    // ── GetRapportBesoinsAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetRapportBesoinsAsync_SansFiltres_RetourneTousLesBesoins()
    {
        // Arrange
        var besoins = new List<Besoin>
        {
            Besoin(1, "BROUILLON"), Besoin(2, "TERMINE")
        };
        _reportingRepoMock.Setup(r => r.GetBesoinsFiltrésAsync(null)).ReturnsAsync(besoins);

        var service = CreateService();

        // Act
        var result = await service.GetRapportBesoinsAsync(null, generateurId: 1);

        // Assert
        result.Type.Should().Be("RAPPORT_BESOINS");
        result.GenerateurId.Should().Be(1);
        result.Parametres.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRapportBesoinsAsync_AvecFiltres_IncludesParametres()
    {
        // Arrange
        var filtres = new FiltreRapportDTO
        {
            DateDebut = new DateTime(2026, 1, 1),
            DateFin = new DateTime(2026, 12, 31),
            Statut = "TERMINE"
        };
        _reportingRepoMock.Setup(r => r.GetBesoinsFiltrésAsync(filtres)).ReturnsAsync(new List<Besoin>());

        var service = CreateService();

        // Act
        var result = await service.GetRapportBesoinsAsync(filtres, generateurId: 5);

        // Assert
        result.Parametres.Should().ContainKey("dateDebut");
        result.Parametres.Should().ContainKey("dateFin");
        result.Parametres.Should().ContainKey("statut").WhoseValue.Should().Be("TERMINE");
    }

    // ── ExporterAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ExporterAsync_FormatPDF_RetournePdf()
    {
        // Arrange
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF
        var request = new ExportRequestDTO { Format = "PDF", Filtres = null };

        _reportingRepoMock.Setup(r => r.GetBesoinsFiltrésAsync(null)).ReturnsAsync(new List<Besoin>());
        _exportMock.Setup(e => e.ExporterPdf(It.IsAny<IEnumerable<Besoin>>())).Returns(pdfBytes);

        var service = CreateService();

        // Act
        var (contenu, contentType, nomFichier) = await service.ExporterAsync(request, generateurId: 1);

        // Assert
        contentType.Should().Be("application/pdf");
        nomFichier.Should().StartWith("rapport_besoins_").And.EndWith(".pdf");
        contenu.Should().BeEquivalentTo(pdfBytes);
    }

    [Fact]
    public async Task ExporterAsync_FormatEXCEL_RetourneExcel()
    {
        // Arrange
        var excelBytes = new byte[] { 0x50, 0x4B }; // PK (ZIP header)
        var request = new ExportRequestDTO { Format = "EXCEL", Filtres = null };

        _reportingRepoMock.Setup(r => r.GetBesoinsFiltrésAsync(null)).ReturnsAsync(new List<Besoin>());
        _exportMock.Setup(e => e.ExporterExcel(It.IsAny<IEnumerable<Besoin>>())).Returns(excelBytes);

        var service = CreateService();

        // Act
        var (contenu, contentType, nomFichier) = await service.ExporterAsync(request, generateurId: 1);

        // Assert
        contentType.Should().Contain("spreadsheetml");
        nomFichier.Should().EndWith(".xlsx");
        contenu.Should().BeEquivalentTo(excelBytes);
    }
}
