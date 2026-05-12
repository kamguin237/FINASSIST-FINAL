using FinAssist.Core.Entities;
using FinAssist.Infrastructure.Data;
using FinAssist.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FinAssist.Tests.Integration;

/// <summary>
/// Tests d'intégration pour SignatureRepository + AppDbContext (InMemory).
/// </summary>
public class SignatureRepositoryIntegrationTests
{
    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    private static SignatureRepository CreateRepo(AppDbContext db)
        => new(db);

    private static async Task<(Utilisateur user, Document doc)> SeedAsync(AppDbContext db)
    {
        var role = new Role { Code = "Responsable", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow };
        db.Roles.Add(role);
        var user = new Utilisateur
        {
            Nom = "Dupont", Prenom = "Jean", Email = "jean@finstar-cm.com",
            MotDePasse = "hash", RoleId = role.Id, Actif = true,
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow
        };
        db.Utilisateurs.Add(user);
        var cat = new Categorie { Nom = "Informatique", DateCreation = DateTime.UtcNow };
        db.Categories.Add(cat);
        await db.SaveChangesAsync();

        var besoin = new Besoin
        {
            Titre = "Besoin test", Description = "Desc", Statut = "EN_ATTENTE_RESPONSABLE",
            NiveauImportance = "MOYEN", UtilisateurId = user.Id, CategorieId = cat.Id,
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow
        };
        db.Besoins.Add(besoin);
        await db.SaveChangesAsync();

        var doc = new Document
        {
            BesoinId = besoin.Id, Nom = "document.pdf", Type = "application/pdf",
            Checksum = "abc123", Contenu = [0x25, 0x50, 0x44, 0x46],
            DateCreation = DateTime.UtcNow
        };
        db.Documents.Add(doc);
        await db.SaveChangesAsync();

        return (user, doc);
    }

