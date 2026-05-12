using FinAssist.Application.Services;
using FinAssist.Core.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace FinAssist.Tests.Services;

/// <summary>
/// Tests unitaires pour PermissionService — cache et vérification des permissions.
/// </summary>
public class PermissionServiceTests
{
    // ── Setup ─────────────────────────────────────────────────────────────────

    private readonly Mock<IPermissionsRepository> _repoMock = new();

    private (PermissionService service, IMemoryCache cache) CreateService()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new PermissionService(_repoMock.Object, cache);
        return (service, cache);
    }

    // ── GetPermissionsEffectivesAsync ─────────────────────────────────────────

    [Fact]
    public async Task GetPermissionsEffectivesAsync_PremierAppel_InterrogeLaBase()
    {
        // Arrange
        var perms = new List<string> { "BESOIN_CONSULTER", "BESOIN_CREER" };
        _repoMock.Setup(r => r.GetPermissionsEffectivesAsync(1)).ReturnsAsync(perms);
        var (service, _) = CreateService();

        // Act
        var result = await service.GetPermissionsEffectivesAsync(1);

        // Assert
        result.Should().BeEquivalentTo(perms);
        _repoMock.Verify(r => r.GetPermissionsEffectivesAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetPermissionsEffectivesAsync_DeuxiemeAppel_UtiliseLeCache()
    {
        // Arrange
        var perms = new List<string> { "BESOIN_CONSULTER" };
        _repoMock.Setup(r => r.GetPermissionsEffectivesAsync(1)).ReturnsAsync(perms);
        var (service, _) = CreateService();

        // Act — deux appels successifs
        await service.GetPermissionsEffectivesAsync(1);
        await service.GetPermissionsEffectivesAsync(1);

        // Assert — le repo n'est appelé qu'une seule fois (cache hit au 2e appel)
        _repoMock.Verify(r => r.GetPermissionsEffectivesAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetPermissionsEffectivesAsync_UtilisateursDifferents_AppelsDistincts()
    {
        // Arrange
        _repoMock.Setup(r => r.GetPermissionsEffectivesAsync(1)).ReturnsAsync(new List<string> { "PERM_A" });
        _repoMock.Setup(r => r.GetPermissionsEffectivesAsync(2)).ReturnsAsync(new List<string> { "PERM_B" });
        var (service, _) = CreateService();

        // Act
        var perms1 = await service.GetPermissionsEffectivesAsync(1);
        var perms2 = await service.GetPermissionsEffectivesAsync(2);

        // Assert
        perms1.Should().Contain("PERM_A");
        perms2.Should().Contain("PERM_B");
        _repoMock.Verify(r => r.GetPermissionsEffectivesAsync(1), Times.Once);
        _repoMock.Verify(r => r.GetPermissionsEffectivesAsync(2), Times.Once);
    }

    // ── HasPermissionAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task HasPermissionAsync_PermissionPresente_RetourneTrue()
    {
        // Arrange
        _repoMock.Setup(r => r.GetPermissionsEffectivesAsync(1))
            .ReturnsAsync(new List<string> { "BESOIN_CONSULTER", "BESOIN_CREER" });
        var (service, _) = CreateService();

        // Act
        var result = await service.HasPermissionAsync(1, "BESOIN_CONSULTER");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasPermissionAsync_PermissionAbsente_RetourneFalse()
    {
        // Arrange
        _repoMock.Setup(r => r.GetPermissionsEffectivesAsync(1))
            .ReturnsAsync(new List<string> { "BESOIN_CONSULTER" });
        var (service, _) = CreateService();

        // Act
        var result = await service.HasPermissionAsync(1, "RAPPORT_EXPORTER");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasPermissionAsync_ComparaisonInsensibleCasse()
    {
        // Arrange
        _repoMock.Setup(r => r.GetPermissionsEffectivesAsync(1))
            .ReturnsAsync(new List<string> { "BESOIN_CONSULTER" });
        var (service, _) = CreateService();

        // Act — minuscule vs majuscule
        var result = await service.HasPermissionAsync(1, "besoin_consulter");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasPermissionAsync_AucunePermission_RetourneFalse()
    {
        // Arrange
        _repoMock.Setup(r => r.GetPermissionsEffectivesAsync(1))
            .ReturnsAsync(new List<string>());
        var (service, _) = CreateService();

        // Act
        var result = await service.HasPermissionAsync(1, "BESOIN_CONSULTER");

        // Assert
        result.Should().BeFalse();
    }

    // ── InvalidateCache ───────────────────────────────────────────────────────

    [Fact]
    public async Task InvalidateCache_ForceNouvelAppelBase()
    {
        // Arrange
        _repoMock.Setup(r => r.GetPermissionsEffectivesAsync(1))
            .ReturnsAsync(new List<string> { "BESOIN_CONSULTER" });
        var (service, _) = CreateService();

        // Act — premier appel (mise en cache), invalidation, deuxième appel
        await service.GetPermissionsEffectivesAsync(1);
        service.InvalidateCache(1);
        await service.GetPermissionsEffectivesAsync(1);

        // Assert — le repo est appelé deux fois (cache invalidé entre les deux)
        _repoMock.Verify(r => r.GetPermissionsEffectivesAsync(1), Times.Exactly(2));
    }

    [Fact]
    public async Task InvalidateCache_AutreUtilisateur_NaffectePas()
    {
        // Arrange
        _repoMock.Setup(r => r.GetPermissionsEffectivesAsync(1)).ReturnsAsync(new List<string> { "PERM_A" });
        _repoMock.Setup(r => r.GetPermissionsEffectivesAsync(2)).ReturnsAsync(new List<string> { "PERM_B" });
        var (service, _) = CreateService();

        // Act — charger les deux, invalider userId=1, recharger userId=2
        await service.GetPermissionsEffectivesAsync(1);
        await service.GetPermissionsEffectivesAsync(2);
        service.InvalidateCache(1); // invalide seulement userId=1
        await service.GetPermissionsEffectivesAsync(2); // doit utiliser le cache

        // Assert — userId=2 n'est appelé qu'une seule fois (cache intact)
        _repoMock.Verify(r => r.GetPermissionsEffectivesAsync(2), Times.Once);
    }
}
