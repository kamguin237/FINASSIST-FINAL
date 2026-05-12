using FinAssist.Application.Services;
using FinAssist.Core.DTOs.Logs;
using FinAssist.Core.Entities;
using FinAssist.Infrastructure.Data;
using FinAssist.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FinAssist.Tests.Integration;

/// <summary>
/// Tests d'intégration pour LogService + LogRepository + AppDbContext (InMemory).
/// </summary>
public class LogServiceIntegrationTests
{
    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    private static LogService CreateService(AppDbContext db)
        => new(new LogRepository(db));

    private static async Task<Utilisateur> SeedUserAsync(AppDbContext db)
    {
        var role = new Role { Code = "Agent", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow };
        db.Roles.Add(role);
        var user = new Utilisateur
        {
            Nom = "Dupont", Prenom = "Jean", Email = "jean@finstar-cm.com",
            MotDePasse = "hash", RoleId = role.Id, Actif = true,
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow
        };
        db.Utilisateurs.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    // ── LoggerAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task LoggerAsync_PersistLogEnBase()
    {
        // Arrange
        using var db = CreateDb(nameof(LoggerAsync_PersistLogEnBase));
        var service = CreateService(db);

        // Act
        await service.LoggerAsync(
            action: "CREATION",
            entiteType: "Besoin",
            entiteId: 1,
            nouvelleValeur: "Besoin créé",
            utilisateurId: 10,
            adresseIp: "192.168.1.1",
            pays: "Cameroun",
            ville: "Yaoundé");

        // Assert
        var logs = await db.LogsActivites.ToListAsync();
        logs.Should().HaveCount(1);
        logs[0].Action.Should().Be("CREATION");
        logs[0].EntiteType.Should().Be("Besoin");
        logs[0].EntiteId.Should().Be(1);
        logs[0].AdresseIp.Should().Be("192.168.1.1");
        logs[0].Pays.Should().Be("Cameroun");
    }

    [Fact]
    public async Task LoggerAsync_PlusieursLogs_TousPersistent()
    {
        // Arrange
        using var db = CreateDb(nameof(LoggerAsync_PlusieursLogs_TousPersistent));
        var service = CreateService(db);

        // Act
        await service.LoggerAsync("CREATION", "Besoin", entiteId: 1);
        await service.LoggerAsync("MODIFICATION", "Besoin", entiteId: 1);
        await service.LoggerAsync("SUPPRESSION", "Utilisateur", entiteId: 5);

        // Assert
        var logs = await db.LogsActivites.ToListAsync();
        logs.Should().HaveCount(3);
        logs.Select(l => l.Action).Should().Contain(["CREATION", "MODIFICATION", "SUPPRESSION"]);
    }

    // ── GetLogsAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetLogsAsync_RetourneLogsAvecPagination()
    {
        // Arrange
        using var db = CreateDb(nameof(GetLogsAsync_RetourneLogsAvecPagination));
        var service = CreateService(db);

        // Créer 5 logs
        for (int i = 1; i <= 5; i++)
            await service.LoggerAsync($"ACTION_{i}", "Besoin", entiteId: i);

        // Act — page 1, taille 3
        var (items, total) = await service.GetLogsAsync(new FiltreLogsDTO { Page = 1, PageSize = 3 });

        // Assert
        total.Should().Be(5);
        items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetLogsAsync_Page2_RetourneElementsRestants()
    {
        // Arrange
        using var db = CreateDb(nameof(GetLogsAsync_Page2_RetourneElementsRestants));
        var service = CreateService(db);

        for (int i = 1; i <= 5; i++)
            await service.LoggerAsync($"ACTION_{i}", "Besoin", entiteId: i);

        // Act — page 2, taille 3 → 2 éléments restants
        var (items, total) = await service.GetLogsAsync(new FiltreLogsDTO { Page = 2, PageSize = 3 });

        // Assert
        total.Should().Be(5);
        items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetLogsAsync_AvecUtilisateur_RetourneNomUtilisateur()
    {
        // Arrange
        using var db = CreateDb(nameof(GetLogsAsync_AvecUtilisateur_RetourneNomUtilisateur));
        var user = await SeedUserAsync(db);
        var service = CreateService(db);

        await service.LoggerAsync("CONNEXION", "auth", utilisateurId: user.Id);

        // Act
        var (items, _) = await service.GetLogsAsync(new FiltreLogsDTO { Page = 1, PageSize = 10 });

        // Assert
        var log = items.First();
        log.NomUtilisateur.Should().Be("Jean Dupont");
        log.UtilisateurId.Should().Be(user.Id);
    }

    [Fact]
    public async Task GetLogsAsync_SansLogs_RetourneListeVide()
    {
        // Arrange
        using var db = CreateDb(nameof(GetLogsAsync_SansLogs_RetourneListeVide));
        var service = CreateService(db);

        // Act
        var (items, total) = await service.GetLogsAsync(new FiltreLogsDTO { Page = 1, PageSize = 20 });

        // Assert
        total.Should().Be(0);
        items.Should().BeEmpty();
    }

    // ── DateKind UTC ──────────────────────────────────────────────────────────

    [Fact]
    public async Task LoggerAsync_DateEstUTC()
    {
        // Arrange
        using var db = CreateDb(nameof(LoggerAsync_DateEstUTC));
        var service = CreateService(db);

        // Act
        await service.LoggerAsync("TEST", "Entite");

        // Assert
        var log = await db.LogsActivites.FirstAsync();
        log.Date.Kind.Should().Be(DateTimeKind.Utc);
    }
}
