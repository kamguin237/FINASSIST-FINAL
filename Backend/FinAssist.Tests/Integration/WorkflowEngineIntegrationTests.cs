using FinAssist.Application.Services;
using FinAssist.Core.Entities;
using FinAssist.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FinAssist.Tests.Integration;

/// <summary>
/// Tests d'intégration pour WorkflowEngine (méthodes statiques) avec de vrais circuits
/// persistés en base InMemory. Vérifie les transitions complètes :
/// soumission → approbation → signature → terminé, rejet, circuit multi-étapes,
/// ResoudreEtapeCourante, AppliquerDecision, VerifierRole.
/// </summary>
public class WorkflowEngineIntegrationTests
{
    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    // ── Helpers de seed ───────────────────────────────────────────────────────

    private static async Task<WorkflowCircuit> SeedCircuitSimpleAsync(AppDbContext db)
    {
        var circuit = new WorkflowCircuit
        {
            Nom = "Circuit Simple", NomCreateur = "Admin",
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow,
            Etapes =
            [
                new EtapeCircuit
                {
                    Ordre = 1, RoleRequis = "Responsable",
                    DelaiMaxJours = 60, EstDerniereEtape = true,
                    SignatureRequise = true
                }
            ]
        };
        db.WorkflowCircuits.Add(circuit);
        await db.SaveChangesAsync();
        return circuit;
    }

    private static async Task<WorkflowCircuit> SeedCircuitMultiEtapesAsync(AppDbContext db)
    {
        var circuit = new WorkflowCircuit
        {
            Nom = "Circuit Multi", NomCreateur = "Admin",
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow,
            Etapes =
            [
                new EtapeCircuit { Ordre = 1, RoleRequis = "Responsable", DelaiMaxJours = 60, EstDerniereEtape = false, SignatureRequise = false },
                new EtapeCircuit { Ordre = 2, RoleRequis = "Direction",   DelaiMaxJours = 60, EstDerniereEtape = false, SignatureRequise = false },
                new EtapeCircuit { Ordre = 3, RoleRequis = "DG",          DelaiMaxJours = 60, EstDerniereEtape = true,  SignatureRequise = true  }
            ]
        };
        db.WorkflowCircuits.Add(circuit);
        await db.SaveChangesAsync();
        return circuit;
    }

