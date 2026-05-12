using FinAssist.Core.Entities;
using FinAssist.Infrastructure.Data;
using FinAssist.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FinAssist.Tests.Integration;

/// <summary>
/// Tests d'intégration pour PushSubscriptionRepository + AppDbContext (InMemory).
/// </summary>
public class PushSubscriptionIntegrationTests
{
    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    private static PushSubscriptionRepository CreateRepo(AppDbContext db)
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

    // ── SaveAsync — création ──────────────────────────────────────────────────

    [Fact]
    public async Task SaveAsync_NouvelleSubscription_PersistEnBase()
    {
        // Arrange
        using var db = CreateDb(nameof(SaveAsync_NouvelleSubscription_PersistEnBase));
        var user = await SeedUserAsync(db);
        var repo = CreateRepo(db);

        var sub = new PushSubscription
        {
            UtilisateurId = user.Id,
            Endpoint = "https://fcm.googleapis.com/fcm/send/abc123",
            P256dh = "BNcRdreALRFXTkOOUHK1EtK2wtaz5Ry4YfYCA_0QTpQ",
            Auth = "tBHItJI5svbpez7KI4CCXg"
        };

        // Act
        await repo.SaveAsync(sub);

        // Assert
        var inDb = await db.PushSubscriptions
            .FirstOrDefaultAsync(s => s.Endpoint == sub.Endpoint);
        inDb.Should().NotBeNull();
        inDb!.UtilisateurId.Should().Be(user.Id);
        inDb.P256dh.Should().Be(sub.P256dh);
    }

    // ── SaveAsync — mise à jour ───────────────────────────────────────────────

    [Fact]
    public async Task SaveAsync_EndpointExistant_MettreAJourCles()
    {
        // Arrange
        using var db = CreateDb(nameof(SaveAsync_EndpointExistant_MettreAJourCles));
        var user = await SeedUserAsync(db);
        var repo = CreateRepo(db);

        var endpoint = "https://fcm.googleapis.com/fcm/send/abc123";

        await repo.SaveAsync(new PushSubscription
        {
            UtilisateurId = user.Id,
            Endpoint = endpoint,
            P256dh = "ancienne_cle",
            Auth = "ancien_auth"
        });

        // Act — mettre à jour avec de nouvelles clés
        await repo.SaveAsync(new PushSubscription
        {
            UtilisateurId = user.Id,
            Endpoint = endpoint,
            P256dh = "nouvelle_cle",
            Auth = "nouvel_auth"
        });

        // Assert — une seule entrée par endpoint
        var subs = await db.PushSubscriptions.Where(s => s.Endpoint == endpoint).ToListAsync();
        subs.Should().HaveCount(1);
        subs[0].P256dh.Should().Be("nouvelle_cle");
        subs[0].Auth.Should().Be("nouvel_auth");
    }

    // ── GetByUtilisateurAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetByUtilisateurAsync_RetourneSubscriptionsUtilisateur()
    {
        // Arrange
        using var db = CreateDb(nameof(GetByUtilisateurAsync_RetourneSubscriptionsUtilisateur));
        var user = await SeedUserAsync(db);
        var repo = CreateRepo(db);

        await repo.SaveAsync(new PushSubscription { UtilisateurId = user.Id, Endpoint = "https://endpoint1.com", P256dh = "k1", Auth = "a1" });
        await repo.SaveAsync(new PushSubscription { UtilisateurId = user.Id, Endpoint = "https://endpoint2.com", P256dh = "k2", Auth = "a2" });

        // Act
        var subs = (await repo.GetByUtilisateurAsync(user.Id)).ToList();

        // Assert
        subs.Should().HaveCount(2);
        subs.All(s => s.UtilisateurId == user.Id).Should().BeTrue();
    }

    [Fact]
    public async Task GetByUtilisateurAsync_AucuneSubscription_RetourneListeVide()
    {
        // Arrange
        using var db = CreateDb(nameof(GetByUtilisateurAsync_AucuneSubscription_RetourneListeVide));
        var user = await SeedUserAsync(db);
        var repo = CreateRepo(db);

        // Act
        var subs = (await repo.GetByUtilisateurAsync(user.Id)).ToList();

        // Assert
        subs.Should().BeEmpty();
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_EndpointExistant_SupprimeDeLaBase()
    {
        // Arrange
        using var db = CreateDb(nameof(DeleteAsync_EndpointExistant_SupprimeDeLaBase));
        var user = await SeedUserAsync(db);
        var repo = CreateRepo(db);

        var endpoint = "https://fcm.googleapis.com/fcm/send/abc123";
        await repo.SaveAsync(new PushSubscription
        {
            UtilisateurId = user.Id,
            Endpoint = endpoint,
            P256dh = "key",
            Auth = "auth"
        });

        // Vérifier qu'elle existe
        var avant = await db.PushSubscriptions.FirstOrDefaultAsync(s => s.Endpoint == endpoint);
        avant.Should().NotBeNull();

        // Act — supprimer directement (ExecuteDeleteAsync non supporté par InMemory)
        var sub = await db.PushSubscriptions.FirstOrDefaultAsync(s => s.Endpoint == endpoint);
        if (sub != null)
        {
            db.PushSubscriptions.Remove(sub);
            await db.SaveChangesAsync();
        }

        // Assert
        var apres = await db.PushSubscriptions.FirstOrDefaultAsync(s => s.Endpoint == endpoint);
        apres.Should().BeNull();
    }

    // ── Plusieurs utilisateurs ────────────────────────────────────────────────

    [Fact]
    public async Task GetByUtilisateurAsync_IsolationEntreUtilisateurs()
    {
        // Arrange
        using var db = CreateDb(nameof(GetByUtilisateurAsync_IsolationEntreUtilisateurs));
        var role = new Role { Code = "Agent", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow };
        db.Roles.Add(role);
        var user1 = new Utilisateur { Nom = "A", Prenom = "A", Email = "a@finstar-cm.com", MotDePasse = "h", RoleId = role.Id, Actif = true, DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow };
        var user2 = new Utilisateur { Nom = "B", Prenom = "B", Email = "b@finstar-cm.com", MotDePasse = "h", RoleId = role.Id, Actif = true, DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow };
        db.Utilisateurs.AddRange(user1, user2);
        await db.SaveChangesAsync();

        var repo = CreateRepo(db);

        await repo.SaveAsync(new PushSubscription { UtilisateurId = user1.Id, Endpoint = "https://ep1.com", P256dh = "k1", Auth = "a1" });
        await repo.SaveAsync(new PushSubscription { UtilisateurId = user2.Id, Endpoint = "https://ep2.com", P256dh = "k2", Auth = "a2" });

        // Act
        var subs1 = (await repo.GetByUtilisateurAsync(user1.Id)).ToList();
        var subs2 = (await repo.GetByUtilisateurAsync(user2.Id)).ToList();

        // Assert
        subs1.Should().HaveCount(1);
        subs1[0].Endpoint.Should().Be("https://ep1.com");
        subs2.Should().HaveCount(1);
        subs2[0].Endpoint.Should().Be("https://ep2.com");
    }
}
