using FinAssist.Core.Entities;
using FinAssist.Infrastructure.Data;
using FinAssist.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FinAssist.Tests.Integration;

/// <summary>
/// Tests d'intégration pour RolesRepository + AppDbContext (InMemory).
/// Note : DeleteAsync utilise ExecuteDeleteAsync (non supporté par InMemory)
/// → on teste la suppression via Remove() + SaveChangesAsync directement.
/// </summary>
public class RolesRepositoryIntegrationTests
{
    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    private static RolesRepository CreateRepo(AppDbContext db) => new(db);

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_RoleValide_PersistEnBase()
    {
        // Arrange
        using var db = CreateDb(nameof(CreateAsync_RoleValide_PersistEnBase));
        var repo = CreateRepo(db);

        var role = new Role
        {
            Code = "Responsable",
            DateCreation = DateTime.UtcNow,
            DateModification = DateTime.UtcNow
        };

        // Act
        var result = await repo.CreateAsync(role);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Code.Should().Be("Responsable");

        var inDb = await db.Roles.FindAsync(result.Id);
        inDb.Should().NotBeNull();
        inDb!.Code.Should().Be("Responsable");
    }

    [Fact]
    public async Task CreateAsync_PlusieursRoles_TousPersistent()
    {
        // Arrange
        using var db = CreateDb(nameof(CreateAsync_PlusieursRoles_TousPersistent));
        var repo = CreateRepo(db);

        // Act
        await repo.CreateAsync(new Role { Code = "Agent", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow });
        await repo.CreateAsync(new Role { Code = "Responsable", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow });
        await repo.CreateAsync(new Role { Code = "Direction", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow });

        // Assert
        var all = (await repo.GetAllAsync()).ToList();
        all.Should().HaveCount(3);
        all.Select(r => r.Code).Should().Contain(["Agent", "Responsable", "Direction"]);
    }

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_BaseVide_RetourneListeVide()
    {
        // Arrange
        using var db = CreateDb(nameof(GetAllAsync_BaseVide_RetourneListeVide));
        var repo = CreateRepo(db);

        // Act
        var result = (await repo.GetAllAsync()).ToList();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_ApresCreation_RetourneTousLesRoles()
    {
        // Arrange
        using var db = CreateDb(nameof(GetAllAsync_ApresCreation_RetourneTousLesRoles));
        var repo = CreateRepo(db);

        await repo.CreateAsync(new Role { Code = "Agent", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow });
        await repo.CreateAsync(new Role { Code = "Admin", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow });

        // Act
        var result = (await repo.GetAllAsync()).ToList();

        // Assert
        result.Should().HaveCount(2);
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_RoleExistant_RetourneRole()
    {
        // Arrange
        using var db = CreateDb(nameof(GetByIdAsync_RoleExistant_RetourneRole));
        var repo = CreateRepo(db);

        var created = await repo.CreateAsync(new Role
        {
            Code = "Responsable",
            DateCreation = DateTime.UtcNow,
            DateModification = DateTime.UtcNow
        });

        // Act
        var result = await repo.GetByIdAsync(created.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be("Responsable");
    }

    [Fact]
    public async Task GetByIdAsync_RoleInexistant_RetourneNull()
    {
        // Arrange
        using var db = CreateDb(nameof(GetByIdAsync_RoleInexistant_RetourneNull));
        var repo = CreateRepo(db);

        // Act
        var result = await repo.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    // ── ExistsAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ExistsAsync_CodeExistant_RetourneTrue()
    {
        // Arrange
        using var db = CreateDb(nameof(ExistsAsync_CodeExistant_RetourneTrue));
        var repo = CreateRepo(db);

        await repo.CreateAsync(new Role { Code = "Agent", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow });

        // Act
        var exists = await repo.ExistsAsync("Agent");

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_CodeInexistant_RetourneFalse()
    {
        // Arrange
        using var db = CreateDb(nameof(ExistsAsync_CodeInexistant_RetourneFalse));
        var repo = CreateRepo(db);

        // Act
        var exists = await repo.ExistsAsync("Inconnu");

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_CodeCasseInsensible_RetourneTrue()
    {
        // Arrange
        using var db = CreateDb(nameof(ExistsAsync_CodeCasseInsensible_RetourneTrue));
        var repo = CreateRepo(db);

        await repo.CreateAsync(new Role { Code = "Agent", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow });

        // Act — recherche en minuscules
        var exists = await repo.ExistsAsync("agent");

        // Assert
        exists.Should().BeTrue();
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ModifieCode_PersisteLaModification()
    {
        // Arrange
        using var db = CreateDb(nameof(UpdateAsync_ModifieCode_PersisteLaModification));
        var repo = CreateRepo(db);

        var role = await repo.CreateAsync(new Role
        {
            Code = "AncienCode",
            DateCreation = DateTime.UtcNow,
            DateModification = DateTime.UtcNow
        });

        // Act
        role.Code = "NouveauCode";
        role.DateModification = DateTime.UtcNow;
        var updated = await repo.UpdateAsync(role);

        // Assert
        updated.Code.Should().Be("NouveauCode");

        var inDb = await db.Roles.FindAsync(role.Id);
        inDb!.Code.Should().Be("NouveauCode");
    }

    // ── Delete (via Remove + SaveChanges — ExecuteDeleteAsync non supporté InMemory) ──

    [Fact]
    public async Task Delete_RoleSansUtilisateurs_SupprimeDeLaBase()
    {
        // Arrange
        using var db = CreateDb(nameof(Delete_RoleSansUtilisateurs_SupprimeDeLaBase));
        var repo = CreateRepo(db);

        var role = await repo.CreateAsync(new Role
        {
            Code = "ASupprimer",
            DateCreation = DateTime.UtcNow,
            DateModification = DateTime.UtcNow
        });

        // Act — suppression directe (ExecuteDeleteAsync non supporté par InMemory)
        var toDelete = await db.Roles.FindAsync(role.Id);
        db.Roles.Remove(toDelete!);
        await db.SaveChangesAsync();

        // Assert
        var inDb = await db.Roles.FindAsync(role.Id);
        inDb.Should().BeNull();
    }

    [Fact]
    public async Task Delete_ApresSuppressionGetAll_RetourneRolesRestants()
    {
        // Arrange
        using var db = CreateDb(nameof(Delete_ApresSuppressionGetAll_RetourneRolesRestants));
        var repo = CreateRepo(db);

        var r1 = await repo.CreateAsync(new Role { Code = "Role1", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow });
        var r2 = await repo.CreateAsync(new Role { Code = "Role2", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow });

        // Act — supprimer r1
        var toDelete = await db.Roles.FindAsync(r1.Id);
        db.Roles.Remove(toDelete!);
        await db.SaveChangesAsync();

        // Assert — seul r2 reste
        var all = (await repo.GetAllAsync()).ToList();
        all.Should().HaveCount(1);
        all[0].Code.Should().Be("Role2");
    }
}
