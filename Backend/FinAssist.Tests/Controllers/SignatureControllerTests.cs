using System.Security.Claims;
using FinAssist.API.Controllers;
using FinAssist.Core.DTOs.Signature;
using FinAssist.Core.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace FinAssist.Tests.Controllers;

/// <summary>
/// Tests unitaires pour SignatureController — signature de documents et vérification.
/// </summary>
public class SignatureControllerTests
{
    // ── Setup ─────────────────────────────────────────────────────────────────

    private readonly Mock<ISignatureService>          _serviceMock = new();
    private readonly Mock<ILogger<SignatureController>> _loggerMock = new();

    private SignatureController CreateController(int userId = 1)
    {
        var controller = new SignatureController(_serviceMock.Object, _loggerMock.Object);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, "Responsable")
        };
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
            }
        };
        return controller;
    }

    private static SignatureDTO SignatureDTO() => new()
    {
        Id = 1, DocumentId = 1, UtilisateurId = 1,
        Horodatage = DateTime.UtcNow, Valide = true,
        Valeur = "sig_value", Empreinte = "hash_abc"
    };

    // ── Signer ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Signer_DocumentExistant_Retourne200()
    {
        // Arrange
        _serviceMock.Setup(s => s.SignerAsync(1, 1)).ReturnsAsync(SignatureDTO());
        var controller = CreateController();

        // Act
        var result = await controller.Signer(1);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Signer_DocumentInexistant_Retourne404()
    {
        // Arrange
        _serviceMock.Setup(s => s.SignerAsync(99, 1))
            .ThrowsAsync(new KeyNotFoundException("Document 99 introuvable."));
        var controller = CreateController();

        // Act
        var result = await controller.Signer(99);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Signer_DejaSigné_Retourne409()
    {
        // Arrange
        _serviceMock.Setup(s => s.SignerAsync(1, 1))
            .ThrowsAsync(new InvalidOperationException("Document déjà signé."));
        var controller = CreateController();

        // Act
        var result = await controller.Signer(1);

        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
    }

    // ── SignerParBesoin ───────────────────────────────────────────────────────

    [Fact]
    public async Task SignerParBesoin_BesoinExistant_Retourne200()
    {
        // Arrange
        _serviceMock.Setup(s => s.SignerParBesoinAsync(1, 1, It.IsAny<SignerBesoinDTO?>()))
            .ReturnsAsync(SignatureDTO());
        var controller = CreateController();

        // Act
        var result = await controller.SignerParBesoin(1, new SignerBesoinDTO { DocumentId = 1 });

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task SignerParBesoin_BesoinInexistant_Retourne404()
    {
        // Arrange
        _serviceMock.Setup(s => s.SignerParBesoinAsync(99, 1, It.IsAny<SignerBesoinDTO?>()))
            .ThrowsAsync(new KeyNotFoundException("Besoin 99 introuvable."));
        var controller = CreateController();

        // Act
        var result = await controller.SignerParBesoin(99, null);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SignerParBesoin_SignatureRequiseNonRemplie_Retourne409()
    {
        // Arrange
        _serviceMock.Setup(s => s.SignerParBesoinAsync(1, 1, It.IsAny<SignerBesoinDTO?>()))
            .ThrowsAsync(new InvalidOperationException("Signature requise non fournie."));
        var controller = CreateController();

        // Act
        var result = await controller.SignerParBesoin(1, null);

        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
    }

    // ── GetByBesoin ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByBesoin_BesoinExistant_Retourne200()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetApercuAsync(1)).ReturnsAsync(new SignatureApercuDTO
        {
            Id = 1, Valide = true, Empreinte = "abc123",
            Horodatage = DateTime.UtcNow
        });
        var controller = CreateController();

        // Act
        var result = await controller.GetByBesoin(1);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByBesoin_BesoinInexistant_Retourne404()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetApercuAsync(99))
            .ThrowsAsync(new KeyNotFoundException("Besoin 99 introuvable."));
        var controller = CreateController();

        // Act
        var result = await controller.GetByBesoin(99);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ── TelechargerDocumentSigne ──────────────────────────────────────────────

    [Fact]
    public async Task TelechargerDocumentSigne_SignatureExistante_RetournePdf()
    {
        // Arrange
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF
        _serviceMock.Setup(s => s.GenererDocumentSigneAsync(1))
            .ReturnsAsync((pdfBytes, "document_signe.pdf"));
        var controller = CreateController();

        // Act
        var result = await controller.TelechargerDocumentSigne(1);

        // Assert
        var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
        fileResult.ContentType.Should().Be("application/pdf");
        fileResult.FileContents.Should().BeEquivalentTo(pdfBytes);
    }

    [Fact]
    public async Task TelechargerDocumentSigne_SignatureInexistante_Retourne404()
    {
        // Arrange
        _serviceMock.Setup(s => s.GenererDocumentSigneAsync(99))
            .ThrowsAsync(new KeyNotFoundException("Signature 99 introuvable."));
        var controller = CreateController();

        // Act
        var result = await controller.TelechargerDocumentSigne(99);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ── Verifier ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Verifier_SignatureValide_Retourne200()
    {
        // Arrange
        _serviceMock.Setup(s => s.VerifierAsync(1)).ReturnsAsync(new VerificationDTO
        {
            SignatureId = 1, Authentique = true, Message = "Signature valide.",
            Horodatage = DateTime.UtcNow
        });
        var controller = CreateController();

        // Act
        var result = await controller.Verifier(1);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<VerificationDTO>()
            .Which.Authentique.Should().BeTrue();
    }

    [Fact]
    public async Task Verifier_SignatureInexistante_Retourne404()
    {
        // Arrange
        _serviceMock.Setup(s => s.VerifierAsync(99))
            .ThrowsAsync(new KeyNotFoundException("Signature 99 introuvable."));
        var controller = CreateController();

        // Act
        var result = await controller.Verifier(99);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
