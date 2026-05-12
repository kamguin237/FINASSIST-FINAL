using FinAssist.Application.Services;
using FinAssist.Core.DTOs.Logs;
using FinAssist.Core.Entities;
using FinAssist.Infrastructure.Data;
using FinAssist.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FinAssist.Tests.Integration;

/// <summary>
/// Tests d'intégration complémentaires pour LogService — champs métadonnées
/// (IP, OS, navigateur, pays, ville, ancienneValeur, nouvelleValeur)
/// et filtres avancés du GetLogsAsync non couverts dans LogServiceIntegrationTests.
/// </summary>
public class LogServiceMetadataIntegrationTests
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

    // ═══════════════════════════════════════════════════════════════════════════
    // LoggerAsync — persistance des métadonnées
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task LoggerAsync_ToutesLesMetadonnees_PersistentCorrectement()
    {
        // Arrange
        using var db = CreateDb(nameof(LoggerAsync_ToutesLesMetadonnees_PersistentCorrectement));
        var service = CreateService(db);

        // Act
        await service.LoggerAsync(
            action: "POST /api/besoins",
            entiteType: "Besoin",
            entiteId: 42,
            ancienneValeur: "BROUILLON",
            nouvelleValeur: "ENREGISTRE",
            utilisateurId: 10,
            adresseIp: "41.202.219.100",
            systemeExploitation: "Windows 11",
            navigateur: "Chrome 124",
            pays: "Cameroun",
            ville: "Yaoundé");

        // Assert — toutes les métadonnées persistées
        var log = await db.LogsActivites.FirstAsync();
        log.Action.Should().Be("POST /api/besoins");
        log.EntiteType.Should().Be("Besoin");
        log.EntiteId.Should().Be(42);
        log.AncienneValeur.Should().Be("BROUILLON");
        log.NouvelleValeur.Should().Be("ENREGISTRE");
        log.UtilisateurId.Should().Be(10);
        log.AdresseIp.Should().Be("41.202.219.100");
        log.SystemeExploitation.Should().Be("Windows 11");
        log.Navigateur.Should().Be("Chrome 124");
        log.Pays.Should().Be("Cameroun");
        log.Ville.Should().Be("Yaoundé");
    }

    [Fact]
    public async Task LoggerAsync_SansMetadonnees_PersisteSansErreur()
    {
        // Arrange
        using var db = CreateDb(nameof(LoggerAsync_SansMetadonnees_PersisteSansErreur));
        var service = CreateService(db);

        // Act — appel minimal sans métadonnées optionnelles
        await service.LoggerAsync("CONNEXION", "auth");

        // Assert
        var log = await db.LogsActivites.FirstAsync();
        log.Action.Should().Be("CONNEXION");
        log.EntiteType.Should().Be("auth");
        log.EntiteId.Should().BeNull();
        log.UtilisateurId.Should().BeNull();
        log.AdresseIp.Should().BeNull();
        log.Pays.Should().BeNull();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetLogsAsync — métadonnées dans les DTOs retournés
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetLogsAsync_RetourneMetadonneesCompletes()
    {
        // Arrange
        using var db = CreateDb(nameof(GetLogsAsync_RetourneMetadonneesCompletes));
        var service = CreateService(db);

        await service.LoggerAsync(
            action: "DELETE /api/users/5",
            entiteType: "Utilisateur",
            entiteId: 5,
            ancienneValeur: "actif",
            nouvelleValeur: "supprimé",
            adresseIp: "192.168.1.50",
            systemeExploitation: "Ubuntu 22",
            navigateur: "Firefox 125",
            pays: "France",
            ville: "Paris");

        // Act
        var (items, _) = await service.GetLogsAsync(new FiltreLogsDTO { Page = 1, PageSize = 10 });

        // Assert — DTO contient toutes les métadonnées
        var dto = items.First();
        dto.Action.Should().Be("DELETE /api/users/5");
        dto.EntiteType.Should().Be("Utilisateur");
        dto.EntiteId.Should().Be(5);
        dto.AncienneValeur.Should().Be("actif");
        dto.NouvelleValeur.Should().Be("supprimé");
        dto.AdresseIp.Should().Be("192.168.1.50");
        dto.SystemeExploitation.Should().Be("Ubuntu 22");
        dto.Navigateur.Should().Be("Firefox 125");
        dto.Pays.Should().Be("France");
        dto.Ville.Should().Be("Paris");
    }

    [Fact]
    public async Task GetLogsAsync_DateEstUTCDansDTO()
    {
        // Arrange
        using var db = CreateDb(nameof(GetLogsAsync_DateEstUTCDansDTO));
        var service = CreateService(db);

        await service.LoggerAsync("TEST", "Entite");

        // Act
        var (items, _) = await service.GetLogsAsync(new FiltreLogsDTO { Page = 1, PageSize = 10 });

        // Assert — la date dans le DTO est UTC
        items.First().Date.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task GetLogsAsync_AvecUtilisateur_NomFormatePrenomNom()
    {
        // Arrange
        using var db = CreateDb(nameof(GetLogsAsync_AvecUtilisateur_NomFormatePrenomNom));
        var user = await SeedUserAsync(db);
        var service = CreateService(db);

        await service.LoggerAsync("CONNEXION", "auth", utilisateurId: user.Id);

        // Act
        var (items, _) = await service.GetLogsAsync(new FiltreLogsDTO { Page = 1, PageSize = 10 });

        // Assert — format "Prenom Nom"
        items.First().NomUtilisateur.Should().Be("Jean Dupont");
    }

    [Fact]
    public async Task GetLogsAsync_SansUtilisateur_NomUtilisateurEstNull()
    {
        // Arrange
        using var db = CreateDb(nameof(GetLogsAsync_SansUtilisateur_NomUtilisateurEstNull));
        var service = CreateService(db);

        await service.LoggerAsync("SYSTEME", "Cron"); // pas d'utilisateurId

        // Act
        var (items, _) = await service.GetLogsAsync(new FiltreLogsDTO { Page = 1, PageSize = 10 });

        // Assert
        items.First().NomUtilisateur.Should().BeNull();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetLogsAsync — filtres avancés (complément de LogRepositoryIntegrationTests)
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetLogsAsync_FiltreParAction_RetourneSeulementCetteAction()
    {
        // Arrange
        using var db = CreateDb(nameof(GetLogsAsync_FiltreParAction_RetourneSeulementCetteAction));
        var service = CreateService(db);

        await service.LoggerAsync("POST /api/besoins", "Besoin");
        await service.LoggerAsync("POST /api/besoins", "Besoin");
        await service.LoggerAsync("DELETE /api/besoins/1", "Besoin");

        // Act
        var (items, total) = await service.GetLogsAsync(new FiltreLogsDTO
        {
            Page = 1, PageSize = 10,
            Action = "POST"
        });

        // Assert — filtre par contenu de l'action
        total.Should().Be(2);
        items.All(l => l.Action.Contains("POST")).Should().BeTrue();
    }

    [Fact]
    public async Task GetLogsAsync_FiltreParEntiteType_RetourneSeulementCetteEntite()
    {
        // Arrange
        using var db = CreateDb(nameof(GetLogsAsync_FiltreParEntiteType_RetourneSeulementCetteEntite));
        var service = CreateService(db);

        await service.LoggerAsync("CREATION", "Besoin");
        await service.LoggerAsync("CREATION", "Besoin");
        await service.LoggerAsync("CREATION", "Utilisateur");

        // Act
        var (items, total) = await service.GetLogsAsync(new FiltreLogsDTO
        {
            Page = 1, PageSize = 10,
            EntiteType = "Besoin"
        });

        // Assert
        total.Should().Be(2);
        items.All(l => l.EntiteType == "Besoin").Should().BeTrue();
    }

    [Fact]
    public async Task GetLogsAsync_FiltreParUtilisateurId_RetourneSesSesLogs()
    {
        // Arrange
        using var db = CreateDb(nameof(GetLogsAsync_FiltreParUtilisateurId_RetourneSesSesLogs));
        var user = await SeedUserAsync(db);
        var service = CreateService(db);

        await service.LoggerAsync("CONNEXION", "auth", utilisateurId: user.Id);
        await service.LoggerAsync("CREATION", "Besoin", utilisateurId: user.Id);
        await service.LoggerAsync("CREATION", "Besoin", utilisateurId: 999); // autre user

        // Act
        var (items, total) = await service.GetLogsAsync(new FiltreLogsDTO
        {
            Page = 1, PageSize = 10,
            UtilisateurId = user.Id
        });

        // Assert
        total.Should().Be(2);
        items.All(l => l.UtilisateurId == user.Id).Should().BeTrue();
    }

    [Fact]
    public async Task GetLogsAsync_TriDecroissant_PremierLogEstLePlusRecent()
    {
        // Arrange
        using var db = CreateDb(nameof(GetLogsAsync_TriDecroissant_PremierLogEstLePlusRecent));
        var service = CreateService(db);

        // Insérer avec un délai pour garantir l'ordre
        await service.LoggerAsync("PREMIER", "Entite");
        await Task.Delay(10);
        await service.LoggerAsync("DERNIER", "Entite");

        // Act
        var (items, _) = await service.GetLogsAsync(new FiltreLogsDTO { Page = 1, PageSize = 10 });

        // Assert — le plus récent en premier
        items.First().Action.Should().Be("DERNIER");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // LocalisationFormatee — propriété calculée du DTO
    // ═══════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("Yaoundé", "Cameroun", "📍 Yaoundé, Cameroun")]
    [InlineData("Local",   "Local",    "🏠 Local")]
    [InlineData("Inconnu", "Inconnu",  "—")]
    [InlineData(null,      "France",   "📍 France")]
    public async Task GetLogsAsync_LocalisationFormatee_CalculeeCorrectement(
        string? ville, string? pays, string attendu)
    {
        // Arrange
        using var db = CreateDb($"{nameof(GetLogsAsync_LocalisationFormatee_CalculeeCorrectement)}_{ville}_{pays}");
        var service = CreateService(db);

        await service.LoggerAsync("TEST", "Entite", pays: pays, ville: ville);

        // Act
        var (items, _) = await service.GetLogsAsync(new FiltreLogsDTO { Page = 1, PageSize = 10 });

        // Assert
        items.First().LocalisationFormatee.Should().Be(attendu);
    }
}
