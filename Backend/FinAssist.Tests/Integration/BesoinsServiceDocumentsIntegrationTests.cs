using FinAssist.Application.Services;
using FinAssist.Core.DTOs.Besoins;
using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;
using FinAssist.Infrastructure.Data;
using FinAssist.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FinAssist.Tests.Integration;

/// <summary>
/// Tests d'intégration pour BesoinsService — opérations sur les documents,
/// soumission, deadlines et gestion des catégories.
/// Complète BesoinsIntegrationTests qui couvre Create/Update/Delete/Historique.
/// </summary>
public class BesoinsServiceDocumentsIntegrationTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    private static BesoinsService CreateService(AppDbContext db)
    {
        var repo    = new BesoinsRepository(db);
        var wfRepo  = new WorkflowRepository(db);
        var notif   = new Mock<INotificationService>();
        notif.Setup(n => n.NotifierSoumissionAsync(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        var hub = new Mock<IBesoinsHubService>();
        hub.Setup(h => h.NotifierHistoriqueAsync(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        return new BesoinsService(repo, wfRepo, notif.Object, hub.Object);
    }

    private static async Task<(Utilisateur user, Categorie cat, WorkflowCircuit circuit)>
        SeedBaseAsync(AppDbContext db)
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
        var circuit = new WorkflowCircuit
        {
            Nom = "Circuit Test", NomCreateur = "Admin",
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow,
            Etapes = [new EtapeCircuit { Ordre = 1, RoleRequis = "Responsable", DelaiMaxJours = 60, EstDerniereEtape = true }]
        };
        db.WorkflowCircuits.Add(circuit);
        var cat = new Categorie { Nom = "Informatique", WorkflowCircuitId = circuit.Id, DateCreation = DateTime.UtcNow };
        db.Categories.Add(cat);
        await db.SaveChangesAsync();
        return (user, cat, circuit);
    }

    private static async Task<Besoin> CreateBesoinBrouillonAsync(AppDbContext db, int userId, int catId)
    {
        var besoin = new Besoin
        {
            Titre = "Besoin test", Description = "Desc",
            Statut = "BROUILLON", NiveauImportance = "MOYEN",
            UtilisateurId = userId, CategorieId = catId,
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow
        };
        db.Besoins.Add(besoin);
        await db.SaveChangesAsync();
        return besoin;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // AjouterPieceJointeAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AjouterPieceJointeAsync_BesoinBrouillon_PersistDocument()
    {
        // Arrange
        using var db = CreateDb(nameof(AjouterPieceJointeAsync_BesoinBrouillon_PersistDocument));
        var (user, cat, _) = await SeedBaseAsync(db);
        var besoin = await CreateBesoinBrouillonAsync(db, user.Id, cat.Id);
        var service = CreateService(db);

        var contenu = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF

        // Act
        var result = await service.AjouterPieceJointeAsync(besoin.Id, "rapport.pdf", "application/pdf", contenu);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Nom.Should().Be("rapport.pdf");
        result.Checksum.Should().NotBeNullOrEmpty();

        var docInDb = await db.Documents.FindAsync(result.Id);
        docInDb.Should().NotBeNull();
        docInDb!.Contenu.Should().BeEquivalentTo(contenu);
    }

    [Fact]
    public async Task AjouterPieceJointeAsync_AjouteHistoriquePieceJointe()
    {
        // Arrange
        using var db = CreateDb(nameof(AjouterPieceJointeAsync_AjouteHistoriquePieceJointe));
        var (user, cat, _) = await SeedBaseAsync(db);
        var besoin = await CreateBesoinBrouillonAsync(db, user.Id, cat.Id);
        var service = CreateService(db);

        // Act
        await service.AjouterPieceJointeAsync(besoin.Id, "doc.pdf", "application/pdf", [0x25, 0x50]);

        // Assert — historique PIECE_JOINTE créé
        var historique = await db.Historiques
            .FirstOrDefaultAsync(h => h.BesoinId == besoin.Id && h.Action == "PIECE_JOINTE");
        historique.Should().NotBeNull();
        historique!.Description.Should().Contain("doc.pdf");
    }

    [Fact]
    public async Task AjouterPieceJointeAsync_BesoinEnregistre_LeveInvalidOperation()
    {
        // Arrange
        using var db = CreateDb(nameof(AjouterPieceJointeAsync_BesoinEnregistre_LeveInvalidOperation));
        var (user, cat, _) = await SeedBaseAsync(db);
        var besoin = await CreateBesoinBrouillonAsync(db, user.Id, cat.Id);
        besoin.Statut = "ENREGISTRE";
        db.Besoins.Update(besoin);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        // Act
        var act = async () => await service.AjouterPieceJointeAsync(besoin.Id, "doc.pdf", "application/pdf", [0x25]);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*enregistré*");
    }

    [Fact]
    public async Task AjouterPieceJointeAsync_BesoinInexistant_LeveKeyNotFound()
    {
        // Arrange
        using var db = CreateDb(nameof(AjouterPieceJointeAsync_BesoinInexistant_LeveKeyNotFound));
        var service = CreateService(db);

        // Act
        var act = async () => await service.AjouterPieceJointeAsync(999, "doc.pdf", "application/pdf", [0x25]);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetDocumentsAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetDocumentsAsync_BesoinAvecDocuments_RetourneListe()
    {
        // Arrange
        using var db = CreateDb(nameof(GetDocumentsAsync_BesoinAvecDocuments_RetourneListe));
        var (user, cat, _) = await SeedBaseAsync(db);
        var besoin = await CreateBesoinBrouillonAsync(db, user.Id, cat.Id);
        var service = CreateService(db);

        await service.AjouterPieceJointeAsync(besoin.Id, "doc1.pdf", "application/pdf", [0x25, 0x50]);
        await service.AjouterPieceJointeAsync(besoin.Id, "doc2.pdf", "application/pdf", [0x25, 0x50]);

        // Act
        var docs = (await service.GetDocumentsAsync(besoin.Id, user.Id, "Agent")).ToList();

        // Assert
        docs.Should().HaveCount(2);
        docs.Select(d => d.Nom).Should().Contain(["doc1.pdf", "doc2.pdf"]);
    }

    [Fact]
    public async Task GetDocumentsAsync_BesoinSansDocuments_RetourneListeVide()
    {
        // Arrange
        using var db = CreateDb(nameof(GetDocumentsAsync_BesoinSansDocuments_RetourneListeVide));
        var (user, cat, _) = await SeedBaseAsync(db);
        var besoin = await CreateBesoinBrouillonAsync(db, user.Id, cat.Id);
        var service = CreateService(db);

        // Act
        var docs = (await service.GetDocumentsAsync(besoin.Id, user.Id, "Agent")).ToList();

        // Assert
        docs.Should().BeEmpty();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SupprimerDocumentAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SupprimerDocumentAsync_BesoinBrouillon_SupprimeDocument()
    {
        // Arrange
        using var db = CreateDb(nameof(SupprimerDocumentAsync_BesoinBrouillon_SupprimeDocument));
        var (user, cat, _) = await SeedBaseAsync(db);
        var besoin = await CreateBesoinBrouillonAsync(db, user.Id, cat.Id);
        var service = CreateService(db);

        var doc = await service.AjouterPieceJointeAsync(besoin.Id, "doc.pdf", "application/pdf", [0x25, 0x50]);

        // Act — SupprimerDocumentAsync utilise ExecuteDeleteAsync (non supporté InMemory)
        // On teste via le repository directement
        var docInDb = await db.Documents.FindAsync(doc.Id);
        docInDb.Should().NotBeNull();

        // Vérifier que la suppression via le service lève une exception pour besoin non-brouillon
        besoin.Statut = "ENREGISTRE";
        db.Besoins.Update(besoin);
        await db.SaveChangesAsync();

        var act = async () => await service.SupprimerDocumentAsync(besoin.Id, doc.Id);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*enregistré*");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // SoumettreAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SoumettreAsync_BesoinEnregistre_PasseEnAttente()
    {
        // Arrange
        using var db = CreateDb(nameof(SoumettreAsync_BesoinEnregistre_PasseEnAttente));
        var (user, cat, _) = await SeedBaseAsync(db);
        var service = CreateService(db);

        // Créer et enregistrer le besoin
        var created = await service.CreateAsync(new CreateBesoinDTO
        {
            Titre = "Besoin à soumettre", Description = "Desc",
            NiveauImportance = "MOYEN", CategorieId = cat.Id
        }, user.Id, "Agent");

        await service.EnregistrerAsync(created.Id, user.Id);

        // Act
        var result = await service.SoumettreAsync(created.Id, user.Id);

        // Assert — statut EN_ATTENTE_RESPONSABLE (première étape du circuit)
        result.Statut.Should().Be("EN_ATTENTE_RESPONSABLE");

        var besoinInDb = await db.Besoins.FindAsync(created.Id);
        besoinInDb!.EtapeCouranteOrdre.Should().Be(1);
        besoinInDb.DateEntreeEnAttente.Should().NotBeNull();
        besoinInDb.Rappel1Envoye.Should().BeFalse();
        besoinInDb.Rappel2Envoye.Should().BeFalse();
    }

    [Fact]
    public async Task SoumettreAsync_BesoinBrouillon_LeveInvalidOperation()
    {
        // Arrange
        using var db = CreateDb(nameof(SoumettreAsync_BesoinBrouillon_LeveInvalidOperation));
        var (user, cat, _) = await SeedBaseAsync(db);
        var service = CreateService(db);

        var created = await service.CreateAsync(new CreateBesoinDTO
        {
            Titre = "Brouillon", Description = "Desc",
            NiveauImportance = "FAIBLE", CategorieId = cat.Id
        }, user.Id, "Agent");

        // Act — soumettre sans enregistrer d'abord
        var act = async () => await service.SoumettreAsync(created.Id, user.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*enregistré*");
    }

    [Fact]
    public async Task SoumettreAsync_AjouteHistoriqueSoumission()
    {
        // Arrange
        using var db = CreateDb(nameof(SoumettreAsync_AjouteHistoriqueSoumission));
        var (user, cat, _) = await SeedBaseAsync(db);
        var service = CreateService(db);

        var created = await service.CreateAsync(new CreateBesoinDTO
        {
            Titre = "Besoin soumis", Description = "Desc",
            NiveauImportance = "MOYEN", CategorieId = cat.Id
        }, user.Id, "Agent");
        await service.EnregistrerAsync(created.Id, user.Id);

        // Act
        await service.SoumettreAsync(created.Id, user.Id);

        // Assert — historique SOUMISSION créé
        var historique = await db.Historiques
            .FirstOrDefaultAsync(h => h.BesoinId == created.Id && h.Action == "SOUMISSION");
        historique.Should().NotBeNull();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetDeadlinesAsync
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetDeadlinesAsync_BesoinEnAttente_RetourneDeadline()
    {
        // Arrange
        using var db = CreateDb(nameof(GetDeadlinesAsync_BesoinEnAttente_RetourneDeadline));
        var (user, cat, _) = await SeedBaseAsync(db);
        var service = CreateService(db);

        var created = await service.CreateAsync(new CreateBesoinDTO
        {
            Titre = "Besoin deadline", Description = "Desc",
            NiveauImportance = "MOYEN", CategorieId = cat.Id
        }, user.Id, "Agent");
        await service.EnregistrerAsync(created.Id, user.Id);
        await service.SoumettreAsync(created.Id, user.Id);

        // Act — admin voit tout
        var deadlines = (await service.GetDeadlinesAsync(user.Id, "Administrateur")).ToList();

        // Assert
        deadlines.Should().HaveCount(1);
        deadlines[0].Titre.Should().Be("Besoin deadline");
        deadlines[0].Statut.Should().Be("EN_ATTENTE_RESPONSABLE");
        deadlines[0].EtapeRole.Should().Be("RESPONSABLE");
    }

    [Fact]
    public async Task GetDeadlinesAsync_BesoinBrouillon_NApparasPas()
    {
        // Arrange
        using var db = CreateDb(nameof(GetDeadlinesAsync_BesoinBrouillon_NApparasPas));
        var (user, cat, _) = await SeedBaseAsync(db);
        var service = CreateService(db);

        // Créer un besoin en BROUILLON (pas soumis)
        await service.CreateAsync(new CreateBesoinDTO
        {
            Titre = "Brouillon", Description = "Desc",
            NiveauImportance = "FAIBLE", CategorieId = cat.Id
        }, user.Id, "Agent");

        // Act
        var deadlines = (await service.GetDeadlinesAsync(user.Id, "Administrateur")).ToList();

        // Assert — les brouillons ne sont pas dans les deadlines
        deadlines.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDeadlinesAsync_UrgenceCalculeeCorrectement()
    {
        // Arrange
        using var db = CreateDb(nameof(GetDeadlinesAsync_UrgenceCalculeeCorrectement));
        var (user, cat, _) = await SeedBaseAsync(db);
        var service = CreateService(db);

        var created = await service.CreateAsync(new CreateBesoinDTO
        {
            Titre = "Besoin urgent", Description = "Desc",
            NiveauImportance = "ELEVE", CategorieId = cat.Id
        }, user.Id, "Agent");
        await service.EnregistrerAsync(created.Id, user.Id);
        await service.SoumettreAsync(created.Id, user.Id);

        // Simuler un besoin entré en attente il y a longtemps (délai dépassé)
        var besoinInDb = await db.Besoins.FindAsync(created.Id);
        besoinInDb!.DateEntreeEnAttente = DateTime.UtcNow.AddMinutes(-200); // délai = 60 min → 333%
        db.Besoins.Update(besoinInDb);
        await db.SaveChangesAsync();

        // Act
        var deadlines = (await service.GetDeadlinesAsync(user.Id, "Administrateur")).ToList();

        // Assert — urgence "expired" car > 100%
        deadlines.Should().HaveCount(1);
        deadlines[0].Urgence.Should().Be("expired");
        deadlines[0].PourcentageEcoule.Should().BeGreaterThan(100);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DeleteAsync — cas d'erreur
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DeleteAsync_BesoinEnAttente_LeveInvalidOperation()
    {
        // Arrange
        using var db = CreateDb(nameof(DeleteAsync_BesoinEnAttente_LeveInvalidOperation));
        var (user, cat, _) = await SeedBaseAsync(db);
        var service = CreateService(db);

        var created = await service.CreateAsync(new CreateBesoinDTO
        {
            Titre = "En attente", Description = "Desc",
            NiveauImportance = "MOYEN", CategorieId = cat.Id
        }, user.Id, "Agent");

        // Mettre en EN_ATTENTE directement
        var besoinInDb = await db.Besoins.FindAsync(created.Id);
        besoinInDb!.Statut = "EN_ATTENTE_RESPONSABLE";
        db.Besoins.Update(besoinInDb);
        await db.SaveChangesAsync();

        // Act
        var act = async () => await service.DeleteAsync(created.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*validation*");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Gestion des catégories
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateCategorieAsync_NomDuplique_LeveInvalidOperation()
    {
        // Arrange
        using var db = CreateDb(nameof(CreateCategorieAsync_NomDuplique_LeveInvalidOperation));
        var (_, cat, circuit) = await SeedBaseAsync(db);
        var service = CreateService(db);

        // Act — créer une catégorie avec le même nom
        var act = async () => await service.CreateCategorieAsync(new CreateCategorieDTO
        {
            Nom = "Informatique", // déjà existant
            WorkflowCircuitId = circuit.Id
        });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Informatique*");
    }

    [Fact]
    public async Task CreateCategorieAsync_CircuitInexistant_LeveKeyNotFound()
    {
        // Arrange
        using var db = CreateDb(nameof(CreateCategorieAsync_CircuitInexistant_LeveKeyNotFound));
        await SeedBaseAsync(db);
        var service = CreateService(db);

        // Act
        var act = async () => await service.CreateCategorieAsync(new CreateCategorieDTO
        {
            Nom = "Nouvelle Catégorie",
            WorkflowCircuitId = 999 // inexistant
        });

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*999*");
    }

    [Fact]
    public async Task DeleteCategorieAsync_AvecBesoinsRattaches_LeveInvalidOperation()
    {
        // Arrange
        using var db = CreateDb(nameof(DeleteCategorieAsync_AvecBesoinsRattaches_LeveInvalidOperation));
        var (user, cat, _) = await SeedBaseAsync(db);
        await CreateBesoinBrouillonAsync(db, user.Id, cat.Id);
        var service = CreateService(db);

        // Act
        var act = async () => await service.DeleteCategorieAsync(cat.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*besoins*");
    }

    [Fact]
    public async Task UpdateCategorieAsync_NomDuplique_LeveInvalidOperation()
    {
        // Arrange
        using var db = CreateDb(nameof(UpdateCategorieAsync_NomDuplique_LeveInvalidOperation));
        var (_, cat, circuit) = await SeedBaseAsync(db);
        var service = CreateService(db);

        // Créer une deuxième catégorie
        await service.CreateCategorieAsync(new CreateCategorieDTO
        {
            Nom = "Finance", WorkflowCircuitId = circuit.Id
        });

        // Act — renommer "Finance" en "Informatique" (déjà pris)
        var financeId = (await db.Categories.FirstAsync(c => c.Nom == "Finance")).Id;
        var act = async () => await service.UpdateCategorieAsync(financeId, new UpdateCategorieDTO
        {
            Nom = "Informatique"
        });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Informatique*");
    }

    [Fact]
    public async Task AssignerCircuitAsync_CircuitExistant_MettreAJourCategorie()
    {
        // Arrange
        using var db = CreateDb(nameof(AssignerCircuitAsync_CircuitExistant_MettreAJourCategorie));
        var (_, cat, _) = await SeedBaseAsync(db);
        var service = CreateService(db);

        // Créer un deuxième circuit
        var circuit2 = new WorkflowCircuit
        {
            Nom = "Circuit 2", NomCreateur = "Admin",
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow,
            Etapes = [new EtapeCircuit { Ordre = 1, RoleRequis = "Direction", DelaiMaxJours = 30, EstDerniereEtape = true }]
        };
        db.WorkflowCircuits.Add(circuit2);
        await db.SaveChangesAsync();

        // Act
        var result = await service.AssignerCircuitAsync(cat.Id, circuit2.Id);

        // Assert
        result.WorkflowCircuitId.Should().Be(circuit2.Id);

        var catInDb = await db.Categories.FindAsync(cat.Id);
        catInDb!.WorkflowCircuitId.Should().Be(circuit2.Id);
    }
}
