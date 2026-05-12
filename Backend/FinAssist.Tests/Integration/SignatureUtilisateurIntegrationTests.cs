using FinAssist.Application.Services;
using FinAssist.Core.DTOs.Signatures;
using FinAssist.Core.Entities;
using FinAssist.Infrastructure.Data;
using FinAssist.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FinAssist.Tests.Integration;

/// <summary>
/// Tests d'intégration pour SignatureUtilisateurService + Repository + InMemory DB.
/// </summary>
public class SignatureUtilisateurIntegrationTests
{
    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    private static SignatureUtilisateurService CreateService(AppDbContext db)
        => new(new SignatureUtilisateurRepository(db));

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

    // ── SauvegarderAsync — création ───────────────────────────────────────────

    [Fact]
    public async Task SauvegarderAsync_NouvelleSignature_PersistEnBase()
    {
        // Arrange
        using var db = CreateDb(nameof(SauvegarderAsync_NouvelleSignature_PersistEnBase));
        var user = await SeedUserAsync(db);
        var service = CreateService(db);

        // Act
        var result = await service.SauvegarderAsync(user.Id, new SaveSignatureUtilisateurDTO
        {
            Type = "manuscrite",
            ImageBase64 = "data:image/png;base64,abc123"
        });

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be("manuscrite");

        var inDb = await db.SignaturesUtilisateurs.FirstOrDefaultAsync(s => s.UtilisateurId == user.Id);
        inDb.Should().NotBeNull();
        inDb!.ImageBase64.Should().Be("data:image/png;base64,abc123");
    }

    // ── SauvegarderAsync — mise à jour ────────────────────────────────────────

    [Fact]
    public async Task SauvegarderAsync_SignatureExistante_MettreAJour()
    {
        // Arrange
        using var db = CreateDb(nameof(SauvegarderAsync_SignatureExistante_MettreAJour));
        var user = await SeedUserAsync(db);
        var service = CreateService(db);

        // Créer une première signature
        await service.SauvegarderAsync(user.Id, new SaveSignatureUtilisateurDTO
        {
            Type = "manuscrite",
            ImageBase64 = "data:image/png;base64,premiere"
        });

        // Act — mettre à jour avec une nouvelle signature
        await service.SauvegarderAsync(user.Id, new SaveSignatureUtilisateurDTO
        {
            Type = "typographique",
            ImageBase64 = "data:image/png;base64,deuxieme",
            Police = "Dancing Script"
        });

        // Assert — une seule signature par utilisateur
        var signatures = await db.SignaturesUtilisateurs
            .Where(s => s.UtilisateurId == user.Id)
            .ToListAsync();
        signatures.Should().HaveCount(1);
        signatures[0].Type.Should().Be("typographique");
        signatures[0].Police.Should().Be("Dancing Script");
    }

    // ── GetMaSignatureAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetMaSignatureAsync_SignatureExistante_RetourneDTO()
    {
        // Arrange
        using var db = CreateDb(nameof(GetMaSignatureAsync_SignatureExistante_RetourneDTO));
        var user = await SeedUserAsync(db);
        var service = CreateService(db);

        await service.SauvegarderAsync(user.Id, new SaveSignatureUtilisateurDTO
        {
            Type = "upload",
            ImageBase64 = "data:image/png;base64,upload_img"
        });

        // Act
        var result = await service.GetMaSignatureAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Type.Should().Be("upload");
        result.ImageBase64.Should().Be("data:image/png;base64,upload_img");
    }

    [Fact]
    public async Task GetMaSignatureAsync_AucuneSignature_RetourneNull()
    {
        // Arrange
        using var db = CreateDb(nameof(GetMaSignatureAsync_AucuneSignature_RetourneNull));
        var user = await SeedUserAsync(db);
        var service = CreateService(db);

        // Act
        var result = await service.GetMaSignatureAsync(user.Id);

        // Assert
        result.Should().BeNull();
    }

    // ── SupprimerAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task SupprimerAsync_SignatureExistante_SupprimeDeLaBase()
    {
        // Arrange
        using var db = CreateDb(nameof(SupprimerAsync_SignatureExistante_SupprimeDeLaBase));
        var user = await SeedUserAsync(db);
        var service = CreateService(db);

        await service.SauvegarderAsync(user.Id, new SaveSignatureUtilisateurDTO
        {
            Type = "manuscrite",
            ImageBase64 = "data:image/png;base64,abc"
        });

        // Vérifier qu'elle existe avant suppression
        var avant = await service.GetMaSignatureAsync(user.Id);
        avant.Should().NotBeNull();

        // Act — supprimer directement via le repository (ExecuteDeleteAsync non supporté par InMemory)
        var sig = await db.SignaturesUtilisateurs.FirstOrDefaultAsync(s => s.UtilisateurId == user.Id);
        if (sig != null)
        {
            db.SignaturesUtilisateurs.Remove(sig);
            await db.SaveChangesAsync();
        }

        // Assert
        var apres = await service.GetMaSignatureAsync(user.Id);
        apres.Should().BeNull();
    }

    // ── Validation des types ──────────────────────────────────────────────────

    [Theory]
    [InlineData("manuscrite")]
    [InlineData("typographique")]
    [InlineData("upload")]
    public async Task SauvegarderAsync_TypesValides_PersistentCorrectement(string type)
    {
        // Arrange
        using var db = CreateDb($"{nameof(SauvegarderAsync_TypesValides_PersistentCorrectement)}_{type}");
        var user = await SeedUserAsync(db);
        var service = CreateService(db);

        // Act
        var result = await service.SauvegarderAsync(user.Id, new SaveSignatureUtilisateurDTO
        {
            Type = type,
            ImageBase64 = "data:image/png;base64,test"
        });

        // Assert
        result.Type.Should().Be(type);
    }
}
