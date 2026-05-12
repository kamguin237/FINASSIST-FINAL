using FinAssist.Application.Services;
using FinAssist.Core.Entities;
using FinAssist.Infrastructure.Data;
using FinAssist.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace FinAssist.Tests.Integration;

/// <summary>
/// Tests d'intégration pour PermissionService (Application layer avec cache) +
/// PermissionsRepository + AppDbContext (InMemory).
/// Couvre GetPermissionsEffectivesAsync, HasPermissionAsync, InvalidateCache,
/// et la combinaison permissions rôle + permissions directes.
/// </summary>
public class PermissionServiceIntegrationTests
{
    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    private static (PermissionService service, IMemoryCache cache) CreateService(AppDbContext db)
    {
        var repo = new PermissionsRepository(db);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new PermissionService(repo, cache);
        return (service, cache);
    }

    private static async Task<(Role role, Utilisateur user, Permission perm1, Permission perm2, Permission perm3)>
        SeedAsync(AppDbContext db)
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

        var perm1 = new Permission { Code = "BESOIN_CONSULTER", Module = "Besoins", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow };
        var perm2 = new Permission { Code = "BESOIN_CREER", Module = "Besoins", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow };
        var perm3 = new Permission { Code = "RAPPORT_EXPORTER", Module = "Reporting", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow };
        db.Permissions.AddRange(perm1, perm2, perm3);
        await db.SaveChangesAsync();

