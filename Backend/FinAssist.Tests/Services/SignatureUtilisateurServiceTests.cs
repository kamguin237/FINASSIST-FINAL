using FinAssist.Application.Services;
using FinAssist.Core.DTOs.Signatures;
using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;
using FluentAssertions;
using Moq;

namespace FinAssist.Tests.Services;

/// <summary>
/// Tests unitaires pour SignatureUtilisateurService.
/// </summary>
public class SignatureUtilisateurServiceTests
{
    private readonly Mock<ISignatureUtilisateurRepository> _repoMock = new();

    private SignatureUtilisateurService CreateService() =>
        new(_repoMock.Object);

    // ── GetMaSignatureAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetMaSignatureAsync_SignatureExistante_RetourneDTO()
    {
        // Arrange
        var sig = new SignatureUtilisateur
        {
            Id = 1, UtilisateurId = 10, Type = "manuscrite",
            ImageBase64 = "data:image/png;base64,abc",
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow
        };
        _repoMock.Setup(r => r.GetByUtilisateurIdAsync(10)).ReturnsAsync(sig);

        var service = CreateService();

        // Act
        var result = await service.GetMaSignatureAsync(10);

        // Assert
        result.Should().NotBeNull();
        result!.Type.Should().Be("manuscrite");
        result.ImageBase64.Should().Be("data:image/png;base64,abc");
    }

    [Fact]
    public async Task GetMaSignatureAsync_AucuneSignature_RetourneNull()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByUtilisateurIdAsync(10)).ReturnsAsync((SignatureUtilisateur?)null);
        var service = CreateService();

        // Act
        var result = await service.GetMaSignatureAsync(10);

        // Assert
        result.Should().BeNull();
    }

    // ── SauvegarderAsync ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("manuscrite")]
    [InlineData("typographique")]
    [InlineData("upload")]
    public async Task SauvegarderAsync_TypeValide_SauvegardeEtRetourneDTO(string type)
    {
        // Arrange
        var dto = new SaveSignatureUtilisateurDTO { Type = type, ImageBase64 = "data:image/png;base64,abc" };
        var saved = new SignatureUtilisateur
        {
            Id = 1, UtilisateurId = 10, Type = type,
            ImageBase64 = dto.ImageBase64,
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow
        };
        _repoMock.Setup(r => r.SaveAsync(It.IsAny<SignatureUtilisateur>())).ReturnsAsync(saved);

        var service = CreateService();

        // Act
        var result = await service.SauvegarderAsync(10, dto);

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be(type);
        _repoMock.Verify(r => r.SaveAsync(It.Is<SignatureUtilisateur>(s =>
            s.UtilisateurId == 10 && s.Type == type)), Times.Once);
    }

    [Fact]
    public async Task SauvegarderAsync_ImageVide_LeveArgumentException()
    {
        // Arrange
        var dto = new SaveSignatureUtilisateurDTO { Type = "manuscrite", ImageBase64 = "" };
        var service = CreateService();

        // Act
        var act = async () => await service.SauvegarderAsync(10, dto);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*obligatoire*");
    }

    [Fact]
    public async Task SauvegarderAsync_TypeInvalide_LeveArgumentException()
    {
        // Arrange
        var dto = new SaveSignatureUtilisateurDTO { Type = "invalide", ImageBase64 = "data:image/png;base64,abc" };
        var service = CreateService();

        // Act
        var act = async () => await service.SauvegarderAsync(10, dto);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*invalide*");
    }

    [Fact]
    public async Task SauvegarderAsync_AvecPolice_StockePolice()
    {
        // Arrange
        var dto = new SaveSignatureUtilisateurDTO
        {
            Type = "typographique",
            ImageBase64 = "data:image/png;base64,abc",
            Police = "Dancing Script"
        };
        var saved = new SignatureUtilisateur
        {
            Id = 1, UtilisateurId = 10, Type = "typographique",
            ImageBase64 = dto.ImageBase64, Police = "Dancing Script",
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow
        };
        _repoMock.Setup(r => r.SaveAsync(It.IsAny<SignatureUtilisateur>())).ReturnsAsync(saved);

        var service = CreateService();

        // Act
        var result = await service.SauvegarderAsync(10, dto);

        // Assert
        result.Police.Should().Be("Dancing Script");
        _repoMock.Verify(r => r.SaveAsync(It.Is<SignatureUtilisateur>(s =>
            s.Police == "Dancing Script")), Times.Once);
    }

    // ── SupprimerAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task SupprimerAsync_AppelleRepository()
    {
        // Arrange
        _repoMock.Setup(r => r.DeleteAsync(10)).Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        await service.SupprimerAsync(10);

        // Assert
        _repoMock.Verify(r => r.DeleteAsync(10), Times.Once);
    }
}
