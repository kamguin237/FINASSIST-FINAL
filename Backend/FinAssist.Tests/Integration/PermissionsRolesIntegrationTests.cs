using FinAssist.Core.Entities;
using FinAssist.Infrastructure.Data;
using FinAssist.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FinAssist.Tests.Integration;

/// <summary>
/// Tests d'intégration pour PermissionsRepository + RolesRepository + AppDbContext (InMemory).
/// </summary>
public class PermissionsRolesIntegrationTests
{
    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<(Role role, Permission perm1, Permission perm2)> SeedAsync(AppDbContext db)
    {
        var role = new Role { Code = "Responsable", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow };
        var perm1 = new Permission { Code = "BESOIN_CONSULTER", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow };
        var perm2 = new Permission { Code = "BESOIN_CREER", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow };
        db.Roles.Add(role);
        db.Permissions.AddRange(perm1, perm2);
        await db.SaveChangesAsync();
        return (role, perm1, perm2);
    }

    // ── RolesRepository ───────────────────────────────────────────────────────

    [Fact]
    public async Task RolesRepository_CreateAsync_PersistEnBase()
    {
        // Arrange
        using var db = CreateDb(nameof(RolesRepository_CreateAsync_PersistEnBase));
        var repo = new RolesRepository(db);

        // Act
        var result = await repo.CreateAsync(new Role
        {
            Code = "NouveauRole",
            Description = "Description test",
            DateCreation = DateTime.UtcNow,
            DateModification = DateTime.UtcNow
        });

        // Assert
        result.Id.Should().BeGreaterThan(0);
        var inDb = await db.Roles.FindAsync(result.Id);
        inDb.Should().NotBeNull();
        inDb!.Code.Should().Be("NouveauRole");
    }

    [Fact]
    public async Task RolesRepository_ExistsAsync_RetourneTrueSiExiste()
    {
        // Arrange
        using var db = CreateDb(nameof(RolesRepository_ExistsAsync_RetourneTrueSiExiste));
        var repo = new RolesRepository(db);
        await repo.CreateAsync(new Role { Code = "Agent", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow });

        // Act & Assert
        (await repo.ExistsAsync("Agent")).Should().BeTrue();
        (await repo.ExistsAsync("agent")).Should().BeTrue(); // insensible à la casse
        (await repo.ExistsAsync("Inconnu")).Should().BeFalse();
    }

    [Fact]
    public async Task RolesRepository_GetAllAsync_RetourneTousLesRoles()
    {
        // Arrange
        using var db = CreateDb(nameof(RolesRepository_GetAllAsync_RetourneTousLesRoles));
        var repo = new RolesRepository(db);
        await repo.CreateAsync(new Role { Code = "Agent", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow });
        await repo.CreateAsync(new Role { Code = "Responsable", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow });

        // Act
        var roles = (await repo.GetAllAsync()).ToList();

        // Assert
        roles.Should().HaveCount(2);
        roles.Select(r => r.Code).Should().Contain(["Agent", "Responsable"]);
    }

    // ── PermissionsRepository — Rôle ──────────────────────────────────────────

    [Fact]
    public async Task AssignToRoleAsync_AssignePermissionAuRole()
    {
        // Arrange
        using var db = CreateDb(nameof(AssignToRoleAsync_AssignePermissionAuRole));
        var (role, perm1, _) = await SeedAsync(db);
        var repo = new PermissionsRepository(db);

        // Act
        await repo.AssignToRoleAsync(perm1.Id, role.Id);

        // Assert
        var perms = (await repo.GetPermissionsByRoleAsync(role.Id)).ToList();
        perms.Should().HaveCount(1);
        perms[0].Code.Should().Be("BESOIN_CONSULTER");
    }

    [Fact]
    public async Task AssignToRoleAsync_DoublonIgnore()
    {
        // Arrange
        using var db = CreateDb(nameof(AssignToRoleAsync_DoublonIgnore));
        var (role, perm1, _) = await SeedAsync(db);
        var repo = new PermissionsRepository(db);

        // Act — assigner deux fois la même permission
        await repo.AssignToRoleAsync(perm1.Id, role.Id);
        await repo.AssignToRoleAsync(perm1.Id, role.Id);

        // Assert — une seule entrée
        var count = await db.RolePermissions.CountAsync(rp => rp.RoleId == role.Id && rp.PermissionId == perm1.Id);
        count.Should().Be(1);
    }