        return (role, user, perm1, perm2, perm3);
    }

    // ── GetPermissionsEffectivesAsync ─────────────────────────────────────────

    [Fact]
    public async Task GetPermissionsEffectivesAsync_UtilisateurSansPermissions_RetourneListeVide()
    {
        // Arrange
        using var db = CreateDb(nameof(GetPermissionsEffectivesAsync_UtilisateurSansPermissions_RetourneListeVide));
        var (_, user, _, _, _) = await SeedAsync(db);
        var (service, _) = CreateService(db);

        // Act
        var result = (await service.GetPermissionsEffectivesAsync(user.Id)).ToList();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPermissionsEffectivesAsync_PermissionsRole_RetournePermissionsRole()
    {
        // Arrange
        using var db = CreateDb(nameof(GetPermissionsEffectivesAsync_PermissionsRole_RetournePermissionsRole));
        var (role, user, perm1, perm2, _) = await SeedAsync(db);
        var permsRepo = new PermissionsRepository(db);
        await permsRepo.AssignToRoleAsync(perm1.Id, role.Id);
        await permsRepo.AssignToRoleAsync(perm2.Id, role.Id);
        var (service, _) = CreateService(db);

        // Act
        var result = (await service.GetPermissionsEffectivesAsync(user.Id)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain("BESOIN_CONSULTER");
        result.Should().Contain("BESOIN_CREER");
    }

    [Fact]
    public async Task GetPermissionsEffectivesAsync_PermissionsDirectes_RetournePermissionsDirectes()
    {
        // Arrange
        using var db = CreateDb(nameof(GetPermissionsEffectivesAsync_PermissionsDirectes_RetournePermissionsDirectes));
        var (_, user, _, _, perm3) = await SeedAsync(db);
        var permsRepo = new PermissionsRepository(db);
        await permsRepo.AssignToUtilisateurAsync(perm3.Id, user.Id);
        var (service, _) = CreateService(db);

        // Act
        var result = (await service.GetPermissionsEffectivesAsync(user.Id)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain("RAPPORT_EXPORTER");
    }

    [Fact]
    public async Task GetPermissionsEffectivesAsync_CombineRoleEtDirectes_SansDoublons()
    {
        // Arrange
        using var db = CreateDb(nameof(GetPermissionsEffectivesAsync_CombineRoleEtDirectes_SansDoublons));
        var (role, user, perm1, perm2, perm3) = await SeedAsync(db);
        var permsRepo = new PermissionsRepository(db);

        // perm1 au rôle, perm1 aussi directement (doublon), perm3 directement
        await permsRepo.AssignToRoleAsync(perm1.Id, role.Id);
        await permsRepo.AssignToUtilisateurAsync(perm1.Id, user.Id); // doublon
        await permsRepo.AssignToUtilisateurAsync(perm3.Id, user.Id);

        var (service, _) = CreateService(db);

        // Act
        var result = (await service.GetPermissionsEffectivesAsync(user.Id)).ToList();

        // Assert — perm1 une seule fois + perm3
        result.Should().HaveCount(2);
        result.Should().Contain("BESOIN_CONSULTER");
        result.Should().Contain("RAPPORT_EXPORTER");
    }

    // ── Cache ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPermissionsEffectivesAsync_AppelMultiples_UtiliseLeCache()
    {
        // Arrange
        using var db = CreateDb(nameof(GetPermissionsEffectivesAsync_AppelMultiples_UtiliseLeCache));
        var (role, user, perm1, _, _) = await SeedAsync(db);
        var permsRepo = new PermissionsRepository(db);
        await permsRepo.AssignToRoleAsync(perm1.Id, role.Id);
        var (service, _) = CreateService(db);

        // Act — premier appel (charge le cache)
        var result1 = (await service.GetPermissionsEffectivesAsync(user.Id)).ToList();

        // Modifier la base directement (sans invalider le cache)
        var perm2 = await db.Permissions.FirstAsync(p => p.Code == "BESOIN_CREER");
        db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = perm2.Id });
        await db.SaveChangesAsync();

        // Deuxième appel — doit retourner le résultat caché (sans perm2)
        var result2 = (await service.GetPermissionsEffectivesAsync(user.Id)).ToList();

        // Assert — le cache retourne l'ancien résultat
        result1.Should().HaveCount(1);
        result2.Should().HaveCount(1); // toujours 1 car cache
        result2.Should().NotContain("BESOIN_CREER");
    }

    [Fact]
    public async Task InvalidateCache_ApresInvalidation_RechargeDepuisLaBase()
    {
        // Arrange
        using var db = CreateDb(nameof(InvalidateCache_ApresInvalidation_RechargeDepuisLaBase));
        var (role, user, perm1, perm2, _) = await SeedAsync(db);
        var permsRepo = new PermissionsRepository(db);
        await permsRepo.AssignToRoleAsync(perm1.Id, role.Id);
        var (service, _) = CreateService(db);

        // Premier appel — charge le cache avec perm1 seulement
        var result1 = (await service.GetPermissionsEffectivesAsync(user.Id)).ToList();
        result1.Should().HaveCount(1);

        // Ajouter perm2 en base
        db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = perm2.Id });
        await db.SaveChangesAsync();

        // Invalider le cache
        service.InvalidateCache(user.Id);

        // Act — rechargement depuis la base
        var result2 = (await service.GetPermissionsEffectivesAsync(user.Id)).ToList();

        // Assert — maintenant perm1 + perm2
        result2.Should().HaveCount(2);
        result2.Should().Contain("BESOIN_CONSULTER");
        result2.Should().Contain("BESOIN_CREER");
    }

    // ── HasPermissionAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task HasPermissionAsync_PermissionPresente_RetourneTrue()
    {
        // Arrange
        using var db = CreateDb(nameof(HasPermissionAsync_PermissionPresente_RetourneTrue));
        var (role, user, perm1, _, _) = await SeedAsync(db);
        var permsRepo = new PermissionsRepository(db);
        await permsRepo.AssignToRoleAsync(perm1.Id, role.Id);
        var (service, _) = CreateService(db);

        // Act
        var result = await service.HasPermissionAsync(user.Id, "BESOIN_CONSULTER");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasPermissionAsync_PermissionAbsente_RetourneFalse()
    {
        // Arrange
        using var db = CreateDb(nameof(HasPermissionAsync_PermissionAbsente_RetourneFalse));
        var (_, user, _, _, _) = await SeedAsync(db);
        var (service, _) = CreateService(db);

        // Act
        var result = await service.HasPermissionAsync(user.Id, "PERMISSION_INEXISTANTE");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasPermissionAsync_InsensibleCasse_RetourneTrue()
    {
        // Arrange
        using var db = CreateDb(nameof(HasPermissionAsync_InsensibleCasse_RetourneTrue));
        var (role, user, perm1, _, _) = await SeedAsync(db);
        var permsRepo = new PermissionsRepository(db);
        await permsRepo.AssignToRoleAsync(perm1.Id, role.Id);
        var (service, _) = CreateService(db);

        // Act — recherche en minuscules
        var result = await service.HasPermissionAsync(user.Id, "besoin_consulter");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasPermissionAsync_UtilisateurInexistant_RetourneFalse()
    {
        // Arrange
        using var db = CreateDb(nameof(HasPermissionAsync_UtilisateurInexistant_RetourneFalse));
        var (service, _) = CreateService(db);

        // Act
        var result = await service.HasPermissionAsync(999, "BESOIN_CONSULTER");

        // Assert
        result.Should().BeFalse();
    }

    // ── PermissionsRepository — GetAllAsync, CreateAsync, UpdateAsync ─────────

    [Fact]
    public async Task PermissionsRepository_CreateAsync_PersistEnBase()
    {
        // Arrange
        using var db = CreateDb(nameof(PermissionsRepository_CreateAsync_PersistEnBase));
        var repo = new PermissionsRepository(db);

        // Act
        var result = await repo.CreateAsync(new Permission
        {
            Code = "NOUVELLE_PERMISSION",
            Module = "Test",
            DateCreation = DateTime.UtcNow,
            DateModification = DateTime.UtcNow
        });

        // Assert
        result.Id.Should().BeGreaterThan(0);
        var inDb = await db.Permissions.FindAsync(result.Id);
        inDb.Should().NotBeNull();
        inDb!.Code.Should().Be("NOUVELLE_PERMISSION");
    }

    [Fact]
    public async Task PermissionsRepository_GetAllAsync_RetourneOrdreModuleEtCode()
    {
        // Arrange
        using var db = CreateDb(nameof(PermissionsRepository_GetAllAsync_RetourneOrdreModuleEtCode));
        var repo = new PermissionsRepository(db);

        await repo.CreateAsync(new Permission { Code = "Z_PERM", Module = "Besoins", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow });
        await repo.CreateAsync(new Permission { Code = "A_PERM", Module = "Besoins", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow });
        await repo.CreateAsync(new Permission { Code = "M_PERM", Module = "Admin", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow });

        // Act
        var result = (await repo.GetAllAsync()).ToList();

        // Assert — trié par Module puis Code
        result.Should().HaveCount(3);
        result[0].Module.Should().Be("Admin");   // Admin avant Besoins
        result[1].Code.Should().Be("A_PERM");    // A avant Z dans Besoins
        result[2].Code.Should().Be("Z_PERM");
    }

    [Fact]
    public async Task PermissionsRepository_UpdateAsync_PersisteLaModification()
    {
        // Arrange
        using var db = CreateDb(nameof(PermissionsRepository_UpdateAsync_PersisteLaModification));
        var repo = new PermissionsRepository(db);

        var perm = await repo.CreateAsync(new Permission
        {
            Code = "ANCIEN_CODE",
            Module = "Test",
            DateCreation = DateTime.UtcNow,
            DateModification = DateTime.UtcNow
        });

        // Act
        perm.Code = "NOUVEAU_CODE";
        perm.DateModification = DateTime.UtcNow;
        await repo.UpdateAsync(perm);

        // Assert
        var inDb = await db.Permissions.FindAsync(perm.Id);
        inDb!.Code.Should().Be("NOUVEAU_CODE");
    }

    [Fact]
    public async Task PermissionsRepository_ExistsAsync_InsensibleCasse()
    {
        // Arrange
        using var db = CreateDb(nameof(PermissionsRepository_ExistsAsync_InsensibleCasse));
        var repo = new PermissionsRepository(db);

        await repo.CreateAsync(new Permission
        {
            Code = "BESOIN_CONSULTER",
            Module = "Besoins",
            DateCreation = DateTime.UtcNow,
            DateModification = DateTime.UtcNow
        });

        // Act
        var existsMaj = await repo.ExistsAsync("BESOIN_CONSULTER");
        var existsMin = await repo.ExistsAsync("besoin_consulter");
        var existsNon = await repo.ExistsAsync("INEXISTANT");

        // Assert
        existsMaj.Should().BeTrue();
        existsMin.Should().BeTrue();
        existsNon.Should().BeFalse();
    }
}