    // ── AddAsync ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_SignatureValide_PersistEnBase()
    {
        // Arrange
        using var db = CreateDb(nameof(AddAsync_SignatureValide_PersistEnBase));
        var (user, doc) = await SeedAsync(db);
        var repo = CreateRepo(db);

        var sig = new SignatureElectronique
        {
            DocumentId = doc.Id,
            UtilisateurId = user.Id,
            Valeur = "signature_base64_value",
            Empreinte = "sha256_hash_abc",
            Horodatage = DateTime.UtcNow,
            Valide = true
        };

        // Act
        var result = await repo.AddAsync(sig);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Valide.Should().BeTrue();

        var inDb = await db.Signatures.FindAsync(result.Id);
        inDb.Should().NotBeNull();
        inDb!.Empreinte.Should().Be("sha256_hash_abc");
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_SignatureExistante_RetourneAvecRelations()
    {
        // Arrange
        using var db = CreateDb(nameof(GetByIdAsync_SignatureExistante_RetourneAvecRelations));
        var (user, doc) = await SeedAsync(db);
        var repo = CreateRepo(db);

        var sig = await repo.AddAsync(new SignatureElectronique
        {
            DocumentId = doc.Id, UtilisateurId = user.Id,
            Valeur = "val", Empreinte = "hash",
            Horodatage = DateTime.UtcNow, Valide = true
        });

        // Act
        var result = await repo.GetByIdAsync(sig.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Utilisateur.Should().NotBeNull();
        result.Utilisateur.Nom.Should().Be("Dupont");
        result.Document.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_SignatureInexistante_RetourneNull()
    {
        // Arrange
        using var db = CreateDb(nameof(GetByIdAsync_SignatureInexistante_RetourneNull));
        var repo = CreateRepo(db);

        // Act
        var result = await repo.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    // ── GetByDocumentIdAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetByDocumentIdAsync_DocumentSigne_RetourneSignature()
    {
        // Arrange
        using var db = CreateDb(nameof(GetByDocumentIdAsync_DocumentSigne_RetourneSignature));
        var (user, doc) = await SeedAsync(db);
        var repo = CreateRepo(db);

        await repo.AddAsync(new SignatureElectronique
        {
            DocumentId = doc.Id, UtilisateurId = user.Id,
            Valeur = "val", Empreinte = "hash",
            Horodatage = DateTime.UtcNow, Valide = true
        });

        // Act
        var result = await repo.GetByDocumentIdAsync(doc.Id);

        // Assert
        result.Should().NotBeNull();
        result!.DocumentId.Should().Be(doc.Id);
    }

    [Fact]
    public async Task GetByDocumentIdAsync_DocumentNonSigne_RetourneNull()
    {
        // Arrange
        using var db = CreateDb(nameof(GetByDocumentIdAsync_DocumentNonSigne_RetourneNull));
        var (_, doc) = await SeedAsync(db);
        var repo = CreateRepo(db);

        // Act
        var result = await repo.GetByDocumentIdAsync(doc.Id);

        // Assert
        result.Should().BeNull();
    }

    // ── GetByDocumentAndUtilisateurAsync ──────────────────────────────────────

    [Fact]
    public async Task GetByDocumentAndUtilisateurAsync_SignatureExistante_RetourneSignature()
    {
        // Arrange
        using var db = CreateDb(nameof(GetByDocumentAndUtilisateurAsync_SignatureExistante_RetourneSignature));
        var (user, doc) = await SeedAsync(db);
        var repo = CreateRepo(db);

        await repo.AddAsync(new SignatureElectronique
        {
            DocumentId = doc.Id, UtilisateurId = user.Id,
            Valeur = "val", Empreinte = "hash",
            Horodatage = DateTime.UtcNow, Valide = true
        });

        // Act
        var result = await repo.GetByDocumentAndUtilisateurAsync(doc.Id, user.Id);

        // Assert
        result.Should().NotBeNull();
        result!.UtilisateurId.Should().Be(user.Id);
        result.DocumentId.Should().Be(doc.Id);
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ModifieValide_PersisteLaModification()
    {
        // Arrange
        using var db = CreateDb(nameof(UpdateAsync_ModifieValide_PersisteLaModification));
        var (user, doc) = await SeedAsync(db);
        var repo = CreateRepo(db);

        var sig = await repo.AddAsync(new SignatureElectronique
        {
            DocumentId = doc.Id, UtilisateurId = user.Id,
            Valeur = "val", Empreinte = "hash",
            Horodatage = DateTime.UtcNow, Valide = true
        });

        // Act
        sig.Valide = false;
        await repo.UpdateAsync(sig);

        // Assert
        var inDb = await db.Signatures.FindAsync(sig.Id);
        inDb!.Valide.Should().BeFalse();
    }

    // ── GetByBesoinIdAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetByBesoinIdAsync_BesoinAvecSignatureValide_RetourneDerniereSignature()
    {
        // Arrange
        using var db = CreateDb(nameof(GetByBesoinIdAsync_BesoinAvecSignatureValide_RetourneDerniereSignature));
        var (user, doc) = await SeedAsync(db);
        var repo = CreateRepo(db);

        // Ajouter deux signatures (la plus récente doit être retournée)
        await repo.AddAsync(new SignatureElectronique
        {
            DocumentId = doc.Id, UtilisateurId = user.Id,
            Valeur = "val1", Empreinte = "hash1",
            Horodatage = DateTime.UtcNow.AddMinutes(-10), Valide = true
        });
        await repo.AddAsync(new SignatureElectronique
        {
            DocumentId = doc.Id, UtilisateurId = user.Id,
            Valeur = "val2", Empreinte = "hash2",
            Horodatage = DateTime.UtcNow, Valide = true
        });

        // Act
        var result = await repo.GetByBesoinIdAsync(doc.BesoinId);

        // Assert
        result.Should().NotBeNull();
        result!.Empreinte.Should().Be("hash2"); // la plus récente
    }
}
