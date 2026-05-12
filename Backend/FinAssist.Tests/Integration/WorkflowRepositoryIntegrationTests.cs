using FinAssist.Core.Entities;
using FinAssist.Infrastructure.Data;
using FinAssist.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FinAssist.Tests.Integration;

/// <summary>
/// Tests d'intégration pour WorkflowRepository (circuits + validations) + AppDbContext (InMemory).
/// Complète WorkflowIntegrationTests qui teste via le service.
/// Ici on teste le repository directement.
/// </summary>
public class WorkflowRepositoryIntegrationTests
{
    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    private static WorkflowRepository CreateRepo(AppDbContext db) => new(db);

    private static async Task<WorkflowCircuit> SeedCircuitAsync(AppDbContext db, string nom = "Circuit Test")
    {
        var circuit = new WorkflowCircuit
        {
            Nom = nom,
            NomCreateur = "Admin",
            DateCreation = DateTime.UtcNow,
            DateModification = DateTime.UtcNow,
            Etapes =
            [
                new EtapeCircuit { Ordre = 1, RoleRequis = "Responsable", DelaiMaxJours = 60 },
                new EtapeCircuit { Ordre = 2, RoleRequis = "Direction", DelaiMaxJours = 60, EstDerniereEtape = true }
            ]
        };
        db.WorkflowCircuits.Add(circuit);
        await db.SaveChangesAsync();
        return circuit;
    }

    private static async Task<(Utilisateur user, Besoin besoin)> SeedBesoinAsync(AppDbContext db, int circuitId)
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
        var cat = new Categorie { Nom = "Informatique", WorkflowCircuitId = circuitId, DateCreation = DateTime.UtcNow };
        db.Categories.Add(cat);
        await db.SaveChangesAsync();

