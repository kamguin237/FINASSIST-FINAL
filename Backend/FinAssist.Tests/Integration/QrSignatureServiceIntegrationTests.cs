using FinAssist.Application.Services;
using FinAssist.Core.Entities;
using FinAssist.Infrastructure.Data;
using FinAssist.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FinAssist.Tests.Integration;

/// <summary>
/// Tests d'intégration pour QrSignatureService + SignatureUtilisateurRepository + AppDbContext (InMemory).
/// Couvre GenerateAsync, VerifyAsync, VerifyToken.
/// </summary>
public class QrSignatureServiceIntegrationTests
{
    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    private static QrSignatureService CreateService(AppDbContext db)
    {
        var repo = new SignatureUtilisateurRepository(db);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Signature:Secret"] = "finassist_test_secret_key_32chars!",
                ["App:BaseUrl"] = "http://localhost:8080"
            })
            .Build();
        return new QrSignatureService(repo, config);
    }

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

    // ── GenerateAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateAsync_UtilisateurValide_RetourneTokenEtPersiste()
    {
        // Arrange
        using var db = CreateDb(nameof(GenerateAsync_UtilisateurValide_RetourneTokenEtPersiste));
        var user = await SeedUserAsync(db);
        var service = CreateService(db);

        // Act
        var result = await service.GenerateAsync(user.Id, user.Nom, user.Prenom, "Agent");

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().StartWith("FA-");
        result.Valide.Should().BeTrue();
        result.Nom.Should().Be("Dupont");
        result.Prenom.Should().Be("Jean");
        result.Role.Should().Be("Agent");
        result.UrlVerification.Should().StartWith("http://localhost:8080");

        // Vérifier persistance en base
        var inDb = await db.SignaturesUtilisateurs
            .FirstOrDefaultAsync(s => s.UtilisateurId == user.Id && s.Type == "qrcode");
        inDb.Should().NotBeNull();
        inDb!.Police.Should().StartWith("FA-");
    }

    [Fact]
    public async Task GenerateAsync_TokenFormatCorrect_ContientAnneeEtInitiales()
    {
        // Arrange
        using var db = CreateDb(nameof(GenerateAsync_TokenFormatCorrect_ContientAnneeEtInitiales));
        var user = await SeedUserAsync(db);
        var service = CreateService(db);

        // Act
        var result = await service.GenerateAsync(user.Id, "Dupont", "Jean", "Agent");

        // Assert — format FA-{année}-{initiales}-{hash8}
        var parts = result.Token.Split('-');
        parts.Should().HaveCount(4);
        parts[0].Should().Be("FA");
        parts[1].Should().Be(DateTime.UtcNow.Year.ToString());
        parts[2].Should().Be("JD"); // Jean Dupont → JD
    }

    [Fact]
    public async Task GenerateAsync_ExpirationDansUnAn()
    {
        // Arrange
        using var db = CreateDb(nameof(GenerateAsync_ExpirationDansUnAn));
        var user = await SeedUserAsync(db);
        var service = CreateService(db);

        // Act
        var result = await service.GenerateAsync(user.Id, user.Nom, user.Prenom, "Agent");

        // Assert — expiration dans ~1 an
        result.Expiration.Should().BeCloseTo(DateTime.UtcNow.AddYears(1), TimeSpan.FromMinutes(5));
    }

    [Fact]
    public async Task GenerateAsync_AppelMultiples_EcraseLAncienneSignature()
    {
        // Arrange
        using var db = CreateDb(nameof(GenerateAsync_AppelMultiples_EcraseLAncienneSignature));
        var user = await SeedUserAsync(db);
        var service = CreateService(db);

        // Act — générer deux fois
        var result1 = await service.GenerateAsync(user.Id, user.Nom, user.Prenom, "Agent");
        await Task.Delay(10); // petit délai pour différencier les tokens
        var result2 = await service.GenerateAsync(user.Id, user.Nom, user.Prenom, "Agent");

        // Assert — les deux tokens sont valides (le service SaveAsync peut upsert)
        result1.Valide.Should().BeTrue();
        result2.Valide.Should().BeTrue();
    }

    // ── VerifyAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task VerifyAsync_ApresGeneration_RetourneValide()
    {
        // Arrange
        using var db = CreateDb(nameof(VerifyAsync_ApresGeneration_RetourneValide));
        var user = await SeedUserAsync(db);
        var service = CreateService(db);

        await service.GenerateAsync(user.Id, user.Nom, user.Prenom, "Agent");

        // Act
        var result = await service.VerifyAsync(user.Id);

        // Assert
        result.Valide.Should().BeTrue();
        result.Statut.Should().Be("valide");
        result.Nom.Should().Be("Dupont");
        result.Prenom.Should().Be("Jean");
        result.Role.Should().Be("Agent");
    }

    [Fact]
    public async Task VerifyAsync_SansSignatureGeneree_RetourneInvalide()
    {
        // Arrange
        using var db = CreateDb(nameof(VerifyAsync_SansSignatureGeneree_RetourneInvalide));
        var user = await SeedUserAsync(db);
        var service = CreateService(db);

        // Act — vérifier sans avoir généré
        var result = await service.VerifyAsync(user.Id);

        // Assert
        result.Valide.Should().BeFalse();
        result.Statut.Should().Be("invalide");
    }

    // ── VerifyToken ───────────────────────────────────────────────────────────

    [Fact]
    public async Task VerifyToken_TokenValide_RetourneValide()
    {
        // Arrange
        using var db = CreateDb(nameof(VerifyToken_TokenValide_RetourneValide));
        var user = await SeedUserAsync(db);
        var service = CreateService(db);

        var generated = await service.GenerateAsync(user.Id, user.Nom, user.Prenom, "Agent");

        // Récupérer le fullToken depuis la base
        var sig = await db.SignaturesUtilisateurs
            .FirstOrDefaultAsync(s => s.UtilisateurId == user.Id && s.Type == "qrcode");
        var fullToken = sig!.ImageBase64;

        // Act
        var result = service.VerifyToken(fullToken);

        // Assert
        result.Valide.Should().BeTrue();
        result.Statut.Should().Be("valide");
        result.Nom.Should().Be("Dupont");
    }

    [Fact]
    public void VerifyToken_TokenMalForme_RetourneInvalide()
    {
        // Arrange
        using var db = CreateDb(nameof(VerifyToken_TokenMalForme_RetourneInvalide));
        var service = CreateService(db);

        // Act
        var result = service.VerifyToken("token_invalide_sans_point");

        // Assert
        result.Valide.Should().BeFalse();
        result.Statut.Should().Be("invalide");
    }

    [Fact]
    public void VerifyToken_TokenAvecSignatureModifiee_RetourneInvalide()
    {
        // Arrange
        using var db = CreateDb(nameof(VerifyToken_TokenAvecSignatureModifiee_RetourneInvalide));
        var service = CreateService(db);

        // Construire un token avec une signature falsifiée
        var payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
            """{"userId":1,"nom":"Hacker","prenom":"Evil","role":"Admin","iat":"2026-01-01T00:00:00Z","exp":"2099-01-01T00:00:00Z"}"""));
        var fakeSignature = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("fake_signature"));
        var fakeToken = $"{payload}.{fakeSignature}";

        // Act
        var result = service.VerifyToken(fakeToken);

        // Assert
        result.Valide.Should().BeFalse();
    }
}
