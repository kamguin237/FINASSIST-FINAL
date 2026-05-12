using FinAssist.Core.Entities;
using FinAssist.Infrastructure.Data;
using FinAssist.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FinAssist.Tests.Integration;

/// <summary>
/// Tests d'intégration pour UserPreferencesRepository + AppDbContext (InMemory).
/// </summary>
public class UserPreferencesIntegrationTests
{
    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    private static UserPreferencesRepository CreateRepo(AppDbContext db)
        => new(db);

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

    // ── GetByUtilisateurIdAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetByUtilisateurIdAsync_AucunePreference_RetourneNull()
    {
        // Arrange
        using var db = CreateDb(nameof(GetByUtilisateurIdAsync_AucunePreference_RetourneNull));
        var user = await SeedUserAsync(db);
        var repo = CreateRepo(db);

        // Act
        var result = await repo.GetByUtilisateurIdAsync(user.Id);

        // Assert
        result.Should().BeNull();
    }

    // ── SaveAsync — création ──────────────────────────────────────────────────

    [Fact]
    public async Task SaveAsync_NouvellesPreferences_PersistEnBase()
    {
        // Arrange
        using var db = CreateDb(nameof(SaveAsync_NouvellesPreferences_PersistEnBase));
        var user = await SeedUserAsync(db);
        var repo = CreateRepo(db);

        var prefs = new UserPreferences
        {
            UtilisateurId = user.Id,
            NotifApp = true,
            NotifEmail = false,
            Langue = "fr",
            FormatDate = "dd/MM/yyyy",
            FuseauHoraire = "Africa/Douala",
            ItemsParPage = 20,
            PageAccueil = "/dashboard",
            TriDefaut = "dateDesc",
            DateModification = DateTime.UtcNow
        };

        // Act
        var result = await repo.SaveAsync(prefs);

        // Assert
        result.Should().NotBeNull();
        result.Langue.Should().Be("fr");
        result.NotifApp.Should().BeTrue();

        var inDb = await db.UserPreferences.FirstOrDefaultAsync(p => p.UtilisateurId == user.Id);
        inDb.Should().NotBeNull();
        inDb!.ItemsParPage.Should().Be(20);
    }

    // ── SaveAsync — mise à jour ───────────────────────────────────────────────

    [Fact]
    public async Task SaveAsync_PreferencesExistantes_MettreAJour()
    {
        // Arrange
        using var db = CreateDb(nameof(SaveAsync_PreferencesExistantes_MettreAJour));
        var user = await SeedUserAsync(db);
        var repo = CreateRepo(db);

        // Créer les préférences initiales
        await repo.SaveAsync(new UserPreferences
        {
            UtilisateurId = user.Id,
            Langue = "fr",
            ItemsParPage = 20,
            DateModification = DateTime.UtcNow
        });

        // Act — mettre à jour
        await repo.SaveAsync(new UserPreferences
        {
            UtilisateurId = user.Id,
            Langue = "en",
            ItemsParPage = 50,
            NotifEmail = true,
            DateModification = DateTime.UtcNow
        });

        // Assert — une seule entrée par utilisateur
        var allPrefs = await db.UserPreferences.Where(p => p.UtilisateurId == user.Id).ToListAsync();
        allPrefs.Should().HaveCount(1);
        allPrefs[0].Langue.Should().Be("en");
        allPrefs[0].ItemsParPage.Should().Be(50);
        allPrefs[0].NotifEmail.Should().BeTrue();
    }

    // ── Plusieurs utilisateurs ────────────────────────────────────────────────

    [Fact]
    public async Task SaveAsync_DeuxUtilisateurs_PreferencesIsolees()
    {
        // Arrange
        using var db = CreateDb(nameof(SaveAsync_DeuxUtilisateurs_PreferencesIsolees));
        var role = new Role { Code = "Agent", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow };
        db.Roles.Add(role);
        var user1 = new Utilisateur { Nom = "A", Prenom = "A", Email = "a@finstar-cm.com", MotDePasse = "h", RoleId = role.Id, Actif = true, DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow };
        var user2 = new Utilisateur { Nom = "B", Prenom = "B", Email = "b@finstar-cm.com", MotDePasse = "h", RoleId = role.Id, Actif = true, DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow };
        db.Utilisateurs.AddRange(user1, user2);
        await db.SaveChangesAsync();

        var repo = CreateRepo(db);

        // Act
        await repo.SaveAsync(new UserPreferences { UtilisateurId = user1.Id, Langue = "fr", DateModification = DateTime.UtcNow });
        await repo.SaveAsync(new UserPreferences { UtilisateurId = user2.Id, Langue = "en", DateModification = DateTime.UtcNow });

        // Assert
        var prefs1 = await repo.GetByUtilisateurIdAsync(user1.Id);
        var prefs2 = await repo.GetByUtilisateurIdAsync(user2.Id);

        prefs1!.Langue.Should().Be("fr");
        prefs2!.Langue.Should().Be("en");
    }

    // ── Valeurs par défaut ────────────────────────────────────────────────────

    [Fact]
    public async Task SaveAsync_TousLesChamps_PersistentCorrectement()
    {
        // Arrange
        using var db = CreateDb(nameof(SaveAsync_TousLesChamps_PersistentCorrectement));
        var user = await SeedUserAsync(db);
        var repo = CreateRepo(db);

        var prefs = new UserPreferences
        {
            UtilisateurId = user.Id,
            NotifApp = true,
            NotifEmail = true,
            AlertNouveauBesoin = true,
            AlertValidation = false,
            AlertEnAttente = true,
            Langue = "en",
            FormatDate = "MM/dd/yyyy",
            FuseauHoraire = "UTC",
            ItemsParPage = 50,
            PageAccueil = "/besoins",
            TriDefaut = "titre",
            DateModification = DateTime.UtcNow
        };

        // Act
        var result = await repo.SaveAsync(prefs);

        // Assert
        result.NotifApp.Should().BeTrue();
        result.NotifEmail.Should().BeTrue();
        result.AlertValidation.Should().BeFalse();
        result.Langue.Should().Be("en");
        result.ItemsParPage.Should().Be(50);
        result.PageAccueil.Should().Be("/besoins");
        result.TriDefaut.Should().Be("titre");
    }
}
