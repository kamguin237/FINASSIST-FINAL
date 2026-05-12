using FinAssist.Core.Entities;
using FinAssist.Infrastructure.Data;
using FinAssist.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FinAssist.Tests.Integration;

/// <summary>
/// Tests d'intégration pour QrSessionRepository + AppDbContext (InMemory).
/// </summary>
public class QrSessionIntegrationTests
{
    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    private static QrSessionRepository CreateRepo(AppDbContext db)
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

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_SessionValide_PersistEnBase()
    {
        // Arrange
        using var db = CreateDb(nameof(CreateAsync_SessionValide_PersistEnBase));
        var user = await SeedUserAsync(db);
        var repo = CreateRepo(db);

        var session = new QrSignatureSession
        {
            Token = "abc123def456",
            UtilisateurId = user.Id,
            CreeLe = DateTime.UtcNow,
            Expiration = DateTime.UtcNow.AddMinutes(10),
            Completed = false
        };

        // Act
        var result = await repo.CreateAsync(session);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().Be("abc123def456");

        var inDb = await db.QrSignatureSessions.FindAsync(result.Id);
        inDb.Should().NotBeNull();
        inDb!.Completed.Should().BeFalse();
    }

    // ── GetByTokenAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetByTokenAsync_TokenExistant_RetourneSession()
    {
        // Arrange
        using var db = CreateDb(nameof(GetByTokenAsync_TokenExistant_RetourneSession));
        var user = await SeedUserAsync(db);
        var repo = CreateRepo(db);

        await repo.CreateAsync(new QrSignatureSession
        {
            Token = "token_test_123",
            UtilisateurId = user.Id,
            CreeLe = DateTime.UtcNow,
            Expiration = DateTime.UtcNow.AddMinutes(10),
            Completed = false
        });

        // Act
        var result = await repo.GetByTokenAsync("token_test_123");

        // Assert
        result.Should().NotBeNull();
        result!.Token.Should().Be("token_test_123");
        result.UtilisateurId.Should().Be(user.Id);
    }

    [Fact]
    public async Task GetByTokenAsync_TokenInexistant_RetourneNull()
    {
        // Arrange
        using var db = CreateDb(nameof(GetByTokenAsync_TokenInexistant_RetourneNull));
        var repo = CreateRepo(db);

        // Act
        var result = await repo.GetByTokenAsync("token_inexistant");

        // Assert
        result.Should().BeNull();
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_MarquerComplete_PersisteLaModification()
    {
        // Arrange
        using var db = CreateDb(nameof(UpdateAsync_MarquerComplete_PersisteLaModification));
        var user = await SeedUserAsync(db);
        var repo = CreateRepo(db);

        var session = await repo.CreateAsync(new QrSignatureSession
        {
            Token = "token_update",
            UtilisateurId = user.Id,
            CreeLe = DateTime.UtcNow,
            Expiration = DateTime.UtcNow.AddMinutes(10),
            Completed = false
        });

        // Act
        session.Completed = true;
        session.SignatureBase64 = "data:image/png;base64,signature";
        session.CompletedAt = DateTime.UtcNow;
        await repo.UpdateAsync(session);

        // Assert
        var inDb = await db.QrSignatureSessions.FindAsync(session.Id);
        inDb!.Completed.Should().BeTrue();
        inDb.SignatureBase64.Should().Be("data:image/png;base64,signature");
        inDb.CompletedAt.Should().NotBeNull();
    }

    // ── Expiration ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByTokenAsync_SessionExpiree_RetourneSessionMaisExpiration()
    {
        // Arrange
        using var db = CreateDb(nameof(GetByTokenAsync_SessionExpiree_RetourneSessionMaisExpiration));
        var user = await SeedUserAsync(db);
        var repo = CreateRepo(db);

        await repo.CreateAsync(new QrSignatureSession
        {
            Token = "token_expire",
            UtilisateurId = user.Id,
            CreeLe = DateTime.UtcNow.AddMinutes(-20),
            Expiration = DateTime.UtcNow.AddMinutes(-10), // expirée
            Completed = false
        });

        // Act
        var result = await repo.GetByTokenAsync("token_expire");

        // Assert — la session est retournée mais son expiration est dans le passé
        result.Should().NotBeNull();
        result!.Expiration.Should().BeBefore(DateTime.UtcNow);
    }
}
