using FinAssist.API.Controllers;
using FinAssist.Core.DTOs.Permissions;
using FinAssist.Core.DTOs.Roles;
using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FinAssist.Tests.Controllers;

/// <summary>
/// Tests unitaires pour PermissionsController et RolesController.
/// </summary>
public class PermissionsControllerTests
{
    private readonly Mock<IPermissionsRepository> _repoMock = new();

    private PermissionsController CreateController() => new(_repoMock.Object);

    private static Permission Perm(int id = 1) => new()
    {
        Id = id, Code = $"PERM_{id}", Description = "Test",
        Fonctionnalite = "Besoins", Module = "Core",
        DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow
    };

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_RetourneListePermissions()
    {
        // Arrange
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Permission> { Perm(1), Perm(2) });
        var controller = CreateController();

        // Act
        var result = await controller.GetAll();

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IEnumerable<PermissionDTO>>()
            .Which.Should().HaveCount(2);
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_PermissionExistante_Retourne200()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(Perm(1));
        var controller = CreateController();

        // Act
        var result = await controller.GetById(1);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<PermissionDTO>()
            .Which.Code.Should().Be("PERM_1");
    }

    [Fact]
    public async Task GetById_PermissionInexistante_Retourne404()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Permission?)null);
        var controller = CreateController();

        // Act
        var result = await controller.GetById(99);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_PermissionExistante_Retourne200()
    {
        // Arrange
        var perm = Perm(1);
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(perm);
        _repoMock.Setup(r => r.ExistsAsync("PERM_1")).ReturnsAsync(false);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Permission>())).ReturnsAsync(new Permission());
        var controller = CreateController();

        // Act
        var result = await controller.Update(1, new CreatePermissionDTO
        {
            Code = "PERM_1_UPDATED", Description = "Updated", Fonctionnalite = "X", Module = "Y"
        });

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _repoMock.Verify(r => r.UpdateAsync(It.Is<Permission>(p => p.Code == "PERM_1_UPDATED")), Times.Once);
    }

    [Fact]
    public async Task Update_CodeDuplique_Retourne409()
    {
        // Arrange
        var perm = Perm(1);
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(perm);
        _repoMock.Setup(r => r.ExistsAsync("PERM_EXISTANTE")).ReturnsAsync(true);
        var controller = CreateController();

        // Act
        var result = await controller.Update(1, new CreatePermissionDTO
        {
            Code = "PERM_EXISTANTE", Description = "X", Fonctionnalite = "X", Module = "Y"
        });

        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
    }
}

public class RolesControllerTests
{
    private readonly Mock<IRolesRepository>       _rolesRepoMock = new();
    private readonly Mock<IPermissionsRepository> _permsRepoMock = new();

    private RolesController CreateController() =>
        new(_rolesRepoMock.Object, _permsRepoMock.Object);

    private static Role RoleEntity(int id = 1) => new()
    {
        Id = id, Code = $"Role{id}", Description = "Test",
        DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow
    };

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_RetourneListeRoles()
    {
        // Arrange
        _rolesRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Role> { RoleEntity(1), RoleEntity(2) });
        var controller = CreateController();

        // Act
        var result = await controller.GetAll();

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IEnumerable<RoleDTO>>()
            .Which.Should().HaveCount(2);
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_RoleExistant_Retourne200()
    {
        // Arrange
        _rolesRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(RoleEntity(1));
        var controller = CreateController();

        // Act
        var result = await controller.GetById(1);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_RoleInexistant_Retourne404()
    {
        // Arrange
        _rolesRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Role?)null);
        var controller = CreateController();

        // Act
        var result = await controller.GetById(99);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_CodeUnique_Retourne201()
    {
        // Arrange
        _rolesRepoMock.Setup(r => r.ExistsAsync("NouveauRole")).ReturnsAsync(false);
        _rolesRepoMock.Setup(r => r.CreateAsync(It.IsAny<Role>())).ReturnsAsync(RoleEntity(3));
        var controller = CreateController();

        // Act
        var result = await controller.Create(new CreateRoleDTO { Code = "NouveauRole", Description = "Desc" });

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task Create_CodeDuplique_Retourne409()
    {
        // Arrange
        _rolesRepoMock.Setup(r => r.ExistsAsync("ExistantRole")).ReturnsAsync(true);
        var controller = CreateController();

        // Act
        var result = await controller.Create(new CreateRoleDTO { Code = "ExistantRole", Description = "Desc" });

        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_RoleExistant_Retourne204()
    {
        // Arrange
        _rolesRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(RoleEntity(1));
        _rolesRepoMock.Setup(r => r.DeleteAsync(1)).Returns(Task.CompletedTask);
        var controller = CreateController();

        // Act
        var result = await controller.Delete(1);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_RoleInexistant_Retourne404()
    {
        // Arrange
        _rolesRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Role?)null);
        var controller = CreateController();

        // Act
        var result = await controller.Delete(99);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ── GetPermissions ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPermissions_RoleExistant_RetournePermissions()
    {
        // Arrange
        _rolesRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(RoleEntity(1));
        _permsRepoMock.Setup(r => r.GetPermissionsByRoleAsync(1)).ReturnsAsync(new List<Permission>
        {
            new() { Id = 1, Code = "BESOIN_CONSULTER", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow }
        });
        var controller = CreateController();

        // Act
        var result = await controller.GetPermissions(1);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IEnumerable<PermissionDTO>>()
            .Which.Should().HaveCount(1);
    }

    // ── SetPermissions ────────────────────────────────────────────────────────

    [Fact]
    public async Task SetPermissions_RoleExistant_Retourne200()
    {
        // Arrange
        _rolesRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(RoleEntity(1));
        _permsRepoMock.Setup(r => r.SetRolePermissionsAsync(1, It.IsAny<IEnumerable<int>>())).Returns(Task.CompletedTask);
        var controller = CreateController();

        // Act
        var result = await controller.SetPermissions(1, new AssignerPermissionsDTO { PermissionIds = [1, 2, 3] });

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _permsRepoMock.Verify(r => r.SetRolePermissionsAsync(1, It.IsAny<IEnumerable<int>>()), Times.Once);
    }
}
