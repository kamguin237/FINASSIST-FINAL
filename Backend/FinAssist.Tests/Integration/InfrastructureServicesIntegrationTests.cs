using FinAssist.Infrastructure.Services;
using FluentAssertions;

namespace FinAssist.Tests.Integration;

/// <summary>
/// Tests d'intégration pour les services d'infrastructure purs :
/// - HashingService : ComputeHash, Sign, Verify
/// - Argon2PasswordService : Hash, Verify
/// Ces services n'ont pas de dépendances externes et peuvent être testés directement.
/// </summary>
public class InfrastructureServicesIntegrationTests
{
    // ═══════════════════════════════════════════════════════════════════════════
    // HashingService
    // ═══════════════════════════════════════════════════════════════════════════

    private static HashingService CreateHashingService() => new();

    // ── ComputeHash ───────────────────────────────────────────────────────────

    [Fact]
    public void ComputeHash_MemesDonnees_RetourneMemeHash()
    {
        // Arrange
        var service = CreateHashingService();
        var data = "Contenu du document PDF"u8.ToArray();

        // Act
        var hash1 = service.ComputeHash(data);
        var hash2 = service.ComputeHash(data);

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void ComputeHash_DonneesDifferentes_RetourneHashsDifferents()
    {
        // Arrange
        var service = CreateHashingService();
        var data1 = "Document original"u8.ToArray();
        var data2 = "Document modifié"u8.ToArray();

        // Act
        var hash1 = service.ComputeHash(data1);
        var hash2 = service.ComputeHash(data2);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void ComputeHash_RetourneHashHexadecimalMinuscule()
    {
        // Arrange
        var service = CreateHashingService();
        var data = "test"u8.ToArray();

        // Act
        var hash = service.ComputeHash(data);

        // Assert — SHA256 = 64 caractères hex
        hash.Should().HaveLength(64);
        hash.Should().MatchRegex("^[0-9a-f]+$");
    }

    [Fact]
    public void ComputeHash_TableauVide_RetourneHashValide()
    {
        // Arrange
        var service = CreateHashingService();

        // Act
        var hash = service.ComputeHash([]);

        // Assert — SHA256 de données vides est connu
        hash.Should().Be("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
    }

    // ── Sign ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Sign_MemePayloadEtSecret_RetourneMemeSignature()
    {
        // Arrange
        var service = CreateHashingService();
        const string payload = "empreinte|42|10|2026-04-29T12:00:00Z";
        const string secret = "super_secret_key";

        // Act
        var sig1 = service.Sign(payload, secret);
        var sig2 = service.Sign(payload, secret);

        // Assert
        sig1.Should().Be(sig2);
    }

    [Fact]
    public void Sign_SecretsDifferents_RetourneSignaturesDifferentes()
    {
        // Arrange
        var service = CreateHashingService();
        const string payload = "empreinte|42|10|2026-04-29T12:00:00Z";

        // Act
        var sig1 = service.Sign(payload, "secret1");
        var sig2 = service.Sign(payload, "secret2");

        // Assert
        sig1.Should().NotBe(sig2);
    }

    [Fact]
    public void Sign_RetourneStringBase64Valide()
    {
        // Arrange
        var service = CreateHashingService();

        // Act
        var sig = service.Sign("payload_test", "secret");

        // Assert — doit être décodable en Base64
        var act = () => Convert.FromBase64String(sig);
        act.Should().NotThrow();
    }

    // ── Verify ────────────────────────────────────────────────────────────────

    [Fact]
    public void Verify_SignatureValide_RetourneTrue()
    {
        // Arrange
        var service = CreateHashingService();
        const string payload = "empreinte|42|10|2026-04-29T12:00:00Z";
        const string secret = "super_secret_key";
        var signature = service.Sign(payload, secret);

        // Act
        var result = service.Verify(payload, signature, secret);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Verify_SignatureInvalide_RetourneFalse()
    {
        // Arrange
        var service = CreateHashingService();
        const string payload = "empreinte|42|10|2026-04-29T12:00:00Z";
        const string secret = "super_secret_key";
        var signatureValide = service.Sign(payload, secret);

        // Modifier la signature
        var signatureInvalide = service.Sign("payload_different", secret);

        // Act
        var result = service.Verify(payload, signatureInvalide, secret);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Verify_MauvaisSecret_RetourneFalse()
    {
        // Arrange
        var service = CreateHashingService();
        const string payload = "empreinte|42|10|2026-04-29T12:00:00Z";
        var signature = service.Sign(payload, "bon_secret");

        // Act — vérifier avec un mauvais secret
        var result = service.Verify(payload, signature, "mauvais_secret");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Verify_PayloadModifie_RetourneFalse()
    {
        // Arrange
        var service = CreateHashingService();
        const string secret = "super_secret_key";
        var signature = service.Sign("payload_original", secret);

        // Act — vérifier avec un payload différent
        var result = service.Verify("payload_modifie", signature, secret);

        // Assert
        result.Should().BeFalse();
    }

    // ── Cycle complet Sign → Verify ───────────────────────────────────────────

    [Fact]
    public void SignEtVerify_CycleComplet_Coherent()
    {
        // Arrange
        var service = CreateHashingService();
        var data = "Contenu PDF important"u8.ToArray();
        const string secret = "finassist_secret_2026";

        // Act
        var empreinte = service.ComputeHash(data);
        var payload = $"{empreinte}|100|5|{DateTime.UtcNow:O}";
        var signature = service.Sign(payload, secret);
        var valide = service.Verify(payload, signature, secret);

        // Assert
        valide.Should().BeTrue();
        empreinte.Should().HaveLength(64);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Argon2PasswordService
    // ═══════════════════════════════════════════════════════════════════════════

    private static Argon2PasswordService CreatePasswordService() => new();

    // ── Hash ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Hash_MotDePasseValide_RetourneHashNonVide()
    {
        // Arrange
        var service = CreatePasswordService();

        // Act
        var hash = service.Hash("MonMotDePasse123!");

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().NotBe("MonMotDePasse123!");
    }

    [Fact]
    public void Hash_MemeMotDePasse_RetourneHashsDifferents()
    {
        // Arrange — Argon2 utilise un sel aléatoire
        var service = CreatePasswordService();

        // Act
        var hash1 = service.Hash("MotDePasse");
        var hash2 = service.Hash("MotDePasse");

        // Assert — deux hashes différents grâce au sel
        hash1.Should().NotBe(hash2);
    }

    // ── Verify ────────────────────────────────────────────────────────────────

    [Fact]
    public void Verify_MotDePasseCorrect_RetourneTrue()
    {
        // Arrange
        var service = CreatePasswordService();
        const string password = "MonMotDePasse123!";
        var hash = service.Hash(password);

        // Act
        var result = service.Verify(password, hash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Verify_MotDePasseIncorrect_RetourneFalse()
    {
        // Arrange
        var service = CreatePasswordService();
        var hash = service.Hash("BonMotDePasse");

        // Act
        var result = service.Verify("MauvaisMotDePasse", hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Verify_MotDePasseVideSurHashValide_RetourneFalse()
    {
        // Arrange
        var service = CreatePasswordService();
        var hash = service.Hash("MotDePasseNonVide");

        // Act
        var result = service.Verify("", hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Verify_CycleHashVerify_Coherent()
    {
        // Arrange
        var service = CreatePasswordService();
        const string password = "Finassist@2026!";

        // Act
        var hash = service.Hash(password);
        var correct = service.Verify(password, hash);
        var incorrect = service.Verify("AutreMotDePasse", hash);

        // Assert
        correct.Should().BeTrue();
        incorrect.Should().BeFalse();
    }
}
