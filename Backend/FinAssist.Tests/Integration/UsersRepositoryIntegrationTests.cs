using FinAssist.Application.Services;
using FinAssist.Core.DTOs.Users;
using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;
using FinAssist.Infrastructure.Data;
using FinAssist.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FinAssist.Tests.Integration;

/// <summary>
/// Tests d'intégration pour UsersService + UsersRepository + AppDbContext (InMemory).
/// </summary>
public class UsersRepositoryIntegrationTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    private static UsersService CreateService(AppDbContext db)
    {
        var repo = new UsersRepository(db);
        var pwdMock = new Mock<IPasswordService>();
        pwdMock.Setup(p => p.Hash(It.IsAny<string>())).Returns("hashed_pwd");
        pwdMock.Setup(p => p.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        var permsMock = new Mock<IPermissionsRepository>();
        permsMock.Setup(p => p.SetUtilisateurPermissionsAsync(It.IsAny<int>(), It.IsAny<IEnumerable<int>>()))
                 .Returns(Task.CompletedTask);
        var emailMock = new Mock<IEmailService>();
        emailMock.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                 .ReturnsAsync(true);
        return new UsersService(repo, pwdMock.Object, permsMock.Object, emailMock.Object);
    }

    private static async Task<Role> SeedRoleAsync(AppDbContext db, string code = "Agent")
    {
        var role = new Role { Code = code, DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow };
        db.Roles.Add(role);
        await db.SaveChangesAsync();
        return role;
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_EmailValide_PersistUtilisateurEnBase()
    {
        // Arrange
        using var db = CreateDb(nameof(CreateAsync_EmailValide_PersistUtilisateurEnBase));
        var role = await SeedRoleAsync(db);
        var service = CreateService(db);

        // Act
        var result = await service.CreateAsync(new CreateUtilisateurDTO
        {
            Nom = "Dupont", Prenom = "Jean",
            Email = "jean@finstar-cm.com",
            RoleId = role.Id
        });

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be("jean@finstar-cm.com");
        result.Actif.Should().BeTrue();

        var inDb = await db.Utilisateurs.FirstOrDefaultAsync(u => u.Email == "jean@finstar-cm.com");
        inDb.Should().NotBeNull();
        inDb!.DoitChangerMotDePasse.Should().BeTrue(); // obligatoire à la première connexion
    }

    [Fact]
    public async Task CreateAsync_EmailDuplique_LeveInvalidOperation()
    {
        // Arrange
        using var db = CreateDb(nameof(CreateAsync_EmailDuplique_LeveInvalidOperation));
        var role = await SeedRoleAsync(db);
        var service = CreateService(db);

        await service.CreateAsync(new CreateUtilisateurDTO
        {
            Nom = "Dupont", Prenom = "Jean",
            Email = "jean@finstar-cm.com", RoleId = role.Id
        });

        // Act
        var act = async () => await service.CreateAsync(new CreateUtilisateurDTO
        {
            Nom = "Martin", Prenom = "Paul",
            Email = "jean@finstar-cm.com", RoleId = role.Id
        });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*existe déjà*");
    }

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_Retourne2Utilisateurs()
    {
        // Arrange
        using var db = CreateDb(nameof(GetAllAsync_Retourne2Utilisateurs));
        var role = await SeedRoleAsync(db);
        var service = CreateService(db);

        await service.CreateAsync(new CreateUtilisateurDTO { Nom = "A", Prenom = "A", Email = "a@finstar-cm.com", RoleId = role.Id });
        await service.CreateAsync(new CreateUtilisateurDTO { Nom = "B", Prenom = "B", Email = "b@finstar-cm.com", RoleId = role.Id });

        // Act
        var users = (await service.GetAllAsync()).ToList();

        // Assert
        users.Should().HaveCount(2);
    }

    // ── DeactivateAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task DeactivateAsync_CompteActif_PasseInactif()
    {
        // Arrange
        using var db = CreateDb(nameof(DeactivateAsync_CompteActif_PasseInactif));
        var role = await SeedRoleAsync(db);
        var service = CreateService(db);

        var created = await service.CreateAsync(new CreateUtilisateurDTO
        {
            Nom = "Dupont", Prenom = "Jean",
            Email = "jean@finstar-cm.com", RoleId = role.Id
        });

        // Act
        await service.DeactivateAsync(created.Id);

        // Assert
        var inDb = await db.Utilisateurs.FindAsync(created.Id);
        inDb!.Actif.Should().BeFalse();
    }

    // ── ActivateAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ActivateAsync_CompteDesactive_PasseActif()
    {
        // Arrange
        using var db = CreateDb(nameof(ActivateAsync_CompteDesactive_PasseActif));
        var role = await SeedRoleAsync(db);
        var service = CreateService(db);

        var created = await service.CreateAsync(new CreateUtilisateurDTO
        {
            Nom = "Dupont", Prenom = "Jean",
            Email = "jean@finstar-cm.com", RoleId = role.Id
        });

        await service.DeactivateAsync(created.Id);

        // Act
        await service.ActivateAsync(created.Id);

        // Assert
        var inDb = await db.Utilisateurs.FindAsync(created.Id);
        inDb!.Actif.Should().BeTrue();
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ModifieNom_PersisteLaModification()
    {
        // Arrange
        using var db = CreateDb(nameof(UpdateAsync_ModifieNom_PersisteLaModification));
        var role = await SeedRoleAsync(db);
        var service = CreateService(db);

        var created = await service.CreateAsync(new CreateUtilisateurDTO
        {
            Nom = "Dupont", Prenom = "Jean",
            Email = "jean@finstar-cm.com", RoleId = role.Id
        });

        // Act
        await service.UpdateAsync(created.Id, new UpdateUtilisateurDTO { Nom = "Martin" });

        // Assert
        var inDb = await db.Utilisateurs.FindAsync(created.Id);
        inDb!.Nom.Should().Be("Martin");
    }

    // ── HasActionsAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task HasActionsAsync_SansActions_RetourneFalse()
    {
        // Arrange
        using var db = CreateDb(nameof(HasActionsAsync_SansActions_RetourneFalse));
        var role = await SeedRoleAsync(db);
        var repo = new UsersRepository(db);

        var user = new Utilisateur
        {
            Nom = "Test", Prenom = "User", Email = "test@finstar-cm.com",
            MotDePasse = "hash", RoleId = role.Id, Actif = true,
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow
        };
        db.Utilisateurs.Add(user);
        await db.SaveChangesAsync();

        // Act
        var hasActions = await repo.HasActionsAsync(user.Id);

        // Assert
        hasActions.Should().BeFalse();
    }

    // ── GetLogsAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetLogsAsync_ApresCreation_RetourneLogCreation()
    {
        // Arrange
        using var db = CreateDb(nameof(GetLogsAsync_ApresCreation_RetourneLogCreation));
        var role = await SeedRoleAsync(db);
        var service = CreateService(db);

        var created = await service.CreateAsync(new CreateUtilisateurDTO
        {
            Nom = "Dupont", Prenom = "Jean",
            Email = "jean@finstar-cm.com", RoleId = role.Id
        });

        // Act
        var logs = (await service.GetLogsAsync(created.Id)).ToList();

        // Assert
        logs.Should().HaveCount(1);
        logs[0].Action.Should().Be("CREATION");
    }
}
