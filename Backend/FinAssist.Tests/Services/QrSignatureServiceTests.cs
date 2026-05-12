using System.Text;
using System.Text.Json;
using FinAssist.Application.Services;
using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;

namespace FinAssist.Tests.Services;

/// <summary>
/// Tests unitaires pour QrSignatureService — génération et vérification de tokens HMAC.
/// </summary>
public class QrSignatureServiceTests
{
    // ── Setup ─────────────────────────────────────────────────────────────────

    private readonly Mock<ISignatureUtilisateurRepository> _repoMock = new();

    private QrSignatureService CreateService(string secret = "finassist_test_secret_key_32chars!")
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Signature:Secret"] = secret,
                ["App:BaseUrl"] = "https://test.finassist.com"
            })
            .Build();

        return new QrSignatureService(_repoMock.Object, config);
    }

    // ── GenerateAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateAsync_RetourneTokenValide()
    {
        // Arrange
        _repoMock.Setup(r => r.SaveAsync(It.IsAny<SignatureUtilisateur>()))
            .ReturnsAsync(new SignatureUtilisateur { Id = 1 });

        var service = CreateService();

        // Act
        var result = await service.GenerateAsync(10, "Dupont", "Jean", "Responsable");

        // Assert
        result.Should().NotBeNull();
        result.Valide.Should().BeTrue();
        result.Nom.Should().Be("Dupont");
        result.Prenom.Should().Be("Jean");
        result.Role.Should().Be("Responsable");
        result.Token.Should().StartWith("FA-");
        result.UrlVerification.Should().StartWith("https://test.finassist.com");
    }

    [Fact]
    public async Task GenerateAsync_TokenFormatCorrect()
    {
        // Arrange — FA-{année}-{initiales}-{hash8}
        _repoMock.Setup(r => r.SaveAsync(It.IsAny<SignatureUtilisateur>()))
            .ReturnsAsync(new SignatureUtilisateur { Id = 1 });

        var service = CreateService();

        // Act
        var result = await service.GenerateAsync(10, "Dupont", "Jean", "Responsable");

        // Assert — format FA-2026-JD-XXXXXXXX
        var parts = result.Token.Split('-');
        parts.Should().HaveCount(4);
        parts[0].Should().Be("FA");
        parts[1].Should().Be(DateTime.UtcNow.Year.ToString());
        parts[2].Should().Be("JD"); // Jean Dupont → JD
        parts[3].Should().HaveLength(8);
    }

    [Fact]
    public async Task GenerateAsync_SauvegardeEnBase()
    {
        // Arrange
        _repoMock.Setup(r => r.SaveAsync(It.IsAny<SignatureUtilisateur>()))
            .ReturnsAsync(new SignatureUtilisateur { Id = 1 });

        var service = CreateService();

        // Act
        await service.GenerateAsync(10, "Dupont", "Jean", "Responsable");

        // Assert
        _repoMock.Verify(r => r.SaveAsync(It.Is<SignatureUtilisateur>(s =>
            s.UtilisateurId == 10 && s.Type == "qrcode")), Times.Once);
    }

    [Fact]
    public async Task GenerateAsync_ExpirationDansUnAn()
    {
        // Arrange
        _repoMock.Setup(r => r.SaveAsync(It.IsAny<SignatureUtilisateur>()))
            .ReturnsAsync(new SignatureUtilisateur { Id = 1 });

        var service = CreateService();

        // Act
        var result = await service.GenerateAsync(10, "Dupont", "Jean", "Responsable");

        // Assert
        result.Expiration.Should().BeCloseTo(DateTime.UtcNow.AddYears(1), TimeSpan.FromMinutes(1));
    }

    // ── VerifyToken ───────────────────────────────────────────────────────────

    [Fact]
    public async Task VerifyToken_TokenValide_RetourneValide()
    {
        // Arrange — générer un vrai token puis le vérifier
        _repoMock.Setup(r => r.SaveAsync(It.IsAny<SignatureUtilisateur>()))
            .ReturnsAsync(new SignatureUtilisateur { Id = 1 });

        var service = CreateService();
        var generated = await service.GenerateAsync(10, "Dupont", "Jean", "Responsable");

        // Extraire le fullToken depuis ce qui a été sauvegardé
        SignatureUtilisateur? savedSig = null;
        _repoMock.Setup(r => r.SaveAsync(It.IsAny<SignatureUtilisateur>()))
            .Callback<SignatureUtilisateur>(s => savedSig = s)
            .ReturnsAsync(new SignatureUtilisateur { Id = 1 });

        await service.GenerateAsync(10, "Dupont", "Jean", "Responsable");

        // Act
        var result = service.VerifyToken(savedSig!.ImageBase64);

        // Assert
        result.Valide.Should().BeTrue();
        result.Statut.Should().Be("valide");
        result.Nom.Should().Be("Dupont");
        result.Prenom.Should().Be("Jean");
        result.Role.Should().Be("Responsable");
    }

    [Fact]
    public void VerifyToken_FormatInvalide_RetourneInvalide()
    {
        // Arrange
        var service = CreateService();

        // Act — token sans le séparateur '.'
        var result = service.VerifyToken("tokenSansPoint");

        // Assert
        result.Valide.Should().BeFalse();
        result.Statut.Should().Be("invalide");
        result.Message.Should().Contain("Format");
    }

    [Fact]
    public void VerifyToken_SignatureAltere_RetourneInvalide()
    {
        // Arrange
        var service = CreateService();

        // Créer un payload valide mais avec une signature falsifiée
        var payload = new { userId = 10, nom = "Dupont", prenom = "Jean", role = "Responsable",
            iat = DateTime.UtcNow.ToString("o"), exp = DateTime.UtcNow.AddYears(1).ToString("o") };
        var payloadB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)));
        var fakeSignatureB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("fausse_signature"));
        var tokenAltere = $"{payloadB64}.{fakeSignatureB64}";

        // Act
        var result = service.VerifyToken(tokenAltere);

        // Assert
        result.Valide.Should().BeFalse();
        result.Statut.Should().Be("invalide");
    }

    [Fact]
    public void VerifyToken_TokenExpire_RetourneExpire()
    {
        // Arrange — créer un token avec expiration dans le passé
        var service = CreateService("finassist_test_secret_key_32chars!");

        var payload = new { userId = 10, nom = "Dupont", prenom = "Jean", role = "Responsable",
            iat = DateTime.UtcNow.AddYears(-2).ToString("o"),
            exp = DateTime.UtcNow.AddYears(-1).ToString("o") }; // expiré il y a 1 an

        var payloadJson = JsonSerializer.Serialize(payload);
        var payloadB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson));

        using var hmac = new System.Security.Cryptography.HMACSHA256(
            Encoding.UTF8.GetBytes("finassist_test_secret_key_32chars!"));
        var sig = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadB64)));
        var sigB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(sig));
        var tokenExpire = $"{payloadB64}.{sigB64}";

        // Act
        var result = service.VerifyToken(tokenExpire);

        // Assert
        result.Valide.Should().BeFalse();
        result.Statut.Should().Be("expire");
    }

    // ── VerifyAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task VerifyAsync_AucuneSignatureEnBase_RetourneInvalide()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByUtilisateurIdAsync(10)).ReturnsAsync((SignatureUtilisateur?)null);
        var service = CreateService();

        // Act
        var result = await service.VerifyAsync(10);

        // Assert
        result.Valide.Should().BeFalse();
        result.Message.Should().Contain("Aucune signature QR");
    }

    [Fact]
    public async Task VerifyAsync_SignatureTypeManuscrite_RetourneInvalide()
    {
        // Arrange — signature de type manuscrite, pas qrcode
        var sig = new SignatureUtilisateur { Id = 1, UtilisateurId = 10, Type = "manuscrite", ImageBase64 = "abc" };
        _repoMock.Setup(r => r.GetByUtilisateurIdAsync(10)).ReturnsAsync(sig);
        var service = CreateService();

        // Act
        var result = await service.VerifyAsync(10);

        // Assert
        result.Valide.Should().BeFalse();
        result.Message.Should().Contain("Aucune signature QR");
    }
}
