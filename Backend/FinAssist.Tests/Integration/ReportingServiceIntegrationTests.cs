using FinAssist.Application.Services;
using FinAssist.Core.DTOs.Reporting;
using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;
using FinAssist.Infrastructure.Data;
using FinAssist.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FinAssist.Tests.Integration;

/// <summary>
/// Tests d'intégration pour ReportingService + ReportingRepository + BesoinsRepository + AppDbContext (InMemory).
/// Couvre GetStatistiquesAsync, GetRapportBesoinsAsync, GetDashboardAsync (admin + utilisateur normal).
/// </summary>
public class ReportingServiceIntegrationTests
{
    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    private static ReportingService CreateService(AppDbContext db)
    {
        var reportingRepo = new ReportingRepository(db);
        var besoinsRepo = new BesoinsRepository(db);
        var exportMock = new Mock<IExportService>();
        exportMock.Setup(e => e.ExporterExcel(It.IsAny<IEnumerable<Besoin>>())).Returns([0x50, 0x4B]);
        exportMock.Setup(e => e.ExporterPdf(It.IsAny<IEnumerable<Besoin>>())).Returns([0x25, 0x50]);
        return new ReportingService(reportingRepo, besoinsRepo, exportMock.Object);
    }

    private static async Task<(Role agentRole, Role respRole, Utilisateur agent, Utilisateur resp, Categorie cat)>
        SeedBaseAsync(AppDbContext db)
    {
        var agentRole = new Role { Code = "Agent", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow };
        var respRole = new Role { Code = "Responsable", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow };
        db.Roles.AddRange(agentRole, respRole);

        var agent = new Utilisateur
        {
            Nom = "Dupont", Prenom = "Jean", Email = "jean@finstar-cm.com",
            MotDePasse = "hash", RoleId = agentRole.Id, Actif = true,
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow
        };
        var resp = new Utilisateur
        {
            Nom = "Martin", Prenom = "Paul", Email = "paul@finstar-cm.com",
            MotDePasse = "hash", RoleId = respRole.Id, Actif = true,
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow
        };
        db.Utilisateurs.AddRange(agent, resp);

        var circuit = new WorkflowCircuit
        {
            Nom = "Circuit Test", NomCreateur = "Admin",
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow,
            Etapes = [new EtapeCircuit { Ordre = 1, RoleRequis = "Responsable", DelaiMaxJours = 60, EstDerniereEtape = true }]
        };
        db.WorkflowCircuits.Add(circuit);

        var cat = new Categorie { Nom = "Informatique", WorkflowCircuitId = circuit.Id, DateCreation = DateTime.UtcNow };
        db.Categories.Add(cat);
        await db.SaveChangesAsync();

        return (agentRole, respRole, agent, resp, cat);
    }

    private static Besoin MakeBesoin(int userId, int catId, string statut, string titre = "Besoin") => new()
    {
        Titre = titre, Description = "Desc", Statut = statut,
        NiveauImportance = "MOYEN", UtilisateurId = userId, CategorieId = catId,
        DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow
    };

    // ── GetStatistiquesAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetStatistiquesAsync_BaseVide_RetourneZeros()
    {
        // Arrange
        using var db = CreateDb(nameof(GetStatistiquesAsync_BaseVide_RetourneZeros));
        var service = CreateService(db);

        // Act
        var stats = await service.GetStatistiquesAsync();

        // Assert
        stats.TotalBesoins.Should().Be(0);
        stats.TotalUtilisateurs.Should().Be(0);
        stats.TotalActifs.Should().Be(0);
        stats.SignaturesApposees.Should().Be(0);
    }

    [Fact]
    public async Task GetStatistiquesAsync_AvecDonnees_RetourneComptesCorrects()
    {
        // Arrange
        using var db = CreateDb(nameof(GetStatistiquesAsync_AvecDonnees_RetourneComptesCorrects));
        var (_, _, agent, resp, cat) = await SeedBaseAsync(db);
        var service = CreateService(db);

        db.Besoins.AddRange(
            MakeBesoin(agent.Id, cat.Id, "BROUILLON"),
            MakeBesoin(agent.Id, cat.Id, "EN_ATTENTE_RESPONSABLE"),
            MakeBesoin(agent.Id, cat.Id, "TERMINE")
        );
        await db.SaveChangesAsync();

        // Act
        var stats = await service.GetStatistiquesAsync();

        // Assert
        stats.TotalBesoins.Should().Be(3);
        stats.TotalUtilisateurs.Should().Be(2);
        stats.TotalActifs.Should().Be(2);
        stats.BesoinsByStatut.Should().ContainKey("BROUILLON");
        stats.BesoinsByStatut["BROUILLON"].Should().Be(1);
        stats.BesoinsByCategorie.Should().ContainKey("Informatique");
        stats.BesoinsByCategorie["Informatique"].Should().Be(3);
    }

