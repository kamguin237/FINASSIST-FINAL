using FinAssist.API.Controllers;
using FinAssist.Core.DTOs.Logs;
using FinAssist.Core.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FinAssist.Tests.Controllers;

/// <summary>
/// Tests unitaires pour LogsController — consultation paginée des logs.
/// </summary>
public class LogsControllerTests
{
    // ── Setup ─────────────────────────────────────────────────────────────────

    private static LogsController CreateController(Mock<ILogService> serviceMock)
        => new(serviceMock.Object);

    // ── GetLogs ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetLogs_RetourneLogsAvecTotal()
    {
        // Arrange
        var serviceMock = new Mock<ILogService>();
        var logs = new List<LogActiviteDTO>
        {
            new() { Id = 1, Action = "CREATION", EntiteType = "Besoin", Date = DateTime.UtcNow },
            new() { Id = 2, Action = "MODIFICATION", EntiteType = "Utilisateur", Date = DateTime.UtcNow }
        };
        serviceMock.Setup(s => s.GetLogsAsync(It.IsAny<FiltreLogsDTO>())).ReturnsAsync((logs, 2));
        var controller = CreateController(serviceMock);

        // Act
        var result = await controller.GetLogs(new FiltreLogsDTO { Page = 1, PageSize = 20 });

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = ok.Value!;
        var type = value.GetType();
        type.GetProperty("total")?.GetValue(value).Should().Be(2);
        type.GetProperty("page")?.GetValue(value).Should().Be(1);
        type.GetProperty("pageSize")?.GetValue(value).Should().Be(20);
    }

    [Fact]
    public async Task GetLogs_AucunLog_RetourneListeVide()
    {
        // Arrange
        var serviceMock = new Mock<ILogService>();
        serviceMock.Setup(s => s.GetLogsAsync(It.IsAny<FiltreLogsDTO>()))
            .ReturnsAsync((new List<LogActiviteDTO>(), 0));
        var controller = CreateController(serviceMock);

        // Act
        var result = await controller.GetLogs(new FiltreLogsDTO());

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = ok.Value!;
        value.GetType().GetProperty("total")?.GetValue(value).Should().Be(0);
    }

    [Fact]
    public async Task GetLogs_PasseFiltresAuService()
    {
        // Arrange
        var serviceMock = new Mock<ILogService>();
        serviceMock.Setup(s => s.GetLogsAsync(It.IsAny<FiltreLogsDTO>()))
            .ReturnsAsync((new List<LogActiviteDTO>(), 0));
        var controller = CreateController(serviceMock);
        var filtres = new FiltreLogsDTO { Page = 2, PageSize = 10, Action = "CREATION" };

        // Act
        await controller.GetLogs(filtres);

        // Assert
        serviceMock.Verify(s => s.GetLogsAsync(It.Is<FiltreLogsDTO>(f =>
            f.Page == 2 && f.PageSize == 10 && f.Action == "CREATION")), Times.Once);
    }

    [Fact]
    public async Task GetLogs_RetourneItemsDansReponse()
    {
        // Arrange
        var serviceMock = new Mock<ILogService>();
        var logs = new List<LogActiviteDTO>
        {
            new() { Id = 1, Action = "CONNEXION", EntiteType = "auth", Date = DateTime.UtcNow }
        };
        serviceMock.Setup(s => s.GetLogsAsync(It.IsAny<FiltreLogsDTO>())).ReturnsAsync((logs, 1));
        var controller = CreateController(serviceMock);

        // Act
        var result = await controller.GetLogs(new FiltreLogsDTO());

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = ok.Value!;
        var items = value.GetType().GetProperty("items")?.GetValue(value) as IEnumerable<LogActiviteDTO>;
        items.Should().NotBeNull();
        items!.Should().HaveCount(1);
        items.First().Action.Should().Be("CONNEXION");
    }
}
