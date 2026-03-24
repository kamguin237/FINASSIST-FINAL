using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FinAssist.Infrastructure.Data;

public static class DataSeeder
{
    private static readonly (string Code, string Description, string Fonctionnalite, string Module)[] PermissionsSeed =
    [
        // Besoins
        ("BESOIN_CREER",     "Créer un besoin",          "Création d'un besoin",          "Besoins"),
        ("BESOIN_MODIFIER",  "Modifier un besoin",       "Modification d'un besoin",      "Besoins"),
        ("BESOIN_SUPPRIMER", "Supprimer un besoin",      "Suppression d'un besoin",       "Besoins"),
        ("BESOIN_CONSULTER", "Consulter les besoins",    "Consultation des besoins",      "Besoins"),
        ("BESOIN_VALIDER",   "Valider un besoin",        "Validation d'un besoin",        "Besoins"),
        ("BESOIN_SIGNER",    "Signer un besoin",         "Signature d'un besoin",         "Besoins"),
        // Utilisateurs
        ("USER_CREER",       "Créer un utilisateur",     "Création d'un utilisateur",     "Utilisateurs"),
        ("USER_MODIFIER",    "Modifier un utilisateur",  "Modification d'un utilisateur", "Utilisateurs"),
        ("USER_SUPPRIMER",   "Supprimer un utilisateur", "Suppression d'un utilisateur",  "Utilisateurs"),
        ("USER_CONSULTER",   "Consulter les utilisateurs","Consultation des utilisateurs","Utilisateurs"),
        // Rôles
        ("ROLE_CREER",       "Créer un rôle",            "Création d'un rôle",            "Rôles"),
        ("ROLE_MODIFIER",    "Modifier un rôle",         "Modification d'un rôle",        "Rôles"),
        ("ROLE_SUPPRIMER",   "Supprimer un rôle",        "Suppression d'un rôle",         "Rôles"),
        ("ROLE_CONSULTER",   "Consulter les rôles",      "Consultation des rôles",        "Rôles"),
        // Permissions
        ("PERMISSION_CREER",    "Créer une permission",    "Création d'une permission",    "Permissions"),
        ("PERMISSION_MODIFIER", "Modifier une permission", "Modification d'une permission","Permissions"),
        ("PERMISSION_SUPPRIMER","Supprimer une permission","Suppression d'une permission", "Permissions"),
        ("PERMISSION_CONSULTER","Consulter les permissions","Consultation des permissions","Permissions"),
        // Workflow
        ("WORKFLOW_CREER",      "Créer un circuit",         "Création d'un circuit",        "Workflow"),
        ("WORKFLOW_MODIFIER",   "Modifier un circuit",      "Modification d'un circuit",    "Workflow"),
        ("WORKFLOW_SUPPRIMER",  "Supprimer un circuit",     "Suppression d'un circuit",     "Workflow"),
        ("WORKFLOW_CONSULTER",  "Consulter les circuits",   "Consultation des circuits",    "Workflow"),
        ("WORKFLOW_CONFIGURER", "Configurer un circuit",    "Configuration d'un circuit",   "Workflow"),
        // Catégories
        ("CATEGORIE_CREER",     "Créer une catégorie",      "Création d'une catégorie",     "Catégories"),
        ("CATEGORIE_MODIFIER",  "Modifier une catégorie",   "Modification d'une catégorie", "Catégories"),
        ("CATEGORIE_SUPPRIMER", "Supprimer une catégorie",  "Suppression d'une catégorie",  "Catégories"),
        ("CATEGORIE_CONSULTER", "Consulter les catégories", "Consultation des catégories",  "Catégories"),
        // Reporting
        ("RAPPORT_CONSULTER",   "Consulter les rapports",   "Consultation des rapports",    "Reporting"),
        ("RAPPORT_EXPORTER",    "Exporter les rapports",    "Export des rapports",          "Reporting"),
        ("DASHBOARD_CONSULTER", "Consulter le dashboard",   "Consultation du dashboard",    "Reporting"),
        // Logs
        ("LOG_CONSULTER",       "Consulter les logs",       "Consultation des logs",        "Logs"),
        // Notifications
        ("NOTIFICATION_LIRE",   "Lire les notifications",   "Lecture des notifications",    "Notifications"),
        ("NOTIFICATION_ENVOYER","Envoyer des notifications","Envoi de notifications",       "Notifications"),
    ];

    // C’est la méthode appelée au démarrage
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();// crée un scope DI (Dependency Injection) qui permet d’utiliser les services
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); // Récupère le DbContext (base de données)
        var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>(); // Récupère le service de hash du mot de passe

        await db.Database.MigrateAsync();// applique toutes les migrations, crée la base si elle n’existe pas, met à jour les tables


        // ── Rôles ────────────────────────────────────────────────────────────
        if (!await db.Roles.AnyAsync())
        {
            db.Roles.AddRange(
                new Role { Code = "Administrateur", Description = "Accès complet au système" },
                new Role { Code = "Direction",      Description = "Accès aux tableaux de bord et rapports" },
                new Role { Code = "Responsable",    Description = "Gestion des agents et validation" },
                new Role { Code = "Agent",          Description = "Saisie et traitement des réclamations" }
            );
            await db.SaveChangesAsync();
        }

        // ── Admin ────────────────────────────────────────────────────────────
        if (!await db.Utilisateurs.AnyAsync(u => u.Email == "admin@finassist.com"))
        {
            var adminRole = await db.Roles.FirstAsync(r => r.Code == "Administrateur");
            db.Utilisateurs.Add(new Utilisateur
            {
                Nom = "Admin",
                Prenom = "FinAssist",
                Email = "admin@finassist.com",
                MotDePasse = passwordService.Hash("Admin@1234"),
                RoleId = adminRole.Id,
                Actif = true
            });
            await db.SaveChangesAsync();
        }

        // ── Permissions ──────────────────────────────────────────────────────
        foreach (var (code, desc, fonc, module) in PermissionsSeed)
        {
            if (!await db.Permissions.AnyAsync(p => p.Code == code))
            {
                db.Permissions.Add(new Permission
                {
                    Code = code,
                    Description = desc,
                    Fonctionnalite = fonc,
                    Module = module
                });
            }
        }
        await db.SaveChangesAsync();

        // ── Administrateur → toutes les permissions ──────────────────────────
        var adminRoleEntity = await db.Roles.FirstAsync(r => r.Code == "Administrateur");
        var allPermissions = await db.Permissions.ToListAsync();
        foreach (var perm in allPermissions)
        {
            if (!await db.RolePermissions.AnyAsync(rp => rp.RoleId == adminRoleEntity.Id && rp.PermissionId == perm.Id))
                db.RolePermissions.Add(new RolePermission { RoleId = adminRoleEntity.Id, PermissionId = perm.Id });
        }
        await db.SaveChangesAsync();
    }
}