    [Fact]
    public async Task GetStatistiquesAsync_UtilisateursActifsEtInactifs_CompteCorrectement()
    {
        // Arrange
        using var db = CreateDb(nameof(GetStatistiquesAsync_UtilisateursActifsEtInactifs_CompteCorrectement));
        var role = new Role { Code = "Agent", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow };
        db.Roles.Add(role);
        db.Utilisateurs.AddRange(
            new Utilisateur { Nom = "A", Prenom = "A", Email = "a@finstar-cm.com", MotDePasse = "h", RoleId = role.Id, Actif = true, DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow },
            new Utilisateur { Nom = "B", Prenom = "B", Email = "b@finstar-cm.com", MotDePasse = "h", RoleId = role.Id, Actif = true, DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow },
            new Utilisateur { Nom = "C", Prenom = "C", Email = "c@finstar-cm.com", MotDePasse = "h", RoleId = role.Id, Actif = false, DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();
        var service = CreateService(db);

        // Act
        var stats = await service.GetStatistiquesAsync();

        // Assert
        stats.TotalUtilisateurs.Should().Be(3);
        stats.TotalActifs.Should().Be(2);
    }

    // ── GetRapportBesoinsAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetRapportBesoinsAsync_SansFiltres_RetourneTousLesBesoins()
    {
        // Arrange
        using var db = CreateDb(nameof(GetRapportBesoinsAsync_SansFiltres_RetourneTousLesBesoins));
        var (_, _, agent, _, cat) = await SeedBaseAsync(db);
        var service = CreateService(db);

        db.Besoins.AddRange(
            MakeBesoin(agent.Id, cat.Id, "BROUILLON", "Besoin 1"),
            MakeBesoin(agent.Id, cat.Id, "TERMINE", "Besoin 2")
        );
        await db.SaveChangesAsync();

        // Act
        var rapport = await service.GetRapportBesoinsAsync(null, generateurId: agent.Id);

        // Assert
        rapport.Should().NotBeNull();
        rapport.Type.Should().Be("RAPPORT_BESOINS");
        rapport.GenerateurId.Should().Be(agent.Id);
        rapport.Contenu.Should().NotBeNull();
        var contenu = ((System.Collections.IEnumerable)rapport.Contenu).Cast<object>().ToList();
        contenu.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetRapportBesoinsAsync_AvecFiltreStatut_RetourneSeulementCeStatut()
    {
        // Arrange
        using var db = CreateDb(nameof(GetRapportBesoinsAsync_AvecFiltreStatut_RetourneSeulementCeStatut));
        var (_, _, agent, _, cat) = await SeedBaseAsync(db);
        var service = CreateService(db);

        db.Besoins.AddRange(
            MakeBesoin(agent.Id, cat.Id, "BROUILLON"),
            MakeBesoin(agent.Id, cat.Id, "BROUILLON"),
            MakeBesoin(agent.Id, cat.Id, "TERMINE")
        );
        await db.SaveChangesAsync();

        // Act
        var rapport = await service.GetRapportBesoinsAsync(
            new FiltreRapportDTO { Statut = "BROUILLON" }, generateurId: agent.Id);

        // Assert
        var contenu = ((System.Collections.IEnumerable)rapport.Contenu).Cast<object>().ToList();
        contenu.Should().HaveCount(2);
        rapport.Parametres.Should().ContainKey("statut");
        rapport.Parametres["statut"].Should().Be("BROUILLON");
    }

    [Fact]
    public async Task GetRapportBesoinsAsync_AvecFiltreDates_IncludesParametres()
    {
        // Arrange
        using var db = CreateDb(nameof(GetRapportBesoinsAsync_AvecFiltreDates_IncludesParametres));
        var (_, _, agent, _, cat) = await SeedBaseAsync(db);
        var service = CreateService(db);

        db.Besoins.Add(MakeBesoin(agent.Id, cat.Id, "BROUILLON"));
        await db.SaveChangesAsync();

        var debut = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var fin = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var rapport = await service.GetRapportBesoinsAsync(
            new FiltreRapportDTO { DateDebut = debut, DateFin = fin }, generateurId: agent.Id);

        // Assert
        rapport.Parametres.Should().ContainKey("dateDebut");
        rapport.Parametres.Should().ContainKey("dateFin");
    }

    // ── GetDashboardAsync — Administrateur ────────────────────────────────────

    [Fact]
    public async Task GetDashboardAsync_Administrateur_VoitTousLesBesoins()
    {
        // Arrange
        using var db = CreateDb(nameof(GetDashboardAsync_Administrateur_VoitTousLesBesoins));
        var (_, _, agent, resp, cat) = await SeedBaseAsync(db);
        var service = CreateService(db);

        db.Besoins.AddRange(
            MakeBesoin(agent.Id, cat.Id, "BROUILLON"),
            MakeBesoin(agent.Id, cat.Id, "EN_ATTENTE_RESPONSABLE"),
            MakeBesoin(agent.Id, cat.Id, "APPROUVE_PAR_RESPONSABLE"),
            MakeBesoin(agent.Id, cat.Id, "REJETE_PAR_RESPONSABLE"),
            MakeBesoin(agent.Id, cat.Id, "TERMINE")
        );
        await db.SaveChangesAsync();

        // Act
        var dashboard = await service.GetDashboardAsync(resp.Id, "Administrateur");

        // Assert
        dashboard.Should().NotBeNull();
        dashboard.BesoinsEnAttente.Should().Be(1);   // EN_ATTENTE_RESPONSABLE
        dashboard.BesoinsApprouves.Should().BeGreaterThan(0);
        dashboard.BesoinsRejetes.Should().Be(1);
        dashboard.BesoinsBrouillons.Should().Be(1);
    }

    // ── GetDashboardAsync — Utilisateur normal ────────────────────────────────

    [Fact]
    public async Task GetDashboardAsync_UtilisateurNormal_VoitSesSoumissions()
    {
        // Arrange
        using var db = CreateDb(nameof(GetDashboardAsync_UtilisateurNormal_VoitSesSoumissions));
        var (_, _, agent, _, cat) = await SeedBaseAsync(db);
        var service = CreateService(db);

        // Agent a créé 2 besoins soumis + 1 brouillon
        db.Besoins.AddRange(
            MakeBesoin(agent.Id, cat.Id, "EN_ATTENTE_RESPONSABLE", "Soumis 1"),
            MakeBesoin(agent.Id, cat.Id, "EN_ATTENTE_RESPONSABLE", "Soumis 2"),
            MakeBesoin(agent.Id, cat.Id, "BROUILLON", "Brouillon")
        );
        await db.SaveChangesAsync();

        // Act
        var dashboard = await service.GetDashboardAsync(agent.Id, "Agent");

        // Assert
        dashboard.Should().NotBeNull();
        dashboard.BesoinsSoumisParMoi.Should().Be(2); // les 2 EN_ATTENTE
        dashboard.BesoinsBrouillons.Should().Be(1);
    }

    [Fact]
    public async Task GetDashboardAsync_DerniersBesoins_LimiteA5()
    {
        // Arrange
        using var db = CreateDb(nameof(GetDashboardAsync_DerniersBesoins_LimiteA5));
        var (_, _, agent, _, cat) = await SeedBaseAsync(db);
        var service = CreateService(db);

        // Créer 8 besoins
        for (int i = 1; i <= 8; i++)
            db.Besoins.Add(MakeBesoin(agent.Id, cat.Id, "BROUILLON", $"Besoin {i}"));
        await db.SaveChangesAsync();

        // Act
        var dashboard = await service.GetDashboardAsync(agent.Id, "Administrateur");

        // Assert — max 5 derniers besoins
        dashboard.DerniersBesoins.Should().HaveCount(5);
    }

    // ── ExporterAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ExporterAsync_FormatExcel_RetourneContenuExcel()
    {
        // Arrange
        using var db = CreateDb(nameof(ExporterAsync_FormatExcel_RetourneContenuExcel));
        var (_, _, agent, _, cat) = await SeedBaseAsync(db);
        var service = CreateService(db);

        db.Besoins.Add(MakeBesoin(agent.Id, cat.Id, "BROUILLON"));
        await db.SaveChangesAsync();

        // Act
        var (contenu, contentType, nomFichier) = await service.ExporterAsync(
            new ExportRequestDTO { Format = "EXCEL" }, generateurId: agent.Id);

        // Assert
        contenu.Should().NotBeEmpty();
        contentType.Should().Contain("spreadsheetml");
        nomFichier.Should().EndWith(".xlsx");
    }

    [Fact]
    public async Task ExporterAsync_FormatPdf_RetourneContenuPdf()
    {
        // Arrange
        using var db = CreateDb(nameof(ExporterAsync_FormatPdf_RetourneContenuPdf));
        var (_, _, agent, _, cat) = await SeedBaseAsync(db);
        var service = CreateService(db);

        db.Besoins.Add(MakeBesoin(agent.Id, cat.Id, "BROUILLON"));
        await db.SaveChangesAsync();

        // Act
        var (contenu, contentType, nomFichier) = await service.ExporterAsync(
            new ExportRequestDTO { Format = "PDF" }, generateurId: agent.Id);

        // Assert
        contenu.Should().NotBeEmpty();
        contentType.Should().Be("application/pdf");
        nomFichier.Should().EndWith(".pdf");
    }
}
