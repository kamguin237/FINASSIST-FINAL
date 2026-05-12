using FinAssist.Application.Services;
using FinAssist.Core.DTOs.Signature;
using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;
using FinAssist.Infrastructure.Data;
using FinAssist.Infrastructure.Repositories;
using FinAssist.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FinAssist.Tests.Integration;

/// <summary>
/// Tests d'intégration pour SignatureService + SignatureRepository + BesoinsRepository + AppDbContext (InMemory).
/// Couvre : SignerParBesoinAsync, SignerAsync, VerifierAsync, GetApercuAsync,
/// GenererDocumentSigneAsync, et tous les cas d'erreur métier.
/// </summary>
public class SignatureServiceIntegrationTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    private static SignatureService CreateService(AppDbContext db)
    {
        var sigRepo    = new SignatureRepository(db);
        var besoinsRepo = new BesoinsRepository(db);
        var hashing    = new HashingService();
        var configMock = new Mock<ISignatureConfig>();
        configMock.Setup(c => c.Secret).Returns("finassist_test_secret_key_32chars!");
        var notifMock  = new Mock<INotificationService>();
        notifMock.Setup(n => n.NotifierSignatureAsync(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        var pdfMock = new Mock<IPdfSignatureService>();
        pdfMock.Setup(p => p.IncrusterSignature(
            It.IsAny<byte[]>(), It.IsAny<string>(),
            It.IsAny<double>(), It.IsAny<double>(),
            It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
            .Returns([0x25, 0x50, 0x44, 0x46]); // %PDF

        return new SignatureService(sigRepo, besoinsRepo, hashing, configMock.Object, notifMock.Object, pdfMock.Object);
    }

    /// <summary>
    /// Crée un besoin avec un document, dans un état APPROUVE_PAR_RESPONSABLE (prêt à signer).
    /// </summary>
    private static async Task<(Utilisateur user, Besoin besoin, Document doc)> SeedBesoinApprouveAsync(
        AppDbContext db,
        bool derniereEtape = true)
    {
        var role = new Role { Code = "Responsable", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow };
        db.Roles.Add(role);

        var user = new Utilisateur
        {
            Nom = "Martin", Prenom = "Paul", Email = "paul@finstar-cm.com",
            MotDePasse = "hash", RoleId = role.Id, Actif = true,
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow
        };
        db.Utilisateurs.Add(user);

        var circuit = new WorkflowCircuit
        {
            Nom = "Circuit Test", NomCreateur = "Admin",
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow,
            Etapes =
            [
                new EtapeCircuit
                {
                    Ordre = 1, RoleRequis = "Responsable",
                    DelaiMaxJours = 60, EstDerniereEtape = derniereEtape,
                    SignatureRequise = true
                }
            ]
        };
        db.WorkflowCircuits.Add(circuit);

        var cat = new Categorie
        {
            Nom = "Informatique", WorkflowCircuitId = circuit.Id,
            DateCreation = DateTime.UtcNow
        };
        db.Categories.Add(cat);
        await db.SaveChangesAsync();

        var besoin = new Besoin
        {
            Titre = "Besoin à signer", Description = "Desc",
            Statut = "APPROUVE_PAR_RESPONSABLE",
            NiveauImportance = "MOYEN",
            UtilisateurId = user.Id, CategorieId = cat.Id,
            EtapeCouranteOrdre = 1,
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow
        };
        db.Besoins.Add(besoin);
        await db.SaveChangesAsync();

        var doc = new Document
        {
            BesoinId = besoin.Id, Nom = "document.pdf",
            Type = "application/pdf",
            Checksum = "abc123",
            Contenu = [0x25, 0x50, 0x44, 0x46, 0x2D], // %PDF-
            DateCreation = DateTime.UtcNow
        };
        db.Documents.Add(doc);
        await db.SaveChangesAsync();

        return (user, besoin, doc);
    }

    // ── SignerParBesoinAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task SignerParBesoinAsync_BesoinApprouve_PersistSignatureEtMettreAJourStatut()
    {
        // Arrange
        using var db = CreateDb(nameof(SignerParBesoinAsync_BesoinApprouve_PersistSignatureEtMettreAJourStatut));
        var (user, besoin, _) = await SeedBesoinApprouveAsync(db);
        var service = CreateService(db);

        // Act
        var result = await service.SignerParBesoinAsync(besoin.Id, user.Id);

        // Assert — signature créée
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Valide.Should().BeTrue();
        result.UtilisateurId.Should().Be(user.Id);
        result.Empreinte.Should().NotBeNullOrEmpty();
        result.Valeur.Should().NotBeNullOrEmpty();

        // Signature persistée en base
        var sigInDb = await db.Signatures.FindAsync(result.Id);
        sigInDb.Should().NotBeNull();
        sigInDb!.Valide.Should().BeTrue();
    }

    [Fact]
    public async Task SignerParBesoinAsync_DerniereEtape_StatutPasseATermine()
    {
        // Arrange
        using var db = CreateDb(nameof(SignerParBesoinAsync_DerniereEtape_StatutPasseATermine));
        var (user, besoin, _) = await SeedBesoinApprouveAsync(db, derniereEtape: true);
        var service = CreateService(db);

        // Act
        await service.SignerParBesoinAsync(besoin.Id, user.Id);

        // Assert — statut TERMINE
        var besoinInDb = await db.Besoins.FindAsync(besoin.Id);
        besoinInDb!.Statut.Should().Be("TERMINE");
    }

    [Fact]
    public async Task SignerParBesoinAsync_AjouteHistoriqueSignature()
    {
        // Arrange
        using var db = CreateDb(nameof(SignerParBesoinAsync_AjouteHistoriqueSignature));
        var (user, besoin, _) = await SeedBesoinApprouveAsync(db);
        var service = CreateService(db);

        // Act
        await service.SignerParBesoinAsync(besoin.Id, user.Id);

        // Assert — historique SIGNATURE créé
        var historique = await db.Historiques
            .FirstOrDefaultAsync(h => h.BesoinId == besoin.Id && h.Action == "SIGNATURE");
        historique.Should().NotBeNull();
        historique!.Description.Should().Contain("document.pdf");
    }

    [Fact]
    public async Task SignerParBesoinAsync_BesoinNonApprouve_LeveInvalidOperation()
    {
        // Arrange
        using var db = CreateDb(nameof(SignerParBesoinAsync_BesoinNonApprouve_LeveInvalidOperation));
        var (user, besoin, _) = await SeedBesoinApprouveAsync(db);
        var service = CreateService(db);

        // Mettre le besoin en BROUILLON (non approuvé)
        besoin.Statut = "BROUILLON";
        db.Besoins.Update(besoin);
        await db.SaveChangesAsync();

        // Act
        var act = async () => await service.SignerParBesoinAsync(besoin.Id, user.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*approuvés*");
    }

    [Fact]
    public async Task SignerParBesoinAsync_SansDocument_LeveInvalidOperation()
    {
        // Arrange
        using var db = CreateDb(nameof(SignerParBesoinAsync_SansDocument_LeveInvalidOperation));
        var (user, besoin, doc) = await SeedBesoinApprouveAsync(db);
        var service = CreateService(db);

        // Supprimer le document
        db.Documents.Remove(doc);
        await db.SaveChangesAsync();

        // Act
        var act = async () => await service.SignerParBesoinAsync(besoin.Id, user.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*pièce jointe*");
    }

    [Fact]
    public async Task SignerParBesoinAsync_DejaSigneParCetUtilisateur_LeveInvalidOperation()
    {
        // Arrange
        using var db = CreateDb(nameof(SignerParBesoinAsync_DejaSigneParCetUtilisateur_LeveInvalidOperation));
        var (user, besoin, _) = await SeedBesoinApprouveAsync(db);
        var service = CreateService(db);

        // Première signature
        await service.SignerParBesoinAsync(besoin.Id, user.Id);

        // Remettre le besoin en APPROUVE pour tenter une deuxième signature
        besoin.Statut = "APPROUVE_PAR_RESPONSABLE";
        db.Besoins.Update(besoin);
        await db.SaveChangesAsync();

        // Act — deuxième tentative
        var act = async () => await service.SignerParBesoinAsync(besoin.Id, user.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*déjà signé*");
    }

    [Fact]
    public async Task SignerParBesoinAsync_AvecSignatureBase64_StockeSignatureManuscrite()
    {
        // Arrange
        using var db = CreateDb(nameof(SignerParBesoinAsync_AvecSignatureBase64_StockeSignatureManuscrite));
        var (user, besoin, _) = await SeedBesoinApprouveAsync(db);
        var service = CreateService(db);

        // Act
        var result = await service.SignerParBesoinAsync(besoin.Id, user.Id, new SignerBesoinDTO
        {
            SignatureBase64 = "data:image/png;base64,iVBORw0KGgo=",
            PositionX = 10.0,
            PositionY = 80.0,
            Largeur = 200,
            Hauteur = 80
        });

        // Assert — signature manuscrite stockée
        var sigInDb = await db.Signatures.FindAsync(result.Id);
        sigInDb!.SignatureBase64.Should().Be("data:image/png;base64,iVBORw0KGgo=");
        sigInDb.PositionX.Should().Be(10.0);
        sigInDb.PositionY.Should().Be(80.0);
    }

    // ── SignerAsync (par documentId) ──────────────────────────────────────────

    [Fact]
    public async Task SignerAsync_DocumentExistant_PersistSignature()
    {
        // Arrange
        using var db = CreateDb(nameof(SignerAsync_DocumentExistant_PersistSignature));
        var (user, _, doc) = await SeedBesoinApprouveAsync(db);
        var service = CreateService(db);

        // Act
        var result = await service.SignerAsync(doc.Id, user.Id);

        // Assert
        result.Should().NotBeNull();
        result.DocumentId.Should().Be(doc.Id);
        result.Valide.Should().BeTrue();
    }

    [Fact]
    public async Task SignerAsync_DocumentInexistant_LeveKeyNotFound()
    {
        // Arrange
        using var db = CreateDb(nameof(SignerAsync_DocumentInexistant_LeveKeyNotFound));
        var service = CreateService(db);

        // Act
        var act = async () => await service.SignerAsync(999, 1);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*999*");
    }

    // ── VerifierAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task VerifierAsync_SignatureIntacte_RetourneAuthentique()
    {
        // Arrange
        using var db = CreateDb(nameof(VerifierAsync_SignatureIntacte_RetourneAuthentique));
        var (user, besoin, _) = await SeedBesoinApprouveAsync(db);
        var service = CreateService(db);

        var sig = await service.SignerParBesoinAsync(besoin.Id, user.Id);

        // Act
        var result = await service.VerifierAsync(sig.Id);

        // Assert
        result.Authentique.Should().BeTrue();
        result.Message.Should().Contain("authentique");
        result.SignataireNom.Should().Contain("Martin");
    }

    [Fact]
    public async Task VerifierAsync_DocumentModifieApresSignature_RetourneNonAuthentique()
    {
        // Arrange
        using var db = CreateDb(nameof(VerifierAsync_DocumentModifieApresSignature_RetourneNonAuthentique));
        var (user, besoin, doc) = await SeedBesoinApprouveAsync(db);
        var service = CreateService(db);

        var sig = await service.SignerParBesoinAsync(besoin.Id, user.Id);

        // Modifier le contenu du document après signature (falsification)
        doc.Contenu = [0xFF, 0xFE, 0x00, 0x01]; // contenu différent
        db.Documents.Update(doc);
        await db.SaveChangesAsync();

        // Act
        var result = await service.VerifierAsync(sig.Id);

        // Assert — document modifié → signature invalide
        result.Authentique.Should().BeFalse();
        result.Message.Should().Contain("modifié");

        // La signature doit être marquée invalide en base
        var sigInDb = await db.Signatures.FindAsync(sig.Id);
        sigInDb!.Valide.Should().BeFalse();
    }

    [Fact]
    public async Task VerifierAsync_SignatureInexistante_LeveKeyNotFound()
    {
        // Arrange
        using var db = CreateDb(nameof(VerifierAsync_SignatureInexistante_LeveKeyNotFound));
        var service = CreateService(db);

        // Act
        var act = async () => await service.VerifierAsync(999);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*999*");
    }

    // ── GetApercuAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetApercuAsync_BesoinSigne_RetourneApercuAvecSignataire()
    {
        // Arrange
        using var db = CreateDb(nameof(GetApercuAsync_BesoinSigne_RetourneApercuAvecSignataire));
        var (user, besoin, _) = await SeedBesoinApprouveAsync(db);
        var service = CreateService(db);

        await service.SignerParBesoinAsync(besoin.Id, user.Id);

        // Act
        var apercu = await service.GetApercuAsync(besoin.Id);

        // Assert
        apercu.Should().NotBeNull();
        apercu.Valide.Should().BeTrue();
        apercu.Empreinte.Should().NotBeNullOrEmpty();
        apercu.Signataire.Nom.Should().Be("Martin");
        apercu.Signataire.Prenom.Should().Be("Paul");
        apercu.Signataire.Role.Should().Be("Responsable");
    }

    [Fact]
    public async Task GetApercuAsync_BesoinNonSigne_LeveKeyNotFound()
    {
        // Arrange
        using var db = CreateDb(nameof(GetApercuAsync_BesoinNonSigne_LeveKeyNotFound));
        var (_, besoin, _) = await SeedBesoinApprouveAsync(db);
        var service = CreateService(db);

        // Act — aucune signature pour ce besoin
        var act = async () => await service.GetApercuAsync(besoin.Id);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{besoin.Id}*");
    }

    // ── GenererDocumentSigneAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GenererDocumentSigneAsync_SansPdfSigne_RetourneDocumentOriginal()
    {
        // Arrange
        using var db = CreateDb(nameof(GenererDocumentSigneAsync_SansPdfSigne_RetourneDocumentOriginal));
        var (user, besoin, _) = await SeedBesoinApprouveAsync(db);
        var service = CreateService(db);

        // Signer sans PdfSigneBase64 ni SignatureBase64
        var sig = await service.SignerParBesoinAsync(besoin.Id, user.Id);

        // Act
        var (pdfBytes, nomFichier) = await service.GenererDocumentSigneAsync(sig.Id);

        // Assert — retourne le document original (pas de PDF pré-signé, pas de SignatureBase64)
        pdfBytes.Should().NotBeEmpty();
        nomFichier.Should().StartWith("document_signe_");
        nomFichier.Should().EndWith(".pdf");
    }

    [Fact]
    public async Task GenererDocumentSigneAsync_AvecPdfSigne_RetournePdfPreSigne()
    {
        // Arrange
        using var db = CreateDb(nameof(GenererDocumentSigneAsync_AvecPdfSigne_RetournePdfPreSigne));
        var (user, besoin, _) = await SeedBesoinApprouveAsync(db);
        var service = CreateService(db);

        // PDF pré-signé côté frontend
        var pdfPreSigne = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 }; // %PDF-1.4
        var sig = await service.SignerParBesoinAsync(besoin.Id, user.Id, new SignerBesoinDTO
        {
            PdfSigneBase64 = Convert.ToBase64String(pdfPreSigne)
        });

        // Act
        var (pdfBytes, nomFichier) = await service.GenererDocumentSigneAsync(sig.Id);

        // Assert — retourne exactement le PDF pré-signé
        pdfBytes.Should().BeEquivalentTo(pdfPreSigne);
        nomFichier.Should().EndWith(".pdf");
    }

    [Fact]
    public async Task GenererDocumentSigneAsync_SignatureInexistante_LeveKeyNotFound()
    {
        // Arrange
        using var db = CreateDb(nameof(GenererDocumentSigneAsync_SignatureInexistante_LeveKeyNotFound));
        var service = CreateService(db);

        // Act
        var act = async () => await service.GenererDocumentSigneAsync(999);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*999*");
    }

    // ── Cycle complet : signer → vérifier → aperçu ───────────────────────────

    [Fact]
    public async Task CycleComplet_Signer_Verifier_Apercu_Coherent()
    {
        // Arrange
        using var db = CreateDb(nameof(CycleComplet_Signer_Verifier_Apercu_Coherent));
        var (user, besoin, _) = await SeedBesoinApprouveAsync(db);
        var service = CreateService(db);

        // Act — cycle complet
        var sig    = await service.SignerParBesoinAsync(besoin.Id, user.Id);
        var verif  = await service.VerifierAsync(sig.Id);
        var apercu = await service.GetApercuAsync(besoin.Id);

        // Assert — cohérence entre les trois opérations
        sig.Valide.Should().BeTrue();
        verif.Authentique.Should().BeTrue();
        apercu.Valide.Should().BeTrue();
        apercu.Empreinte.Should().Be(sig.Empreinte);
        verif.SignataireNom.Should().Contain(apercu.Signataire.Nom);
    }
}