    private static async Task<WorkflowCircuit> SeedCircuitAvecSignatureAsync(AppDbContext db)
    {
        var circuit = new WorkflowCircuit
        {
            Nom = "Circuit Signature", NomCreateur = "Admin",
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow,
            Etapes =
            [
                new EtapeCircuit { Ordre = 1, RoleRequis = "Responsable", DelaiMaxJours = 60, EstDerniereEtape = false, SignatureRequise = true },
                new EtapeCircuit { Ordre = 2, RoleRequis = "Direction",   DelaiMaxJours = 60, EstDerniereEtape = true,  SignatureRequise = true }
            ]
        };
        db.WorkflowCircuits.Add(circuit);
        await db.SaveChangesAsync();
        return circuit;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // StatutEnAttente / StatutApprouve / StatutSigne / StatutRejete
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task StatutEnAttente_AvecCircuitEnBase_GenereStatutCorrect()
    {
        // Arrange
        using var db = CreateDb(nameof(StatutEnAttente_AvecCircuitEnBase_GenereStatutCorrect));
        var circuit = await SeedCircuitSimpleAsync(db);
        var etape = circuit.Etapes.First();

        // Act
        var statut = WorkflowEngine.StatutEnAttente(etape.RoleRequis!);

        // Assert
        statut.Should().Be("EN_ATTENTE_RESPONSABLE");
    }

    [Fact]
    public async Task StatutApprouve_AvecCircuitEnBase_GenereStatutCorrect()
    {
        // Arrange
        using var db = CreateDb(nameof(StatutApprouve_AvecCircuitEnBase_GenereStatutCorrect));
        var circuit = await SeedCircuitSimpleAsync(db);
        var etape = circuit.Etapes.First();

        // Act
        var statut = WorkflowEngine.StatutApprouve(etape.RoleRequis!);

        // Assert
        statut.Should().Be("APPROUVE_PAR_RESPONSABLE");
    }

    [Fact]
    public async Task StatutSigne_AvecCircuitEnBase_GenereStatutCorrect()
    {
        // Arrange
        using var db = CreateDb(nameof(StatutSigne_AvecCircuitEnBase_GenereStatutCorrect));
        var circuit = await SeedCircuitSimpleAsync(db);
        var etape = circuit.Etapes.First();

        // Act
        var statut = WorkflowEngine.StatutSigne(etape.RoleRequis!);

        // Assert
        statut.Should().Be("SIGNE_PAR_RESPONSABLE");
    }

    [Fact]
    public async Task StatutRejete_AvecCircuitEnBase_GenereStatutCorrect()
    {
        // Arrange
        using var db = CreateDb(nameof(StatutRejete_AvecCircuitEnBase_GenereStatutCorrect));
        var circuit = await SeedCircuitSimpleAsync(db);
        var etape = circuit.Etapes.First();

        // Act
        var statut = WorkflowEngine.StatutRejete(etape.RoleRequis!);

        // Assert
        statut.Should().Be("REJETE_PAR_RESPONSABLE");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // EstEnAttente / EstApprouve / EstRejete / EstSigne
    // ═══════════════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("EN_ATTENTE_RESPONSABLE", true)]
    [InlineData("EN_ATTENTE_DIRECTION",   true)]
    [InlineData("EN_ATTENTE",             true)]
    [InlineData("BROUILLON",              false)]
    [InlineData("APPROUVE_PAR_RESPONSABLE", false)]
    [InlineData("TERMINE",                false)]
    public void EstEnAttente_VariousStatuts_RetourneResultatAttendu(string statut, bool attendu)
    {
        WorkflowEngine.EstEnAttente(statut).Should().Be(attendu);
    }

    [Theory]
    [InlineData("APPROUVE_PAR_RESPONSABLE", true)]
    [InlineData("APPROUVE_PAR_DIRECTION",   true)]
    [InlineData("EN_ATTENTE_RESPONSABLE",   false)]
    [InlineData("REJETE_PAR_RESPONSABLE",   false)]
    [InlineData("TERMINE",                  false)]
    public void EstApprouve_VariousStatuts_RetourneResultatAttendu(string statut, bool attendu)
    {
        WorkflowEngine.EstApprouve(statut).Should().Be(attendu);
    }

    [Theory]
    [InlineData("REJETE_PAR_RESPONSABLE", true)]
    [InlineData("REJETE_PAR_DIRECTION",   true)]
    [InlineData("APPROUVE_PAR_RESPONSABLE", false)]
    [InlineData("TERMINE",                  false)]
    public void EstRejete_VariousStatuts_RetourneResultatAttendu(string statut, bool attendu)
    {
        WorkflowEngine.EstRejete(statut).Should().Be(attendu);
    }

    [Theory]
    [InlineData("SIGNE_PAR_RESPONSABLE", true)]
    [InlineData("SIGNE_PAR_DIRECTION",   true)]
    [InlineData("APPROUVE_PAR_RESPONSABLE", false)]
    [InlineData("TERMINE",                  false)]
    public void EstSigne_VariousStatuts_RetourneResultatAttendu(string statut, bool attendu)
    {
        WorkflowEngine.EstSigne(statut).Should().Be(attendu);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ResoudreEtapeCourante — avec vrais circuits en base
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ResoudreEtapeCourante_EnAttenteResponsable_RetourneEtape1()
    {
        // Arrange
        using var db = CreateDb(nameof(ResoudreEtapeCourante_EnAttenteResponsable_RetourneEtape1));
        var circuit = await SeedCircuitMultiEtapesAsync(db);
        var etapes = circuit.Etapes.ToList();

        // Act
        var etape = WorkflowEngine.ResoudreEtapeCourante("EN_ATTENTE_RESPONSABLE", etapes);

        // Assert
        etape.Should().NotBeNull();
        etape!.Ordre.Should().Be(1);
        etape.RoleRequis.Should().Be("Responsable");
    }

    [Fact]
    public async Task ResoudreEtapeCourante_EnAttenteDirection_RetourneEtape2()
    {
        // Arrange
        using var db = CreateDb(nameof(ResoudreEtapeCourante_EnAttenteDirection_RetourneEtape2));
        var circuit = await SeedCircuitMultiEtapesAsync(db);
        var etapes = circuit.Etapes.ToList();

        // Act
        var etape = WorkflowEngine.ResoudreEtapeCourante("EN_ATTENTE_DIRECTION", etapes);

        // Assert
        etape.Should().NotBeNull();
        etape!.Ordre.Should().Be(2);
        etape.RoleRequis.Should().Be("Direction");
    }

    [Fact]
    public async Task ResoudreEtapeCourante_AvecEtapeCouranteOrdre_PrioriteSurStatut()
    {
        // Arrange
        using var db = CreateDb(nameof(ResoudreEtapeCourante_AvecEtapeCouranteOrdre_PrioriteSurStatut));
        var circuit = await SeedCircuitMultiEtapesAsync(db);
        var etapes = circuit.Etapes.ToList();

        // Act — etapeCouranteOrdre=3 doit primer sur le statut EN_ATTENTE_RESPONSABLE
        var etape = WorkflowEngine.ResoudreEtapeCourante("EN_ATTENTE_RESPONSABLE", etapes, etapeCouranteOrdre: 3);

        // Assert — retourne l'étape 3 (DG), pas l'étape 1 (Responsable)
        etape.Should().NotBeNull();
        etape!.Ordre.Should().Be(3);
        etape.RoleRequis.Should().Be("DG");
    }

    [Fact]
    public async Task ResoudreEtapeCourante_ApprouveParResponsable_RetourneEtapeResponsable()
    {
        // Arrange
        using var db = CreateDb(nameof(ResoudreEtapeCourante_ApprouveParResponsable_RetourneEtapeResponsable));
        var circuit = await SeedCircuitMultiEtapesAsync(db);
        var etapes = circuit.Etapes.ToList();

        // Act
        var etape = WorkflowEngine.ResoudreEtapeCourante("APPROUVE_PAR_RESPONSABLE", etapes);

        // Assert
        etape.Should().NotBeNull();
        etape!.RoleRequis.Should().Be("Responsable");
    }

    [Fact]
    public async Task ResoudreEtapeCourante_StatutTermine_RetourneNull()
    {
        // Arrange
        using var db = CreateDb(nameof(ResoudreEtapeCourante_StatutTermine_RetourneNull));
        var circuit = await SeedCircuitSimpleAsync(db);
        var etapes = circuit.Etapes.ToList();

        // Act
        var etape = WorkflowEngine.ResoudreEtapeCourante("TERMINE", etapes);

        // Assert
        etape.Should().BeNull();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // AppliquerDecision — avec vrais circuits en base
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AppliquerDecision_ApprobationEtapeSansSignature_AvanceVersEtapeSuivante()
    {
        // Arrange
        using var db = CreateDb(nameof(AppliquerDecision_ApprobationEtapeSansSignature_AvanceVersEtapeSuivante));
        var circuit = await SeedCircuitMultiEtapesAsync(db);
        var etapes = circuit.Etapes.OrderBy(e => e.Ordre).ToList();
        var etape1 = etapes[0]; // Responsable, SignatureRequise = false

        // Act
        var (nouveauStatut, prochaineEtapeOrdre) = WorkflowEngine.AppliquerDecision(
            "EN_ATTENTE_RESPONSABLE", etape1, etapes, "APPROUVE");

        // Assert — avance vers l'étape 2 (Direction)
        nouveauStatut.Should().Be("EN_ATTENTE_DIRECTION");
        prochaineEtapeOrdre.Should().Be(2);
    }

    [Fact]
    public async Task AppliquerDecision_ApprobationEtapeAvecSignature_PasseEnApprouve()
    {
        // Arrange
        using var db = CreateDb(nameof(AppliquerDecision_ApprobationEtapeAvecSignature_PasseEnApprouve));
        var circuit = await SeedCircuitAvecSignatureAsync(db);
        var etapes = circuit.Etapes.OrderBy(e => e.Ordre).ToList();
        var etape1 = etapes[0]; // Responsable, SignatureRequise = true

        // Act
        var (nouveauStatut, prochaineEtapeOrdre) = WorkflowEngine.AppliquerDecision(
            "EN_ATTENTE_RESPONSABLE", etape1, etapes, "APPROUVE");

        // Assert — en attente de signature
        nouveauStatut.Should().Be("APPROUVE_PAR_RESPONSABLE");
        prochaineEtapeOrdre.Should().Be(1);
    }

    [Fact]
    public async Task AppliquerDecision_Rejet_GenereStatutRejete()
    {
        // Arrange
        using var db = CreateDb(nameof(AppliquerDecision_Rejet_GenereStatutRejete));
        var circuit = await SeedCircuitSimpleAsync(db);
        var etapes = circuit.Etapes.OrderBy(e => e.Ordre).ToList();
        var etape1 = etapes[0];

        // Act
        var (nouveauStatut, prochaineEtapeOrdre) = WorkflowEngine.AppliquerDecision(
            "EN_ATTENTE_RESPONSABLE", etape1, etapes, "REJETE");

        // Assert
        nouveauStatut.Should().Be("REJETE_PAR_RESPONSABLE");
        prochaineEtapeOrdre.Should().BeNull();
    }

    [Fact]
    public async Task AppliquerDecision_DerniereEtapeSansSignature_RetourneTermine()
    {
        // Arrange
        using var db = CreateDb(nameof(AppliquerDecision_DerniereEtapeSansSignature_RetourneTermine));

        // Circuit avec une seule étape sans signature requise
        var circuit = new WorkflowCircuit
        {
            Nom = "Circuit Final", NomCreateur = "Admin",
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow,
            Etapes = [new EtapeCircuit { Ordre = 1, RoleRequis = "DG", DelaiMaxJours = 60, EstDerniereEtape = true, SignatureRequise = false }]
        };
        db.WorkflowCircuits.Add(circuit);
        await db.SaveChangesAsync();

        var etapes = circuit.Etapes.OrderBy(e => e.Ordre).ToList();
        var etape1 = etapes[0];

        // Act
        var (nouveauStatut, prochaineEtapeOrdre) = WorkflowEngine.AppliquerDecision(
            "EN_ATTENTE_DG", etape1, etapes, "APPROUVE");

        // Assert — dernière étape sans signature → TERMINE directement
        nouveauStatut.Should().Be("TERMINE");
        prochaineEtapeOrdre.Should().BeNull();
    }

    [Fact]
    public async Task AppliquerDecision_DecisionInvalide_LeveArgumentException()
    {
        // Arrange
        using var db = CreateDb(nameof(AppliquerDecision_DecisionInvalide_LeveArgumentException));
        var circuit = await SeedCircuitSimpleAsync(db);
        var etapes = circuit.Etapes.OrderBy(e => e.Ordre).ToList();
        var etape1 = etapes[0];

        // Act
        var act = () => WorkflowEngine.AppliquerDecision(
            "EN_ATTENTE_RESPONSABLE", etape1, etapes, "INVALIDE");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*APPROUVE*REJETE*");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // VerifierRole — avec vrais circuits en base
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task VerifierRole_RoleCorrect_NeLancePasException()
    {
        // Arrange
        using var db = CreateDb(nameof(VerifierRole_RoleCorrect_NeLancePasException));
        var circuit = await SeedCircuitSimpleAsync(db);
        var etape = circuit.Etapes.First();

        // Act
        var act = () => WorkflowEngine.VerifierRole(etape, "Responsable");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public async Task VerifierRole_RoleIncorrect_LeveUnauthorized()
    {
        // Arrange
        using var db = CreateDb(nameof(VerifierRole_RoleIncorrect_LeveUnauthorized));
        var circuit = await SeedCircuitSimpleAsync(db);
        var etape = circuit.Etapes.First();

        // Act
        var act = () => WorkflowEngine.VerifierRole(etape, "Agent");

        // Assert
        act.Should().Throw<UnauthorizedAccessException>()
            .WithMessage("*Responsable*");
    }

    [Fact]
    public async Task VerifierRole_RoleIncorrectMaisPermissionDirecte_NeLancePasException()
    {
        // Arrange
        using var db = CreateDb(nameof(VerifierRole_RoleIncorrectMaisPermissionDirecte_NeLancePasException));
        var circuit = await SeedCircuitSimpleAsync(db);
        var etape = circuit.Etapes.First(); // RoleRequis = "Responsable"

        // Permission directe pour agir à la place du Responsable
        var permissions = new HashSet<string> { "BESOIN_VALIDER_RESPONSABLE" };

        // Act
        var act = () => WorkflowEngine.VerifierRole(etape, "Agent", permissions);

        // Assert — permission directe → autorisé
        act.Should().NotThrow();
    }

    [Fact]
    public async Task VerifierRole_InsensibleCasse_NeLancePasException()
    {
        // Arrange
        using var db = CreateDb(nameof(VerifierRole_InsensibleCasse_NeLancePasException));
        var circuit = await SeedCircuitSimpleAsync(db);
        var etape = circuit.Etapes.First(); // RoleRequis = "Responsable"

        // Act — rôle en minuscules
        var act = () => WorkflowEngine.VerifierRole(etape, "responsable");

        // Assert
        act.Should().NotThrow();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Transitions complètes — circuit multi-étapes en base
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task TransitionComplete_CircuitMultiEtapes_SoumissionVersTermine()
    {
        // Arrange
        using var db = CreateDb(nameof(TransitionComplete_CircuitMultiEtapes_SoumissionVersTermine));
        var circuit = await SeedCircuitMultiEtapesAsync(db);
        var etapes = circuit.Etapes.OrderBy(e => e.Ordre).ToList();

        // Simuler le parcours complet d'un besoin
        var statut = "EN_ATTENTE_RESPONSABLE";

        // Étape 1 : Responsable approuve (sans signature)
        var (s1, _) = WorkflowEngine.AppliquerDecision(statut, etapes[0], etapes, "APPROUVE");
        s1.Should().Be("EN_ATTENTE_DIRECTION");

        // Étape 2 : Direction approuve (sans signature)
        var (s2, _) = WorkflowEngine.AppliquerDecision(s1, etapes[1], etapes, "APPROUVE");
        s2.Should().Be("EN_ATTENTE_DG");

        // Étape 3 : DG approuve (avec signature, dernière étape)
        // D'abord → APPROUVE_PAR_DG (en attente de signature)
        var (s3, _) = WorkflowEngine.AppliquerDecision(s2, etapes[2], etapes, "APPROUVE");
        s3.Should().Be("APPROUVE_PAR_DG");

        // Puis signature → TERMINE
        var (s4, _) = WorkflowEngine.AppliquerSignature(etapes[2], etapes);
        s4.Should().Be("TERMINE");
    }

    [Fact]
    public async Task TransitionComplete_CircuitMultiEtapes_RejetEtape2()
    {
        // Arrange
        using var db = CreateDb(nameof(TransitionComplete_CircuitMultiEtapes_RejetEtape2));
        var circuit = await SeedCircuitMultiEtapesAsync(db);
        var etapes = circuit.Etapes.OrderBy(e => e.Ordre).ToList();

        // Étape 1 : Responsable approuve
        var (s1, _) = WorkflowEngine.AppliquerDecision("EN_ATTENTE_RESPONSABLE", etapes[0], etapes, "APPROUVE");
        s1.Should().Be("EN_ATTENTE_DIRECTION");

        // Étape 2 : Direction rejette
        var (s2, _) = WorkflowEngine.AppliquerDecision(s1, etapes[1], etapes, "REJETE");
        s2.Should().Be("REJETE_PAR_DIRECTION");

        // Assert — statut final est un rejet
        WorkflowEngine.EstRejete(s2).Should().BeTrue();
    }

    [Fact]
    public async Task TransitionComplete_CircuitAvecSignature_CheminComplet()
    {
        // Arrange
        using var db = CreateDb(nameof(TransitionComplete_CircuitAvecSignature_CheminComplet));
        var circuit = await SeedCircuitAvecSignatureAsync(db);
        var etapes = circuit.Etapes.OrderBy(e => e.Ordre).ToList();

        // Étape 1 : Responsable approuve → APPROUVE_PAR_RESPONSABLE (signature requise)
        var (s1, _) = WorkflowEngine.AppliquerDecision("EN_ATTENTE_RESPONSABLE", etapes[0], etapes, "APPROUVE");
        s1.Should().Be("APPROUVE_PAR_RESPONSABLE");

        // Signature Responsable → avance vers étape 2
        var (s2, _) = WorkflowEngine.AppliquerSignature(etapes[0], etapes);
        s2.Should().Be("EN_ATTENTE_DIRECTION");

        // Étape 2 : Direction approuve → APPROUVE_PAR_DIRECTION (signature requise)
        var (s3, _) = WorkflowEngine.AppliquerDecision(s2, etapes[1], etapes, "APPROUVE");
        s3.Should().Be("APPROUVE_PAR_DIRECTION");

        // Signature Direction → TERMINE (dernière étape)
        var (s4, _) = WorkflowEngine.AppliquerSignature(etapes[1], etapes);
        s4.Should().Be("TERMINE");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // AppliquerSignature
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AppliquerSignature_EtapeIntermediaire_AvanceVersEtapeSuivante()
    {
        // Arrange
        using var db = CreateDb(nameof(AppliquerSignature_EtapeIntermediaire_AvanceVersEtapeSuivante));
        var circuit = await SeedCircuitMultiEtapesAsync(db);
        var etapes = circuit.Etapes.OrderBy(e => e.Ordre).ToList();

        // Act — signature à l'étape 1 (Responsable)
        var (nouveauStatut, prochaineEtapeOrdre) = WorkflowEngine.AppliquerSignature(etapes[0], etapes);

        // Assert — avance vers étape 2 (Direction)
        nouveauStatut.Should().Be("EN_ATTENTE_DIRECTION");
        prochaineEtapeOrdre.Should().Be(2);
    }

    [Fact]
    public async Task AppliquerSignature_DerniereEtape_RetourneTermine()
    {
        // Arrange
        using var db = CreateDb(nameof(AppliquerSignature_DerniereEtape_RetourneTermine));
        var circuit = await SeedCircuitSimpleAsync(db);
        var etapes = circuit.Etapes.OrderBy(e => e.Ordre).ToList();
        var derniereEtape = etapes.Last(); // EstDerniereEtape = true

        // Act
        var (nouveauStatut, prochaineEtapeOrdre) = WorkflowEngine.AppliquerSignature(derniereEtape, etapes);

        // Assert
        nouveauStatut.Should().Be("TERMINE");
        prochaineEtapeOrdre.Should().BeNull();
    }
}
