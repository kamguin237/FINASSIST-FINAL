using FinAssist.Core.DTOs.Users;
using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;

namespace FinAssist.Application.Services;

public class UsersService(IUsersRepository usersRepo, IPasswordService passwordService, IPermissionsRepository permissionsRepo) : IUsersService
{
    public async Task<IEnumerable<UtilisateurDTO>> GetAllAsync()
    {
        var users = await usersRepo.GetAllAsync();
        return users.Select(ToDTO);
    }

    public async Task<UtilisateurDTO> GetByIdAsync(int id)
    {
        var user = await usersRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Utilisateur {id} introuvable.");
        return ToDTO(user);
    }

    public async Task<UtilisateurDTO> CreateAsync(CreateUtilisateurDTO dto)
    {
        var existing = await usersRepo.GetByEmailAsync(dto.Email);
        if (existing is not null)
            throw new InvalidOperationException("Un utilisateur avec cet email existe déjà.");

        var user = new Utilisateur
        {
            Nom = dto.Nom,
            Prenom = dto.Prenom,
            Email = dto.Email,
            MotDePasse = passwordService.Hash(dto.MotDePasse),
            RoleId = dto.RoleId,
            Actif = true,
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

        return ToDTO(await usersRepo.GetByIdAsync(created.Id) ?? created);
    }

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
