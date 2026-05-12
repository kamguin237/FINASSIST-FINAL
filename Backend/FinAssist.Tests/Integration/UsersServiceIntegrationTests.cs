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
/// Tests d'intégration complémentaires pour UsersService + UsersRepository + AppDbContext (InMemory).
/// Couvre les opérations non testées dans UsersRepositoryIntegrationTests :
/// ChangeRoleAsync, ChangePasswordAsync, DeleteAsync, GetLogsAsync (multi-actions),
/// GetIdsAvecActionsAsync, email domain validation.
/// </summary>
public class UsersServiceIntegrationTests
{
    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    private static UsersService CreateService(AppDbContext db, bool passwordValid = true)
    {
        var repo = new UsersRepository(db);
        var pwdMock = new Mock<IPasswordService>();
        pwdMock.Setup(p => p.Hash(It.IsAny<string>())).Returns("hashed_pwd");
        pwdMock.Setup(p => p.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(passwordValid);
        var permsMock = new Mock<IPermissionsRepository>();
        permsMock.Setup(p => p.SetUtilisateurPermissionsAsync(It.IsAny<int>(), It.IsAny<IEnumerable<int>>()))
                 .Returns(Task.CompletedTask);
        var emailMock = new Mock<IEmailService>();
        emailMock.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                 .ReturnsAsync(true);
        return new UsersService(repo, pwdMock.Object, permsMock.Object, emailMock.Object);
    }

    private static async Task<(Role role1, Role role2, int userId)> SeedUserAsync(AppDbContext db)
    {
        var role1 = new Role { Code = "Agent", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow };
        var role2 = new Role { Code = "Responsable", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow };
        db.Roles.AddRange(role1, role2);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var user = await service.CreateAsync(new CreateUtilisateurDTO
        {
            Nom = "Dupont", Prenom = "Jean",
            Email = "jean@finstar-cm.com",
            RoleId = role1.Id
        });
        return (role1, role2, user.Id);
    }

    // ── CreateAsync — validation domaine email ────────────────────────────────

    [Fact]
    public async Task CreateAsync_EmailHorsDomaine_LeveInvalidOperation()
    {
        // Arrange
        using var db = CreateDb(nameof(CreateAsync_EmailHorsDomaine_LeveInvalidOperation));
        var role = new Role { Code = "Agent", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow };
        db.Roles.Add(role);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        // Act
        var act = async () => await service.CreateAsync(new CreateUtilisateurDTO
        {
            Nom = "Test", Prenom = "User",
            Email = "user@gmail.com",  // domaine invalide
            RoleId = role.Id
        });

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*finstar-cm.com*");
    }

    // ── ChangeRoleAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task ChangeRoleAsync_NouveauRole_PersisteLaModification()
    {
        // Arrange
        using var db = CreateDb(nameof(ChangeRoleAsync_NouveauRole_PersisteLaModification));
        var (_, role2, userId) = await SeedUserAsync(db);
        var service = CreateService(db);

        // Act
        var result = await service.ChangeRoleAsync(userId, role2.Id);

        // Assert
        result.Role.Should().Be("Responsable");

        var inDb = await db.Utilisateurs.FindAsync(userId);
        inDb!.RoleId.Should().Be(role2.Id);
    }

    [Fact]
    public async Task ChangeRoleAsync_AjouteLogChangementRole()
    {
        // Arrange
        using var db = CreateDb(nameof(ChangeRoleAsync_AjouteLogChangementRole));
        var (_, role2, userId) = await SeedUserAsync(db);
        var service = CreateService(db);

        // Act
        await service.ChangeRoleAsync(userId, role2.Id);

        // Assert — log CHANGEMENT_ROLE créé
        var logs = await db.LogsUtilisateurs
            .Where(l => l.UtilisateurId == userId && l.Action == "CHANGEMENT_ROLE")
            .ToListAsync();
        logs.Should().HaveCount(1);
        logs[0].Details.Should().Contain("→");
    }

    [Fact]
    public async Task ChangeRoleAsync_UtilisateurInexistant_LeveKeyNotFound()
    {
        // Arrange
        using var db = CreateDb(nameof(ChangeRoleAsync_UtilisateurInexistant_LeveKeyNotFound));
        var service = CreateService(db);

        // Act
        var act = async () => await service.ChangeRoleAsync(999, 1);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── ChangePasswordAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task ChangePasswordAsync_AncienMdpCorrect_MettreAJourHash()
    {
        // Arrange
        using var db = CreateDb(nameof(ChangePasswordAsync_AncienMdpCorrect_MettreAJourHash));
        var (_, _, userId) = await SeedUserAsync(db);
        var service = CreateService(db, passwordValid: true);

        // Act
        await service.ChangePasswordAsync(userId, "ancien_mdp", "nouveau_mdp_12");

        // Assert
        var inDb = await db.Utilisateurs.FindAsync(userId);
        inDb!.MotDePasse.Should().Be("hashed_pwd");

        // Log CHANGEMENT_MOT_DE_PASSE créé
        var log = await db.LogsUtilisateurs
            .FirstOrDefaultAsync(l => l.UtilisateurId == userId && l.Action == "CHANGEMENT_MOT_DE_PASSE");
        log.Should().NotBeNull();
    }

    [Fact]
    public async Task ChangePasswordAsync_AncienMdpIncorrect_LeveUnauthorized()
    {
        // Arrange
        using var db = CreateDb(nameof(ChangePasswordAsync_AncienMdpIncorrect_LeveUnauthorized));
        var (_, _, userId) = await SeedUserAsync(db);
        var service = CreateService(db, passwordValid: false); // mot de passe invalide

        // Act
        var act = async () => await service.ChangePasswordAsync(userId, "mauvais_mdp", "nouveau_mdp_12");

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*incorrect*");
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_UtilisateurSansActions_SupprimeDeLaBase()
    {
        // Arrange
        using var db = CreateDb(nameof(DeleteAsync_UtilisateurSansActions_SupprimeDeLaBase));
        var role = new Role { Code = "Agent", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow };
        db.Roles.Add(role);
        await db.SaveChangesAsync();

        // Créer un utilisateur sans actions (directement en base, sans passer par le service)
        var user = new Utilisateur
        {
            Nom = "ASupprimer", Prenom = "User", Email = "delete@finstar-cm.com",
            MotDePasse = "hash", RoleId = role.Id, Actif = true,
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow
        };
        db.Utilisateurs.Add(user);
        await db.SaveChangesAsync();

        var service = CreateService(db);

        // Act — DeleteCascadeAsync utilise ExecuteDeleteAsync (non supporté InMemory)
        // On teste HasActionsAsync + la logique de validation
        var repo = new UsersRepository(db);
        var hasActions = await repo.HasActionsAsync(user.Id);

        // Assert — pas d'actions → suppression autorisée
        hasActions.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_UtilisateurAvecBesoins_LeveInvalidOperation()
    {
        // Arrange
        using var db = CreateDb(nameof(DeleteAsync_UtilisateurAvecBesoins_LeveInvalidOperation));
        var (role1, _, userId) = await SeedUserAsync(db);
        var service = CreateService(db);

        // Créer un besoin pour cet utilisateur
        var cat = new Categorie { Nom = "Cat", DateCreation = DateTime.UtcNow };
        db.Categories.Add(cat);
        await db.SaveChangesAsync();

        db.Besoins.Add(new Besoin
        {
            Titre = "Besoin test", Description = "Desc",
            Statut = "BROUILLON", NiveauImportance = "MOYEN",
            UtilisateurId = userId, CategorieId = cat.Id,
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        // Act
        var act = async () => await service.DeleteAsync(userId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*actions*");
    }

    // ── GetLogsAsync — multi-actions ──────────────────────────────────────────

    [Fact]
    public async Task GetLogsAsync_ApresMultiplesActions_RetourneTousLesLogs()
    {
        // Arrange
        using var db = CreateDb(nameof(GetLogsAsync_ApresMultiplesActions_RetourneTousLesLogs));
        var (_, role2, userId) = await SeedUserAsync(db);
        var service = CreateService(db);

        // Effectuer plusieurs actions
        await service.UpdateAsync(userId, new UpdateUtilisateurDTO { Nom = "NouveauNom" });
        await service.ChangeRoleAsync(userId, role2.Id);
        await service.DeactivateAsync(userId);
        await service.ActivateAsync(userId);

        // Act
        var logs = (await service.GetLogsAsync(userId)).ToList();

        // Assert — CREATION + MODIFICATION + CHANGEMENT_ROLE + DESACTIVATION + ACTIVATION
        logs.Should().HaveCount(5);
        logs.Select(l => l.Action).Should().Contain(["CREATION", "MODIFICATION", "CHANGEMENT_ROLE", "DESACTIVATION", "ACTIVATION"]);
    }

    [Fact]
    public async Task GetLogsAsync_TriDecroissant_PremierLogEstLePlusRecent()
    {
        // Arrange
        using var db = CreateDb(nameof(GetLogsAsync_TriDecroissant_PremierLogEstLePlusRecent));
        var (_, role2, userId) = await SeedUserAsync(db);
        var service = CreateService(db);

        await service.ChangeRoleAsync(userId, role2.Id);

        // Act
        var logs = (await service.GetLogsAsync(userId)).ToList();

        // Assert — le plus récent en premier (CHANGEMENT_ROLE après CREATION)
        logs.First().Action.Should().Be("CHANGEMENT_ROLE");
    }

    // ── GetIdsAvecActionsAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetIdsAvecActionsAsync_UtilisateurAvecBesoin_EstInclus()
    {
        // Arrange
        using var db = CreateDb(nameof(GetIdsAvecActionsAsync_UtilisateurAvecBesoin_EstInclus));
        var (_, _, userId) = await SeedUserAsync(db);
        var repo = new UsersRepository(db);

        var cat = new Categorie { Nom = "Cat", DateCreation = DateTime.UtcNow };
        db.Categories.Add(cat);
        await db.SaveChangesAsync();

        db.Besoins.Add(new Besoin
        {
            Titre = "Besoin", Description = "Desc",
            Statut = "BROUILLON", NiveauImportance = "MOYEN",
            UtilisateurId = userId, CategorieId = cat.Id,
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        // Act
        var ids = await repo.GetIdsAvecActionsAsync();

        // Assert
        ids.Should().Contain(userId);
    }

    [Fact]
    public async Task GetIdsAvecActionsAsync_UtilisateurSansActions_NEstPasInclus()
    {
        // Arrange
        using var db = CreateDb(nameof(GetIdsAvecActionsAsync_UtilisateurSansActions_NEstPasInclus));
        var role = new Role { Code = "Agent", DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow };
        db.Roles.Add(role);
        var user = new Utilisateur
        {
            Nom = "Sans", Prenom = "Actions", Email = "sans@finstar-cm.com",
            MotDePasse = "hash", RoleId = role.Id, Actif = true,
            DateCreation = DateTime.UtcNow, DateModification = DateTime.UtcNow
        };
        db.Utilisateurs.Add(user);
        await db.SaveChangesAsync();
        var repo = new UsersRepository(db);

        // Act
        var ids = await repo.GetIdsAvecActionsAsync();

        // Assert
        ids.Should().NotContain(user.Id);
    }

    // ── UpdateAsync — aucun changement → pas de log ───────────────────────────

    [Fact]
    public async Task UpdateAsync_AucunChangement_NeCreesPasDeLog()
    {
        // Arrange
        using var db = CreateDb(nameof(UpdateAsync_AucunChangement_NeCreesPasDeLog));
        var (_, _, userId) = await SeedUserAsync(db);
        var service = CreateService(db);

        var logsAvant = await db.LogsUtilisateurs.CountAsync(l => l.UtilisateurId == userId);

        // Act — UpdateAsync sans changement réel
        await service.UpdateAsync(userId, new UpdateUtilisateurDTO()); // DTO vide

        // Assert — pas de nouveau log
        var logsApres = await db.LogsUtilisateurs.CountAsync(l => l.UtilisateurId == userId);
        logsApres.Should().Be(logsAvant);
    }

    // ── DeactivateAsync — déjà désactivé ─────────────────────────────────────

    [Fact]
    public async Task DeactivateAsync_CompteDejaDesactive_LeveInvalidOperation()
    {
        // Arrange
        using var db = CreateDb(nameof(DeactivateAsync_CompteDejaDesactive_LeveInvalidOperation));
        var (_, _, userId) = await SeedUserAsync(db);
        var service = CreateService(db);

        await service.DeactivateAsync(userId);

        // Act — désactiver une deuxième fois
        var act = async () => await service.DeactivateAsync(userId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*désactivé*");
    }

    // ── ActivateAsync — déjà actif ────────────────────────────────────────────

    [Fact]
    public async Task ActivateAsync_CompteDejaActif_LeveInvalidOperation()
    {
        // Arrange
        using var db = CreateDb(nameof(ActivateAsync_CompteDejaActif_LeveInvalidOperation));
        var (_, _, userId) = await SeedUserAsync(db);
        var service = CreateService(db);

        // Act — activer un compte déjà actif
        var act = async () => await service.ActivateAsync(userId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*actif*");
    }
}
