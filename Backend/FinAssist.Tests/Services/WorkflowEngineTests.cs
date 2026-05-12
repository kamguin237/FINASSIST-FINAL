using FinAssist.Application.Services;
using FinAssist.Core.Entities;
using FluentAssertions;

namespace FinAssist.Tests.Services;

/// <summary>
/// Tests unitaires pour WorkflowEngine — logique pure, pas de dépendances externes.
/// </summary>
public class WorkflowEngineTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static EtapeCircuit Etape(int ordre, string role, bool signature = false, bool derniere = false)
        => new() { Id = ordre, Ordre = ordre, RoleRequis = role, SignatureRequise = signature, EstDerniereEtape = derniere, ApprobationRequise = true, DelaiMaxJours = 7 };

    private static List<EtapeCircuit> CircuitDeuxEtapes() =>
    [
        Etape(1, "Responsable"),
        Etape(2, "Direction", derniere: true)
    ];

    private static List<EtapeCircuit> CircuitAvecSignature() =>
    [
        Etape(1, "Responsable", signature: true),
        Etape(2, "Direction", derniere: true)
    ];

    // ── Génération des statuts ────────────────────────────────────────────────

    [Fact]
    public void StatutEnAttente_RetourneFormatCorrect()
    {
        WorkflowEngine.StatutEnAttente("Responsable").Should().Be("EN_ATTENTE_RESPONSABLE");
        WorkflowEngine.StatutEnAttente("direction").Should().Be("EN_ATTENTE_DIRECTION");
        WorkflowEngine.StatutEnAttente("  Admin  ").Should().Be("EN_ATTENTE_ADMIN");
    }

    [Fact]
    public void StatutApprouve_RetourneFormatCorrect()
    {
        WorkflowEngine.StatutApprouve("Responsable").Should().Be("APPROUVE_PAR_RESPONSABLE");
    }

    [Fact]
    public void StatutSigne_RetourneFormatCorrect()
    {
        WorkflowEngine.StatutSigne("Direction").Should().Be("SIGNE_PAR_DIRECTION");
    }

    [Fact]
    public void StatutRejete_RetourneFormatCorrect()
    {
        WorkflowEngine.StatutRejete("Responsable").Should().Be("REJETE_PAR_RESPONSABLE");
    }

    // ── Helpers de détection ─────────────────────────────────────────────────

    [Theory]
    [InlineData("EN_ATTENTE", true)]
    [InlineData("EN_ATTENTE_RESPONSABLE", true)]
    [InlineData("EN_ATTENTE_DIRECTION", true)]
    [InlineData("BROUILLON", false)]
    [InlineData("TERMINE", false)]
    [InlineData("APPROUVE_PAR_RESPONSABLE", false)]
    public void EstEnAttente_DetecteCorrectement(string statut, bool attendu)
    {
        WorkflowEngine.EstEnAttente(statut).Should().Be(attendu);
    }

    [Theory]
    [InlineData("APPROUVE_PAR_RESPONSABLE", true)]
    [InlineData("APPROUVE_PAR_DIRECTION", true)]
    [InlineData("EN_ATTENTE_RESPONSABLE", false)]
    [InlineData("BROUILLON", false)]
    public void EstApprouve_DetecteCorrectement(string statut, bool attendu)
    {
        WorkflowEngine.EstApprouve(statut).Should().Be(attendu);
    }

    [Theory]
    [InlineData("REJETE_PAR_RESPONSABLE", true)]
    [InlineData("REJETE_PAR_DIRECTION", true)]
    [InlineData("EN_ATTENTE_RESPONSABLE", false)]
    [InlineData("TERMINE", false)]
    public void EstRejete_DetecteCorrectement(string statut, bool attendu)
    {
        WorkflowEngine.EstRejete(statut).Should().Be(attendu);
    }

    [Theory]
    [InlineData("SIGNE_PAR_RESPONSABLE", true)]
    [InlineData("SIGNE_PAR_DIRECTION", true)]
    [InlineData("APPROUVE_PAR_RESPONSABLE", false)]
    public void EstSigne_DetecteCorrectement(string statut, bool attendu)
    {
        WorkflowEngine.EstSigne(statut).Should().Be(attendu);
    }

    // ── Résolution de l'étape courante ───────────────────────────────────────

    [Fact]
    public void ResoudreEtapeCourante_AvecOrdre_RetourneBonneEtape()
    {
        var etapes = CircuitDeuxEtapes();
        var etape = WorkflowEngine.ResoudreEtapeCourante("EN_ATTENTE_DIRECTION", etapes, etapeCouranteOrdre: 2);
        etape.Should().NotBeNull();
        etape!.RoleRequis.Should().Be("Direction");
    }

    [Fact]
    public void ResoudreEtapeCourante_SansOrdre_ExtraireRoleDepuisStatut()
    {
        var etapes = CircuitDeuxEtapes();
        var etape = WorkflowEngine.ResoudreEtapeCourante("EN_ATTENTE_RESPONSABLE", etapes);
        etape.Should().NotBeNull();
        etape!.RoleRequis.Should().Be("Responsable");
    }

    [Fact]
    public void ResoudreEtapeCourante_StatutApprouve_RetourneEtapeCorrespondante()
    {
        var etapes = CircuitAvecSignature();
        var etape = WorkflowEngine.ResoudreEtapeCourante("APPROUVE_PAR_RESPONSABLE", etapes);
        etape.Should().NotBeNull();
        etape!.RoleRequis.Should().Be("Responsable");
    }

    [Fact]
    public void ResoudreEtapeCourante_StatutInconnu_RetourneNull()
    {
        var etapes = CircuitDeuxEtapes();
        var etape = WorkflowEngine.ResoudreEtapeCourante("BROUILLON", etapes);
        etape.Should().BeNull();
    }

    // ── Application d'une décision ───────────────────────────────────────────

    [Fact]
    public void AppliquerDecision_Rejet_RetourneStatutRejete()
    {
        var etapes = CircuitDeuxEtapes();
        var etape = etapes[0]; // Responsable
        var (statut, ordre) = WorkflowEngine.AppliquerDecision("EN_ATTENTE_RESPONSABLE", etape, etapes, "REJETE");
        statut.Should().Be("REJETE_PAR_RESPONSABLE");
        ordre.Should().BeNull();
    }

    [Fact]
    public void AppliquerDecision_Approbation_SansSignature_AvanceEtapeSuivante()
    {
        var etapes = CircuitDeuxEtapes();
        var etape = etapes[0]; // Responsable, pas de signature
        var (statut, ordre) = WorkflowEngine.AppliquerDecision("EN_ATTENTE_RESPONSABLE", etape, etapes, "APPROUVE");
        statut.Should().Be("EN_ATTENTE_DIRECTION");
        ordre.Should().Be(2);
    }

    [Fact]
    public void AppliquerDecision_Approbation_AvecSignature_RetourneStatutApprouve()
    {
        var etapes = CircuitAvecSignature();
        var etape = etapes[0]; // Responsable avec signature requise
        var (statut, ordre) = WorkflowEngine.AppliquerDecision("EN_ATTENTE_RESPONSABLE", etape, etapes, "APPROUVE");
        statut.Should().Be("APPROUVE_PAR_RESPONSABLE");
        ordre.Should().Be(1);
    }

    [Fact]
    public void AppliquerDecision_DerniereEtape_RetourneTermine()
    {
        var etapes = CircuitDeuxEtapes();
        var etape = etapes[1]; // Direction, dernière étape
        var (statut, ordre) = WorkflowEngine.AppliquerDecision("EN_ATTENTE_DIRECTION", etape, etapes, "APPROUVE");
        statut.Should().Be("TERMINE");
        ordre.Should().BeNull();
    }

    [Fact]
    public void AppliquerDecision_DecisionInvalide_LeveException()
    {
        var etapes = CircuitDeuxEtapes();
        var etape = etapes[0];
        var act = () => WorkflowEngine.AppliquerDecision("EN_ATTENTE_RESPONSABLE", etape, etapes, "INVALIDE");
        act.Should().Throw<ArgumentException>();
    }

    // ── Application d'une signature ──────────────────────────────────────────

    [Fact]
    public void AppliquerSignature_AvanceVersEtapeSuivante()
    {
        var etapes = CircuitAvecSignature();
        var etape = etapes[0]; // Responsable
        var (statut, ordre) = WorkflowEngine.AppliquerSignature(etape, etapes);
        statut.Should().Be("EN_ATTENTE_DIRECTION");
        ordre.Should().Be(2);
    }

    [Fact]
    public void AppliquerSignature_DerniereEtape_RetourneTermine()
    {
        var etapes = new List<EtapeCircuit> { Etape(1, "Direction", signature: true, derniere: true) };
        var (statut, ordre) = WorkflowEngine.AppliquerSignature(etapes[0], etapes);
        statut.Should().Be("TERMINE");
        ordre.Should().BeNull();
    }

    // ── Vérification de rôle ─────────────────────────────────────────────────

    [Fact]
    public void VerifierRole_RoleCorrespondant_NeLevePasException()
    {
        var etape = Etape(1, "Responsable");
        var act = () => WorkflowEngine.VerifierRole(etape, "Responsable");
        act.Should().NotThrow();
    }

    [Fact]
    public void VerifierRole_RoleIncorrect_LeveUnauthorized()
    {
        var etape = Etape(1, "Direction");
        var act = () => WorkflowEngine.VerifierRole(etape, "Responsable");
        act.Should().Throw<UnauthorizedAccessException>()
           .WithMessage("*Direction*");
    }

    [Fact]
    public void VerifierRole_AvecPermission_NeLevePasException()
    {
        var etape = Etape(1, "Direction");
        var permissions = new HashSet<string> { "BESOIN_VALIDER_DIRECTION" };
        var act = () => WorkflowEngine.VerifierRole(etape, "Responsable", permissions);
        act.Should().NotThrow();
    }
}