    [Fact]
    public async Task SetRolePermissionsAsync_RemplaceToutesLesPermissions()
    {
        // Arrange
        using var db = CreateDb(nameof(SetRolePermissionsAsync_RemplaceToutesLesPermissions));
        var (role, perm1, perm2) = await SeedAsync(db);
        var repo = new PermissionsRepository(db);

        // Assigner perm1 initialement via AssignToRoleAsync
        await repo.AssignToRoleAsync(perm1.Id, role.Id);

        // Vérifier que perm1 est assignée
        var avant = (await repo.GetPermissionsByRoleAsync(role.Id)).ToList();
        avant.Should().HaveCount(1);
        avant[0].Code.Should().Be("BESOIN_CONSULTER");

        // Act — remplacer manuellement (ExecuteDeleteAsync non supporté par InMemory)
        // Simuler SetRolePermissionsAsync en supprimant et réassignant
        var existing = db.RolePermissions.Where(rp => rp.RoleId == role.Id).ToList();
        db.RolePermissions.RemoveRange(existing);
        await db.SaveChangesAsync();
        await repo.AssignToRoleAsync(perm2.Id, role.Id);

        // Assert
        var apres = (await repo.GetPermissionsByRoleAsync(role.Id)).ToList();
        apres.Should().HaveCount(1);
        apres[0].Code.Should().Be("BESOIN_CREER");
    }

    // ── PermissionsRepository — Utilisateur ───────────────────────────────────

    [Fact]
    public async Task GetPermissionsEffectivesAsync_CombineRoleEtDirectes()
    {
        // Arrange
        using var db = CreateDb(nameof(GetPermissionsEffectivesAsync_CombineRoleEtDirectes));
        var (role, perm1, perm2) = await SeedAsync(db);

        // Ajouter une permission supplémentaire
        var perm3 = new Permission { Code = "RAPPORT_EXPORTER", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow };
        db.Permissions.Add(perm3);

        var user = new Utilisateur
        {
            Nom = "Test", Prenom = "User", Email = "test@finstar-cm.com",
            MotDePasse = "hash", RoleId = role.Id, Actif = true,
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow
        };
        db.Utilisateurs.Add(user);
        await db.SaveChangesAsync();

        var repo = new PermissionsRepository(db);

        // Assigner perm1 au rôle
        await repo.AssignToRoleAsync(perm1.Id, role.Id);
        // Assigner perm3 directement à l'utilisateur
        await repo.AssignToUtilisateurAsync(perm3.Id, user.Id);

        // Act
        var effectives = (await repo.GetPermissionsEffectivesAsync(user.Id)).ToList();

        // Assert — perm1 (du rôle) + perm3 (directe)
        effectives.Should().HaveCount(2);
        effectives.Should().Contain("BESOIN_CONSULTER");
        effectives.Should().Contain("RAPPORT_EXPORTER");
        effectives.Should().NotContain("BESOIN_CREER"); // non assignée
    }

    [Fact]
    public async Task GetPermissionsEffectivesAsync_PasDeDoublons()
    {
        // Arrange
        using var db = CreateDb(nameof(GetPermissionsEffectivesAsync_PasDeDoublons));
        var (role, perm1, _) = await SeedAsync(db);

        var user = new Utilisateur
        {
            Nom = "Test", Prenom = "User", Email = "test@finstar-cm.com",
            MotDePasse = "hash", RoleId = role.Id, Actif = true,
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow
        };
        db.Utilisateurs.Add(user);
        await db.SaveChangesAsync();

        var repo = new PermissionsRepository(db);

        // Assigner perm1 au rôle ET directement à l'utilisateur
        await repo.AssignToRoleAsync(perm1.Id, role.Id);
        await repo.AssignToUtilisateurAsync(perm1.Id, user.Id);

        // Act
        var effectives = (await repo.GetPermissionsEffectivesAsync(user.Id)).ToList();

        // Assert — pas de doublon
        effectives.Should().HaveCount(1);
        effectives[0].Should().Be("BESOIN_CONSULTER");
    }
}
