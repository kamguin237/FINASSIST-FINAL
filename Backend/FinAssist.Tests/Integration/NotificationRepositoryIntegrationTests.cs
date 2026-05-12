using FinAssist.Core.Entities;
using FinAssist.Infrastructure.Data;
using FinAssist.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FinAssist.Tests.Integration;

/// <summary>
/// Tests d'intégration pour NotificationRepository + AppDbContext (InMemory).
/// </summary>
public class NotificationRepositoryIntegrationTests
{
    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    private static NotificationRepository CreateRepo(AppDbContext db)
        => new(db);

    private static async Task<List<Utilisateur>> SeedUsersAsync(AppDbContext db, int count = 2)
    {
        var role = new Role { Code = "Agent", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow };
        db.Roles.Add(role);
        var users = Enumerable.Range(1, count).Select(i => new Utilisateur
        {
            Nom = $"User{i}", Prenom = $"Prenom{i}",
            Email = $"user{i}@finstar-cm.com",
            MotDePasse = "hash", RoleId = role.Id, Actif = true,
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow
        }).ToList();
        db.Utilisateurs.AddRange(users);
        await db.SaveChangesAsync();
        return users;
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_NotificationAvecDestinataires_PersistEnBase()
    {
        // Arrange
        using var db = CreateDb(nameof(CreateAsync_NotificationAvecDestinataires_PersistEnBase));
        var users = await SeedUsersAsync(db, 3);
        var repo = CreateRepo(db);

        var notif = new Notification
        {
            Message = "Test notification",
            Type = TypeNotification.VALIDATION,
            DateEnvoi = DateTime.UtcNow
        };

        // Act
        var result = await repo.CreateAsync(notif, users.Select(u => u.Id));

        // Assert
        result.Should().NotBeNull();
        result.Message.Should().Be("Test notification");

        // Vérifier les liaisons utilisateur-notification
        var liens = await db.UtilisateurNotifications
            .Where(un => un.NotificationId == result.Id)
            .ToListAsync();
        liens.Should().HaveCount(3);
    }

    [Fact]
    public async Task CreateAsync_SansDestinataires_PersistNotificationSeule()
    {
        // Arrange
        using var db = CreateDb(nameof(CreateAsync_SansDestinataires_PersistNotificationSeule));
        var repo = CreateRepo(db);

        var notif = new Notification
        {
            Message = "Notification sans destinataire",
            Type = TypeNotification.RAPPEL,
            DateEnvoi = DateTime.UtcNow
        };

        // Act
        var result = await repo.CreateAsync(notif, []);

        // Assert
        result.Should().NotBeNull();
        var liens = await db.UtilisateurNotifications
            .Where(un => un.NotificationId == result.Id)
            .ToListAsync();
        liens.Should().BeEmpty();
    }

    // ── GetByUtilisateurAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetByUtilisateurAsync_RetourneNotificationsUtilisateur()
    {
        // Arrange
        using var db = CreateDb(nameof(GetByUtilisateurAsync_RetourneNotificationsUtilisateur));
        var users = await SeedUsersAsync(db, 2);
        var repo = CreateRepo(db);

        // Notif pour user[0] seulement
        await repo.CreateAsync(new Notification { Message = "Pour user 0", Type = TypeNotification.VALIDATION, DateEnvoi = DateTime.UtcNow }, [users[0].Id]);
        // Notif pour les deux
        await repo.CreateAsync(new Notification { Message = "Pour tous", Type = TypeNotification.RAPPEL, DateEnvoi = DateTime.UtcNow }, users.Select(u => u.Id));

        // Act
        var notifs = (await repo.GetByUtilisateurAsync(users[0].Id)).ToList();

        // Assert
        notifs.Should().HaveCount(2);
        notifs.All(n => n.UtilisateurId == users[0].Id).Should().BeTrue();
    }

    // ── MarquerLuAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task MarquerLuAsync_NotificationNonLue_PasseLue()
    {
        // Arrange
        using var db = CreateDb(nameof(MarquerLuAsync_NotificationNonLue_PasseLue));
        var users = await SeedUsersAsync(db, 1);
        var repo = CreateRepo(db);

        var notif = await repo.CreateAsync(
            new Notification { Message = "Test", Type = TypeNotification.VALIDATION, DateEnvoi = DateTime.UtcNow },
            [users[0].Id]);

        // Vérifier qu'elle est non lue
        var avant = await db.UtilisateurNotifications
            .FirstAsync(un => un.NotificationId == notif.Id && un.UtilisateurId == users[0].Id);
        avant.Lu.Should().BeFalse();

        // Act
        await repo.MarquerLuAsync(notif.Id, users[0].Id);

        // Assert
        var apres = await db.UtilisateurNotifications
            .FirstAsync(un => un.NotificationId == notif.Id && un.UtilisateurId == users[0].Id);
        apres.Lu.Should().BeTrue();
        apres.DateLecture.Should().NotBeNull();
    }

    // ── GetUtilisateurIdsByRoleAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetUtilisateurIdsByRoleAsync_RetourneUtilisateursActifsDuRole()
    {
        // Arrange
        using var db = CreateDb(nameof(GetUtilisateurIdsByRoleAsync_RetourneUtilisateursActifsDuRole));
        var role = new Role { Code = "Responsable", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow };
        var roleAgent = new Role { Code = "Agent", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow };
        db.Roles.AddRange(role, roleAgent);

        db.Utilisateurs.AddRange(
            new Utilisateur { Nom = "A", Prenom = "A", Email = "a@finstar-cm.com", MotDePasse = "h", RoleId = role.Id, Actif = true, DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow },
            new Utilisateur { Nom = "B", Prenom = "B", Email = "b@finstar-cm.com", MotDePasse = "h", RoleId = role.Id, Actif = true, DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow },
            new Utilisateur { Nom = "C", Prenom = "C", Email = "c@finstar-cm.com", MotDePasse = "h", RoleId = role.Id, Actif = false, DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow }, // inactif
            new Utilisateur { Nom = "D", Prenom = "D", Email = "d@finstar-cm.com", MotDePasse = "h", RoleId = roleAgent.Id, Actif = true, DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var repo = CreateRepo(db);

        // Act
        var ids = (await repo.GetUtilisateurIdsByRoleAsync("Responsable")).ToList();

        // Assert — 2 actifs Responsable (pas l'inactif, pas l'Agent)
        ids.Should().HaveCount(2);
    }
}