        var besoin = new Besoin
        {
            Titre = "Besoin test", Description = "Desc",
            Statut = "EN_ATTENTE_RESPONSABLE", NiveauImportance = "MOYEN",
            UtilisateurId = user.Id, CategorieId = cat.Id,
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow
        };
        db.Besoins.Add(besoin);
        await db.SaveChangesAsync();
        return (user, besoin);
    }

    // ── CreateCircuitAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateCircuitAsync_CircuitAvecEtapes_PersistEnBase()
    {
        // Arrange
        using var db = CreateDb(nameof(CreateCircuitAsync_CircuitAvecEtapes_PersistEnBase));
        var repo = CreateRepo(db);

        var circuit = new WorkflowCircuit
        {
            Nom = "Circuit Direct",
            NomCreateur = "Admin",
            DateCreation = DateTime.UtcNow,
            DateModification = DateTime.UtcNow,
            Etapes =
            [
                new EtapeCircuit { Ordre = 1, RoleRequis = "Responsable", DelaiMaxJours = 30, EstDerniereEtape = true }
            ]
        };

        // Act
        var result = await repo.CreateCircuitAsync(circuit);

        // Assert
        result.Id.Should().BeGreaterThan(0);
        result.Nom.Should().Be("Circuit Direct");

        var inDb = await db.WorkflowCircuits
            .Include(c => c.Etapes)
            .FirstOrDefaultAsync(c => c.Id == result.Id);
        inDb.Should().NotBeNull();
        inDb!.Etapes.Should().HaveCount(1);
        inDb.Etapes.First().RoleRequis.Should().Be("Responsable");
    }

    // ── GetCircuitByIdAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetCircuitByIdAsync_CircuitExistant_RetourneAvecEtapesOrdonnees()
    {
        // Arrange
        using var db = CreateDb(nameof(GetCircuitByIdAsync_CircuitExistant_RetourneAvecEtapesOrdonnees));
        var circuit = await SeedCircuitAsync(db);
        var repo = CreateRepo(db);

        // Act
        var result = await repo.GetCircuitByIdAsync(circuit.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Nom.Should().Be("Circuit Test");
        result.Etapes.Should().HaveCount(2);
        result.Etapes.First().Ordre.Should().Be(1);
        result.Etapes.Last().Ordre.Should().Be(2);
    }

    [Fact]
    public async Task GetCircuitByIdAsync_CircuitInexistant_RetourneNull()
    {
        // Arrange
        using var db = CreateDb(nameof(GetCircuitByIdAsync_CircuitInexistant_RetourneNull));
        var repo = CreateRepo(db);

        // Act
        var result = await repo.GetCircuitByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    // ── GetAllCircuitsAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetAllCircuitsAsync_PlusieursCircuits_RetourneTous()
    {
        // Arrange
        using var db = CreateDb(nameof(GetAllCircuitsAsync_PlusieursCircuits_RetourneTous));
        await SeedCircuitAsync(db, "Circuit A");
        await SeedCircuitAsync(db, "Circuit B");
        var repo = CreateRepo(db);

        // Act
        var result = (await repo.GetAllCircuitsAsync()).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Select(c => c.Nom).Should().Contain(["Circuit A", "Circuit B"]);
    }

    [Fact]
    public async Task GetAllCircuitsAsync_BaseVide_RetourneListeVide()
    {
        // Arrange
        using var db = CreateDb(nameof(GetAllCircuitsAsync_BaseVide_RetourneListeVide));
        var repo = CreateRepo(db);

        // Act
        var result = (await repo.GetAllCircuitsAsync()).ToList();

        // Assert
        result.Should().BeEmpty();
    }

    // ── CircuitNomExistsAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task CircuitNomExistsAsync_NomExistant_RetourneTrue()
    {
        // Arrange
        using var db = CreateDb(nameof(CircuitNomExistsAsync_NomExistant_RetourneTrue));
        var circuit = await SeedCircuitAsync(db, "Circuit Unique");
        var repo = CreateRepo(db);

        // Act
        var exists = await repo.CircuitNomExistsAsync("Circuit Unique");

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task CircuitNomExistsAsync_NomInexistant_RetourneFalse()
    {
        // Arrange
        using var db = CreateDb(nameof(CircuitNomExistsAsync_NomInexistant_RetourneFalse));
        var repo = CreateRepo(db);

        // Act
        var exists = await repo.CircuitNomExistsAsync("Inexistant");

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task CircuitNomExistsAsync_AvecExcludeId_ExclutLeCircuitLuiMeme()
    {
        // Arrange
        using var db = CreateDb(nameof(CircuitNomExistsAsync_AvecExcludeId_ExclutLeCircuitLuiMeme));
        var circuit = await SeedCircuitAsync(db, "Circuit Modifiable");
        var repo = CreateRepo(db);

        // Act — on exclut le circuit lui-même (cas update)
        var exists = await repo.CircuitNomExistsAsync("Circuit Modifiable", excludeId: circuit.Id);

        // Assert — doit retourner false car c'est le même circuit
        exists.Should().BeFalse();
    }

    // ── UpdateCircuitAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateCircuitAsync_ModifieNomEtDescription_PersisteLaModification()
    {
        // Arrange
        using var db = CreateDb(nameof(UpdateCircuitAsync_ModifieNomEtDescription_PersisteLaModification));
        var circuit = await SeedCircuitAsync(db);
        var repo = CreateRepo(db);

        // Act
        circuit.Nom = "Nom Modifié";
        circuit.Description = "Nouvelle description";
        circuit.DateModification = DateTime.UtcNow;
        var updated = await repo.UpdateCircuitAsync(circuit);

        // Assert
        updated.Nom.Should().Be("Nom Modifié");
        updated.Description.Should().Be("Nouvelle description");

        var inDb = await db.WorkflowCircuits.FindAsync(circuit.Id);
        inDb!.Nom.Should().Be("Nom Modifié");
    }

    // ── DeleteCircuitAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteCircuitAsync_CircuitExistant_SupprimeDeLaBase()
    {
        // Arrange
        using var db = CreateDb(nameof(DeleteCircuitAsync_CircuitExistant_SupprimeDeLaBase));
        var circuit = await SeedCircuitAsync(db);
        var repo = CreateRepo(db);

        // Act
        await repo.DeleteCircuitAsync(circuit);

        // Assert
        var inDb = await db.WorkflowCircuits.FindAsync(circuit.Id);
        inDb.Should().BeNull();
    }

    // ── AddValidationAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task AddValidationAsync_ValidationValide_PersistEnBase()
    {
        // Arrange
        using var db = CreateDb(nameof(AddValidationAsync_ValidationValide_PersistEnBase));
        var circuit = await SeedCircuitAsync(db);
        var (user, besoin) = await SeedBesoinAsync(db, circuit.Id);
        var repo = CreateRepo(db);

        var validation = new Validation
        {
            BesoinId = besoin.Id,
            ValidateurId = user.Id,
            Niveau = 1,
            EtapeOrdre = 1,
            Decision = DecisionValidation.APPROUVE,
            Motif = "Conforme",
            StatutApres = "APPROUVE_PAR_RESPONSABLE",
            DateDecision = DateTime.UtcNow
        };

        // Act
        var result = await repo.AddValidationAsync(validation);

        // Assert
        result.Id.Should().BeGreaterThan(0);
        result.Decision.Should().Be(DecisionValidation.APPROUVE);

        var inDb = await db.Validations.FindAsync(result.Id);
        inDb.Should().NotBeNull();
        inDb!.Motif.Should().Be("Conforme");
    }

    // ── GetValidationByIdAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetValidationByIdAsync_ValidationExistante_RetourneAvecRelations()
    {
        // Arrange
        using var db = CreateDb(nameof(GetValidationByIdAsync_ValidationExistante_RetourneAvecRelations));
        var circuit = await SeedCircuitAsync(db);
        var (user, besoin) = await SeedBesoinAsync(db, circuit.Id);
        var repo = CreateRepo(db);

        var validation = await repo.AddValidationAsync(new Validation
        {
            BesoinId = besoin.Id,
            ValidateurId = user.Id,
            Niveau = 1,
            EtapeOrdre = 1,
            Decision = DecisionValidation.APPROUVE,
            Motif = "OK",
            StatutApres = "APPROUVE_PAR_RESPONSABLE",
            DateDecision = DateTime.UtcNow
        });

        // Act
        var result = await repo.GetValidationByIdAsync(validation.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Validateur.Should().NotBeNull();
        result.Validateur!.Nom.Should().Be("Dupont");
        result.Besoin.Should().NotBeNull();
        result.Besoin!.Titre.Should().Be("Besoin test");
    }

    [Fact]
    public async Task GetValidationByIdAsync_ValidationInexistante_RetourneNull()
    {
        // Arrange
        using var db = CreateDb(nameof(GetValidationByIdAsync_ValidationInexistante_RetourneNull));
        var repo = CreateRepo(db);

        // Act
        var result = await repo.GetValidationByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    // ── GetValidationsByBesoinAsync ───────────────────────────────────────────

    [Fact]
    public async Task GetValidationsByBesoinAsync_PlusieursValidations_RetourneOrdonnees()
    {
        // Arrange
        using var db = CreateDb(nameof(GetValidationsByBesoinAsync_PlusieursValidations_RetourneOrdonnees));
        var circuit = await SeedCircuitAsync(db);
        var (user, besoin) = await SeedBesoinAsync(db, circuit.Id);
        var repo = CreateRepo(db);

        // Ajouter 2 validations à des étapes différentes
        await repo.AddValidationAsync(new Validation
        {
            BesoinId = besoin.Id, ValidateurId = user.Id,
            Niveau = 2, EtapeOrdre = 2,
            Decision = DecisionValidation.APPROUVE,
            Motif = "Etape 2", StatutApres = "APPROUVE_PAR_DIRECTION",
            DateDecision = DateTime.UtcNow
        });
        await repo.AddValidationAsync(new Validation
        {
            BesoinId = besoin.Id, ValidateurId = user.Id,
            Niveau = 1, EtapeOrdre = 1,
            Decision = DecisionValidation.APPROUVE,
            Motif = "Etape 1", StatutApres = "APPROUVE_PAR_RESPONSABLE",
            DateDecision = DateTime.UtcNow.AddMinutes(-5)
        });

        // Act
        var result = (await repo.GetValidationsByBesoinAsync(besoin.Id)).ToList();

        // Assert
        result.Should().HaveCount(2);
        // Doit être ordonné par EtapeOrdre
        result[0].EtapeOrdre.Should().Be(1);
        result[1].EtapeOrdre.Should().Be(2);
    }

    [Fact]
    public async Task GetValidationsByBesoinAsync_BesoinSansValidations_RetourneListeVide()
    {
        // Arrange
        using var db = CreateDb(nameof(GetValidationsByBesoinAsync_BesoinSansValidations_RetourneListeVide));
        var circuit = await SeedCircuitAsync(db);
        var (_, besoin) = await SeedBesoinAsync(db, circuit.Id);
        var repo = CreateRepo(db);

        // Act
        var result = (await repo.GetValidationsByBesoinAsync(besoin.Id)).ToList();

        // Assert
        result.Should().BeEmpty();
    }
}
