using FinAssist.Core.DTOs.Logs;
using FinAssist.Core.Entities;
using FinAssist.Infrastructure.Data;
using FinAssist.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FinAssist.Tests.Integration;

/// <summary>
/// Tests d'intégration pour LogRepository + AppDbContext (InMemory).
/// Teste directement le repository (filtres, pagination) sans passer par LogService.
/// </summary>
public class LogRepositoryIntegrationTests
{
    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    private static LogRepository CreateRepo(AppDbContext db) => new(db);

    private static async Task<Utilisateur> SeedUserAsync(AppDbContext db, string email = "jean@finstar-cm.com")
    {
        var role = new Role { Code = "Agent", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow };
        db.Roles.Add(role);
        var user = new Utilisateur
        {
            Nom = "Dupont", Prenom = "Jean", Email = email,
            MotDePasse = "hash", RoleId = role.Id, Actif = true,
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow
        };
        db.Utilisateurs.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static LogActivite MakeLog(string action, string entiteType, int? userId = null,
        DateTime? date = null) => new()
    {
        Action = action,
        EntiteType = entiteType,
        EntiteId = 1,
        UtilisateurId = userId,
        Date = date ?? DateTime.UtcNow
    };

    // ── AjouterAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task AjouterAsync_LogValide_PersistEnBase()
    {
        // Arrange
        using var db = CreateDb(nameof(AjouterAsync_LogValide_PersistEnBase));
        var repo = CreateRepo(db);

        var log = MakeLog("CREATION", "Besoin");

        // Act
        await repo.AjouterAsync(log);

        // Assert
        var logs = await db.LogsActivites.ToListAsync();
        logs.Should().HaveCount(1);
        logs[0].Action.Should().Be("CREATION");
        logs[0].EntiteType.Should().Be("Besoin");
    }

    // ── GetPagedAsync — pagination ────────────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_Page1_RetournePremierePage()
    {
        // Arrange
        using var db = CreateDb(nameof(GetPagedAsync_Page1_RetournePremierePage));
        var repo = CreateRepo(db);

        for (int i = 1; i <= 7; i++)
            await repo.AjouterAsync(MakeLog($"ACTION_{i}", "Besoin"));

        // Act
        var (items, total) = await repo.GetPagedAsync(new FiltreLogsDTO { Page = 1, PageSize = 5 });

        // Assert
        total.Should().Be(7);
        items.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetPagedAsync_Page2_RetourneElementsRestants()
    {
        // Arrange
        using var db = CreateDb(nameof(GetPagedAsync_Page2_RetourneElementsRestants));
        var repo = CreateRepo(db);

        for (int i = 1; i <= 7; i++)
            await repo.AjouterAsync(MakeLog($"ACTION_{i}", "Besoin"));

        // Act
        var (items, total) = await repo.GetPagedAsync(new FiltreLogsDTO { Page = 2, PageSize = 5 });

        // Assert
        total.Should().Be(7);
        items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPagedAsync_BaseVide_RetourneListeVide()
    {
        // Arrange
        using var db = CreateDb(nameof(GetPagedAsync_BaseVide_RetourneListeVide));
        var repo = CreateRepo(db);

        // Act
        var (items, total) = await repo.GetPagedAsync(new FiltreLogsDTO { Page = 1, PageSize = 10 });

        // Assert
        total.Should().Be(0);
        items.Should().BeEmpty();
    }

    // ── GetPagedAsync — filtre par Action ─────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_FiltreParAction_RetourneSeulementCetteAction()
    {
        // Arrange
        using var db = CreateDb(nameof(GetPagedAsync_FiltreParAction_RetourneSeulementCetteAction));
        var repo = CreateRepo(db);

        await repo.AjouterAsync(MakeLog("CREATION", "Besoin"));
        await repo.AjouterAsync(MakeLog("CREATION", "Utilisateur"));
        await repo.AjouterAsync(MakeLog("MODIFICATION", "Besoin"));
        await repo.AjouterAsync(MakeLog("SUPPRESSION", "Role"));

        // Act
        var (items, total) = await repo.GetPagedAsync(new FiltreLogsDTO
        {
            Page = 1, PageSize = 10,
            Action = "CREATION"
        });

        // Assert
        total.Should().Be(2);
        items.All(l => l.Action.Contains("CREATION")).Should().BeTrue();
    }

    // ── GetPagedAsync — filtre par EntiteType ─────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_FiltreParEntiteType_RetourneSeulementCetteEntite()
    {
        // Arrange
        using var db = CreateDb(nameof(GetPagedAsync_FiltreParEntiteType_RetourneSeulementCetteEntite));
        var repo = CreateRepo(db);

        await repo.AjouterAsync(MakeLog("CREATION", "Besoin"));
        await repo.AjouterAsync(MakeLog("CREATION", "Besoin"));
        await repo.AjouterAsync(MakeLog("CREATION", "Utilisateur"));

        // Act
        var (items, total) = await repo.GetPagedAsync(new FiltreLogsDTO
        {
            Page = 1, PageSize = 10,
            EntiteType = "Besoin"
        });

        // Assert
        total.Should().Be(2);
        items.All(l => l.EntiteType == "Besoin").Should().BeTrue();
    }

    // ── GetPagedAsync — filtre par UtilisateurId ──────────────────────────────

    [Fact]
    public async Task GetPagedAsync_FiltreParUtilisateurId_RetourneSeulementSesLogs()
    {
        // Arrange
        using var db = CreateDb(nameof(GetPagedAsync_FiltreParUtilisateurId_RetourneSeulementSesLogs));
        var user = await SeedUserAsync(db);
        var repo = CreateRepo(db);

        await repo.AjouterAsync(MakeLog("CONNEXION", "auth", userId: user.Id));
        await repo.AjouterAsync(MakeLog("CREATION", "Besoin", userId: user.Id));
        await repo.AjouterAsync(MakeLog("CREATION", "Besoin", userId: 999)); // autre utilisateur

        // Act
        var (items, total) = await repo.GetPagedAsync(new FiltreLogsDTO
        {
            Page = 1, PageSize = 10,
            UtilisateurId = user.Id
        });

        // Assert
        total.Should().Be(2);
        items.All(l => l.UtilisateurId == user.Id).Should().BeTrue();
    }

    // ── GetPagedAsync — filtre par dates ──────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_FiltreParDateDebut_RetourneLogsApresDate()
    {
        // Arrange
        using var db = CreateDb(nameof(GetPagedAsync_FiltreParDateDebut_RetourneLogsApresDate));
        var repo = CreateRepo(db);

        var dateRef = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        await repo.AjouterAsync(MakeLog("ACTION_1", "Besoin", date: dateRef.AddDays(-5)));  // avant
        await repo.AjouterAsync(MakeLog("ACTION_2", "Besoin", date: dateRef));               // exactement
        await repo.AjouterAsync(MakeLog("ACTION_3", "Besoin", date: dateRef.AddDays(3)));   // après

        // Act
        var (items, total) = await repo.GetPagedAsync(new FiltreLogsDTO
        {
            Page = 1, PageSize = 10,
            DateDebut = dateRef
        });

        // Assert
        total.Should().Be(2);
    }

    [Fact]
    public async Task GetPagedAsync_FiltreParDateFin_RetourneLogsAvantDate()
    {
        // Arrange
        using var db = CreateDb(nameof(GetPagedAsync_FiltreParDateFin_RetourneLogsAvantDate));
        var repo = CreateRepo(db);

        var dateRef = new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc);
        await repo.AjouterAsync(MakeLog("ACTION_1", "Besoin", date: dateRef.AddDays(-5)));  // avant
        await repo.AjouterAsync(MakeLog("ACTION_2", "Besoin", date: dateRef));               // exactement
        await repo.AjouterAsync(MakeLog("ACTION_3", "Besoin", date: dateRef.AddDays(5)));   // après

        // Act
        var (items, total) = await repo.GetPagedAsync(new FiltreLogsDTO
        {
            Page = 1, PageSize = 10,
            DateFin = dateRef
        });

        // Assert
        total.Should().Be(2);
    }

    [Fact]
    public async Task GetPagedAsync_FiltrePlageDate_RetourneLogsInclus()
    {
        // Arrange
        using var db = CreateDb(nameof(GetPagedAsync_FiltrePlageDate_RetourneLogsInclus));
        var repo = CreateRepo(db);

        var debut = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var fin = new DateTime(2026, 4, 30, 0, 0, 0, DateTimeKind.Utc);

        await repo.AjouterAsync(MakeLog("A1", "Besoin", date: debut.AddDays(-1)));  // avant
        await repo.AjouterAsync(MakeLog("A2", "Besoin", date: debut));               // dans la plage
        await repo.AjouterAsync(MakeLog("A3", "Besoin", date: debut.AddDays(15)));  // dans la plage
        await repo.AjouterAsync(MakeLog("A4", "Besoin", date: fin));                 // dans la plage
        await repo.AjouterAsync(MakeLog("A5", "Besoin", date: fin.AddDays(1)));     // après

        // Act
        var (items, total) = await repo.GetPagedAsync(new FiltreLogsDTO
        {
            Page = 1, PageSize = 10,
            DateDebut = debut,
            DateFin = fin
        });

        // Assert
        total.Should().Be(3);
    }

    // ── GetPagedAsync — tri décroissant ───────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_TriDecroissant_PremierElementEstLePlusRecent()
    {
        // Arrange
        using var db = CreateDb(nameof(GetPagedAsync_TriDecroissant_PremierElementEstLePlusRecent));
        var repo = CreateRepo(db);

        var base_ = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        await repo.AjouterAsync(MakeLog("ANCIEN", "Besoin", date: base_));
        await repo.AjouterAsync(MakeLog("RECENT", "Besoin", date: base_.AddDays(10)));

        // Act
        var (items, _) = await repo.GetPagedAsync(new FiltreLogsDTO { Page = 1, PageSize = 10 });

        // Assert — le plus récent doit être en premier
        items.First().Action.Should().Be("RECENT");
    }

    // ── GetPagedAsync — combinaison de filtres ────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_FiltresCombinaison_RetourneResultatPrecis()
    {
        // Arrange
        using var db = CreateDb(nameof(GetPagedAsync_FiltresCombinaison_RetourneResultatPrecis));
        var user = await SeedUserAsync(db);
        var repo = CreateRepo(db);

        var dateRef = new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc);

        // Logs variés
        await repo.AjouterAsync(MakeLog("CREATION", "Besoin", userId: user.Id, date: dateRef));
        await repo.AjouterAsync(MakeLog("CREATION", "Besoin", userId: 999, date: dateRef));       // autre user
        await repo.AjouterAsync(MakeLog("CREATION", "Besoin", userId: user.Id, date: dateRef.AddDays(-20))); // hors plage
        await repo.AjouterAsync(MakeLog("MODIFICATION", "Besoin", userId: user.Id, date: dateRef)); // autre action

        // Act — filtrer par user + action + date
        var (items, total) = await repo.GetPagedAsync(new FiltreLogsDTO
        {
            Page = 1, PageSize = 10,
            UtilisateurId = user.Id,
            Action = "CREATION",
            DateDebut = dateRef.AddDays(-1)
        });

        // Assert — seul le premier log correspond
        total.Should().Be(1);
        items.First().Action.Should().Be("CREATION");
        items.First().UtilisateurId.Should().Be(user.Id);
    }
}
