using FinAssist.Core.DTOs.Reporting;
using FinAssist.Core.Entities;
using FinAssist.Infrastructure.Data;
using FinAssist.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FinAssist.Tests.Integration;

/// <summary>
/// Tests d'intégration pour ReportingRepository + AppDbContext (InMemory).
/// </summary>
public class ReportingRepositoryIntegrationTests
{
    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    private static ReportingRepository CreateRepo(AppDbContext db)
        => new(db);

    private static async Task<(Role role, Utilisateur user, Categorie cat)> SeedBaseAsync(AppDbContext db)
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
        var cat = new Categorie { Nom = "Informatique", DateCreation = DateTime.UtcNow };
        db.Categories.Add(cat);
        await db.SaveChangesAsync();
        return (role, user, cat);
    }

    private static Besoin MakeBesoin(int userId, int catId, string statut, DateTime? dateCreation = null) => new()
    {
        Titre = $"Besoin {statut}",
        Description = "Desc",
        Statut = statut,
        NiveauImportance = "MOYEN",
        UtilisateurId = userId,
        CategorieId = catId,
        DateCreation = dateCreation ?? DateTime.UtcNow,
        DateModification = DateTime.UtcNow
    };

    // ── GetAllBesoinsAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllBesoinsAsync_RetourneTousLesBesoins()
    {
        // Arrange
        using var db = CreateDb(nameof(GetAllBesoinsAsync_RetourneTousLesBesoins));
        var (_, user, cat) = await SeedBaseAsync(db);
        db.Besoins.AddRange(
            MakeBesoin(user.Id, cat.Id, "BROUILLON"),
            MakeBesoin(user.Id, cat.Id, "ENREGISTRE"),
            MakeBesoin(user.Id, cat.Id, "TERMINE")
        );
        await db.SaveChangesAsync();
        var repo = CreateRepo(db);

        // Act
        var besoins = (await repo.GetAllBesoinsAsync()).ToList();

        // Assert
        besoins.Should().HaveCount(3);
    }

    // ── GetBesoinsFiltrésAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetBesoinsFiltrésAsync_SansFiltres_RetourneTous()
    {
        // Arrange
        using var db = CreateDb(nameof(GetBesoinsFiltrésAsync_SansFiltres_RetourneTous));
        var (_, user, cat) = await SeedBaseAsync(db);
        db.Besoins.AddRange(
            MakeBesoin(user.Id, cat.Id, "BROUILLON"),
            MakeBesoin(user.Id, cat.Id, "TERMINE")
        );
        await db.SaveChangesAsync();
        var repo = CreateRepo(db);

        // Act
        var besoins = (await repo.GetBesoinsFiltrésAsync(null)).ToList();

        // Assert
        besoins.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetBesoinsFiltrésAsync_FiltreParStatut_RetourneSeulementCeStatut()
    {
        // Arrange
        using var db = CreateDb(nameof(GetBesoinsFiltrésAsync_FiltreParStatut_RetourneSeulementCeStatut));
        var (_, user, cat) = await SeedBaseAsync(db);
        db.Besoins.AddRange(
            MakeBesoin(user.Id, cat.Id, "BROUILLON"),
            MakeBesoin(user.Id, cat.Id, "BROUILLON"),
            MakeBesoin(user.Id, cat.Id, "TERMINE")
        );
        await db.SaveChangesAsync();
        var repo = CreateRepo(db);

        // Act
        var besoins = (await repo.GetBesoinsFiltrésAsync(new FiltreRapportDTO { Statut = "BROUILLON" })).ToList();

        // Assert
        besoins.Should().HaveCount(2);
        besoins.All(b => b.Statut == "BROUILLON").Should().BeTrue();
    }

    [Fact]
    public async Task GetBesoinsFiltrésAsync_FiltreParDates_RetourneBesoinsInclus()
    {
        // Arrange
        using var db = CreateDb(nameof(GetBesoinsFiltrésAsync_FiltreParDates_RetourneBesoinsInclus));
        var (_, user, cat) = await SeedBaseAsync(db);
        var dateRef = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        db.Besoins.AddRange(
            MakeBesoin(user.Id, cat.Id, "BROUILLON", dateRef.AddDays(-10)), // avant
            MakeBesoin(user.Id, cat.Id, "ENREGISTRE", dateRef),              // dans la plage
            MakeBesoin(user.Id, cat.Id, "TERMINE", dateRef.AddDays(5)),      // dans la plage
            MakeBesoin(user.Id, cat.Id, "BROUILLON", dateRef.AddDays(40))    // après
        );
        await db.SaveChangesAsync();
        var repo = CreateRepo(db);

        // Act
        var besoins = (await repo.GetBesoinsFiltrésAsync(new FiltreRapportDTO
        {
            DateDebut = dateRef,
            DateFin = dateRef.AddDays(30)
        })).ToList();

        // Assert
        besoins.Should().HaveCount(2);
    }

    // ── GetAllUtilisateursAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetAllUtilisateursAsync_Retourne2Utilisateurs()
    {
        // Arrange
        using var db = CreateDb(nameof(GetAllUtilisateursAsync_Retourne2Utilisateurs));
        var role = new Role { Code = "Agent", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow };
        db.Roles.Add(role);
        db.Utilisateurs.AddRange(
            new Utilisateur { Nom = "A", Prenom = "A", Email = "a@finstar-cm.com", MotDePasse = "h", RoleId = role.Id, Actif = true, DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow },
            new Utilisateur { Nom = "B", Prenom = "B", Email = "b@finstar-cm.com", MotDePasse = "h", RoleId = role.Id, Actif = false, DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();
        var repo = CreateRepo(db);

        // Act
        var users = (await repo.GetAllUtilisateursAsync()).ToList();

        // Assert
        users.Should().HaveCount(2);
    }

    // ── CountSignaturesAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task CountSignaturesAsync_RetourneNombreCorrect()
    {
        // Arrange
        using var db = CreateDb(nameof(CountSignaturesAsync_RetourneNombreCorrect));
        var (_, user, cat) = await SeedBaseAsync(db);

        // Créer un besoin et un document pour les signatures
        var besoin = MakeBesoin(user.Id, cat.Id, "SIGNE_PAR_RESPONSABLE");
        db.Besoins.Add(besoin);
        await db.SaveChangesAsync();

        var doc = new Document { BesoinId = besoin.Id, Nom = "doc.pdf", Type = "application/pdf", Checksum = "abc", Contenu = [], DateCreation = DateTime.UtcNow };
        db.Documents.Add(doc);
        await db.SaveChangesAsync();

        db.Signatures.AddRange(
            new SignatureElectronique { DocumentId = doc.Id, UtilisateurId = user.Id, Valeur = "sig1", Empreinte = "hash1", Horodatage = DateTime.UtcNow, Valide = true },
            new SignatureElectronique { DocumentId = doc.Id, UtilisateurId = user.Id, Valeur = "sig2", Empreinte = "hash2", Horodatage = DateTime.UtcNow, Valide = true }
        );
        await db.SaveChangesAsync();
        var repo = CreateRepo(db);

        // Act
        var count = await repo.CountSignaturesAsync();

        // Assert
        count.Should().Be(2);
    }

    // ── CountNotificationsNonLuesAsync ────────────────────────────────────────

    [Fact]
    public async Task CountNotificationsNonLuesAsync_RetourneNombreNonLues()
    {
        // Arrange
        using var db = CreateDb(nameof(CountNotificationsNonLuesAsync_RetourneNombreNonLues));
        var (_, user, _) = await SeedBaseAsync(db);

        // Créer 3 notifications dont 2 non lues
        var notif1 = new Notification { Message = "N1", Type = TypeNotification.VALIDATION, DateEnvoi = DateTime.UtcNow };
        var notif2 = new Notification { Message = "N2", Type = TypeNotification.RAPPEL, DateEnvoi = DateTime.UtcNow };
        var notif3 = new Notification { Message = "N3", Type = TypeNotification.REJET, DateEnvoi = DateTime.UtcNow };
        db.Notifications.AddRange(notif1, notif2, notif3);
        await db.SaveChangesAsync();

        db.UtilisateurNotifications.AddRange(
            new UtilisateurNotification { UtilisateurId = user.Id, NotificationId = notif1.Id, Lu = false },
            new UtilisateurNotification { UtilisateurId = user.Id, NotificationId = notif2.Id, Lu = true },  // lue
            new UtilisateurNotification { UtilisateurId = user.Id, NotificationId = notif3.Id, Lu = false }
        );
        await db.SaveChangesAsync();
        var repo = CreateRepo(db);

        // Act
        var count = await repo.CountNotificationsNonLuesAsync(user.Id);

        // Assert
        count.Should().Be(2);
    }
}
