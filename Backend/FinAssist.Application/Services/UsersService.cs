using FinAssist.Core.DTOs.Users;
using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;

namespace FinAssist.Application.Services;

public class UsersService(
    IUsersRepository usersRepo,
    IPasswordService passwordService,
    IPermissionsRepository permissionsRepo,
    IEmailService emailService) : IUsersService
{
    public async Task<IEnumerable<UtilisateurDTO>> GetAllAsync()
    {
        var users = (await usersRepo.GetAllAsync()).ToList();

        // Récupérer en une seule requête les IDs qui ont des actions
        var idsAvecActions = await usersRepo.GetIdsAvecActionsAsync();

        return users.Select(u =>
        {
            var dto = ToDTO(u);
            dto.HasActions = idsAvecActions.Contains(u.Id);
            return dto;
        });
    }

    public async Task<UtilisateurDTO> GetByIdAsync(int id)
    {
        var user = await usersRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Utilisateur {id} introuvable.");
        var dto = ToDTO(user);
        dto.HasActions = await usersRepo.HasActionsAsync(id);
        return dto;
    }

    public async Task<UtilisateurDTO> CreateAsync(CreateUtilisateurDTO dto)
    {
        // Validation domaine email
        if (!dto.Email.EndsWith("@finstar-cm.com", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("L'email doit appartenir au domaine @finstar-cm.com.");

        var existing = await usersRepo.GetByEmailAsync(dto.Email);
        if (existing is not null)
            throw new InvalidOperationException("Un utilisateur avec cet email existe déjà.");

        // Génération du mot de passe aléatoire (jamais loggué)
        var plainPassword = GeneratePassword();

        var user = new Utilisateur
        {
            Nom = dto.Nom,
            Prenom = dto.Prenom,
            Email = dto.Email,
            MotDePasse = passwordService.Hash(plainPassword),
            RoleId = dto.RoleId,
            Actif = true,
            DoitChangerMotDePasse = true, // obligatoire à la première connexion
            DateCreation = DateTime.UtcNow,
            DateModification = DateTime.UtcNow
        };

        var created = await usersRepo.CreateAsync(user);

        if (dto.PermissionsSupplementaires is { Count: > 0 })
            await permissionsRepo.SetUtilisateurPermissionsAsync(created.Id, dto.PermissionsSupplementaires);

        await usersRepo.AddLogAsync(new LogUtilisateur
        {
            UtilisateurId = created.Id,
            Action = "CREATION",
            Details = $"Compte créé pour {created.Prenom} {created.Nom} ({created.Email})",
            DateAction = DateTime.UtcNow
        });

        // Envoi du mot de passe par email — l'utilisateur est créé même si l'envoi échoue
        var emailSent = await emailService.SendAsync(
            to: created.Email,
            subject: "Bienvenue sur FinAssist — Vos identifiants de connexion",
            htmlBody: BuildWelcomeEmail(created.Prenom, created.Nom, created.Email, plainPassword)
        );

        // On efface la variable dès que possible
        plainPassword = string.Empty;

        var result = ToDTO(await usersRepo.GetByIdAsync(created.Id) ?? created);
        if (!emailSent)
            result.EmailWarning = "L'utilisateur a été créé mais l'envoi de l'email a échoué. Veuillez contacter l'administrateur système.";

        return result;
    }

    private static string GeneratePassword()
    {
        const string upper   = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower   = "abcdefghjkmnpqrstuvwxyz";
        const string digits  = "23456789";
        const string special = "@#$!%*?&";
        const string all     = upper + lower + digits + special;

        var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        var bytes = new byte[16];
        rng.GetBytes(bytes);

        // Garantir au moins un caractère de chaque catégorie
        var chars = new char[12];
        chars[0] = upper[bytes[0]  % upper.Length];
        chars[1] = lower[bytes[1]  % lower.Length];
        chars[2] = digits[bytes[2] % digits.Length];
        chars[3] = special[bytes[3] % special.Length];
        for (int i = 4; i < 12; i++)
            chars[i] = all[bytes[i] % all.Length];

        // Mélanger
        rng.GetBytes(bytes);
        return new string(chars.OrderBy(_ => bytes[System.Array.IndexOf(chars, _) % bytes.Length]).ToArray());
    }

    private static string BuildWelcomeEmail(string prenom, string nom, string email, string password) => $"""
        <div style="font-family:Segoe UI,sans-serif;max-width:520px;margin:auto;padding:32px;background:#f9f9f9;border-radius:12px;">
          <h2 style="color:#7c3aed;">Bienvenue sur FinAssist</h2>
          <p>Bonjour <strong>{prenom} {nom}</strong>,</p>
          <p>Votre compte a été créé. Voici vos identifiants de connexion :</p>
          <div style="background:#fff;border:1px solid #e0e0e0;border-radius:8px;padding:16px 24px;margin:20px 0;">
            <p style="margin:4px 0;"><strong>Email :</strong> {email}</p>
            <p style="margin:4px 0;"><strong>Mot de passe :</strong> <code style="background:#f3f0ff;padding:2px 8px;border-radius:4px;">{password}</code></p>
          </div>
          <p style="color:#e74c3c;font-size:13px;">⚠ Changez votre mot de passe dès votre première connexion.</p>
          <hr style="border:none;border-top:1px solid #eee;margin:24px 0;" />
          <p style="font-size:12px;color:#999;">Cet email a été généré automatiquement. Ne pas répondre.</p>
        </div>
        """;

    public async Task<UtilisateurDTO> UpdateAsync(int id, UpdateUtilisateurDTO dto)
    {
        var user = await usersRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Utilisateur {id} introuvable.");

        var changes = new List<string>();
        if (dto.Nom is not null && dto.Nom != user.Nom) { changes.Add($"Nom: {user.Nom} → {dto.Nom}"); user.Nom = dto.Nom; }
        if (dto.Prenom is not null && dto.Prenom != user.Prenom) { changes.Add($"Prenom: {user.Prenom} → {dto.Prenom}"); user.Prenom = dto.Prenom; }
        if (dto.Email is not null && dto.Email != user.Email) { changes.Add($"Email: {user.Email} → {dto.Email}"); user.Email = dto.Email; }
        if (dto.RoleId.HasValue && dto.RoleId.Value != user.RoleId) { changes.Add($"RoleId: {user.RoleId} → {dto.RoleId.Value}"); user.RoleId = dto.RoleId.Value; }
        if (dto.Actif.HasValue && dto.Actif.Value != user.Actif) { changes.Add($"Actif: {user.Actif} → {dto.Actif.Value}"); user.Actif = dto.Actif.Value; }
        user.DateModification = DateTime.UtcNow;

        await usersRepo.UpdateAsync(user);

        if (changes.Count > 0)
        {
            await usersRepo.AddLogAsync(new LogUtilisateur
            {
                UtilisateurId = id,
                Action = "MODIFICATION",
                Details = string.Join(", ", changes),
                DateAction = DateTime.UtcNow
            });
        }

        return ToDTO(await usersRepo.GetByIdAsync(id) ?? user);
    }

    public async Task DeactivateAsync(int id)
    {
        var user = await usersRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Utilisateur {id} introuvable.");

        if (!user.Actif)
            throw new InvalidOperationException("Le compte est déjà désactivé.");

        user.Actif = false;
        user.DateModification = DateTime.UtcNow;
        await usersRepo.UpdateAsync(user);

        await usersRepo.AddLogAsync(new LogUtilisateur
        {
            UtilisateurId = id,
            Action = "DESACTIVATION",
            Details = $"Compte désactivé pour {user.Prenom} {user.Nom}",
            DateAction = DateTime.UtcNow
        });
    }

    public async Task ActivateAsync(int id)
    {
        var user = await usersRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Utilisateur {id} introuvable.");

        if (user.Actif)
            throw new InvalidOperationException("Le compte est déjà actif.");

        user.Actif = true;
        user.DateModification = DateTime.UtcNow;
        await usersRepo.UpdateAsync(user);

        await usersRepo.AddLogAsync(new LogUtilisateur
        {
            UtilisateurId = id,
            Action = "ACTIVATION",
            Details = $"Compte réactivé pour {user.Prenom} {user.Nom}",
            DateAction = DateTime.UtcNow
        });
    }

    public async Task DeleteAsync(int id)
    {
        var user = await usersRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Utilisateur {id} introuvable.");

        if (await usersRepo.HasActionsAsync(id))
            throw new InvalidOperationException("Cet utilisateur a déjà effectué des actions et ne peut pas être supprimé. Vous pouvez uniquement le désactiver.");

        await usersRepo.DeleteCascadeAsync(id);
    }

    public async Task<UtilisateurDTO> ChangeRoleAsync(int id, int roleId)
    {
        var user = await usersRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Utilisateur {id} introuvable.");

        var ancienRoleId = user.RoleId;
        user.RoleId = roleId;
        user.DateModification = DateTime.UtcNow;
        await usersRepo.UpdateAsync(user);

        await usersRepo.AddLogAsync(new LogUtilisateur
        {
            UtilisateurId = id,
            Action = "CHANGEMENT_ROLE",
            Details = $"RoleId: {ancienRoleId} → {roleId}",
            DateAction = DateTime.UtcNow
        });

        return ToDTO(await usersRepo.GetByIdAsync(id) ?? user);
    }

    public async Task<IEnumerable<LogUtilisateurDTO>> GetLogsAsync(int id)
    {
        _ = await usersRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Utilisateur {id} introuvable.");

        var logs = await usersRepo.GetLogsAsync(id);
        return logs.Select(l => new LogUtilisateurDTO
        {
            Id = l.Id,
            Action = l.Action,
            Details = l.Details,
            DateAction = l.DateAction
        });
    }

    public async Task ChangePasswordAsync(int id, string ancienMotDePasse, string nouveauMotDePasse)
    {
        var user = await usersRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Utilisateur {id} introuvable.");

        if (!passwordService.Verify(ancienMotDePasse, user.MotDePasse))
            throw new UnauthorizedAccessException("Mot de passe actuel incorrect.");

        user.MotDePasse = passwordService.Hash(nouveauMotDePasse);
        user.DateModification = DateTime.UtcNow;
        await usersRepo.UpdateAsync(user);

        await usersRepo.AddLogAsync(new LogUtilisateur
        {
            UtilisateurId = id,
            Action = "CHANGEMENT_MOT_DE_PASSE",
            Details = "Mot de passe modifié par l'utilisateur.",
            DateAction = DateTime.UtcNow
        });
    }

    private static UtilisateurDTO ToDTO(Utilisateur u) => new()
    {
        Id = u.Id,
        Nom = u.Nom,
        Prenom = u.Prenom,
        Email = u.Email,
        Role = u.Role?.Code ?? string.Empty,
        DateCreation = u.DateCreation,
        Actif = u.Actif
    };
}
