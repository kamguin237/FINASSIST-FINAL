using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;
using FinAssist.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinAssist.Infrastructure.Repositories;

public class UsersRepository(AppDbContext db) : IUsersRepository
{
    public Task<IEnumerable<Utilisateur>> GetAllAsync()
        => Task.FromResult<IEnumerable<Utilisateur>>(
            db.Utilisateurs.Include(u => u.Role).AsEnumerable());

    public Task<Utilisateur?> GetByIdAsync(int id)
        => db.Utilisateurs.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id);

    public Task<Utilisateur?> GetByEmailAsync(string email)
        => db.Utilisateurs.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email);

    public async Task<Utilisateur> CreateAsync(Utilisateur utilisateur)
    {
        db.Utilisateurs.Add(utilisateur);
        await db.SaveChangesAsync();
        return utilisateur;
    }

    public async Task<Utilisateur> UpdateAsync(Utilisateur utilisateur)
    {
        db.Utilisateurs.Update(utilisateur);
        await db.SaveChangesAsync();
        return utilisateur;
    }

    public async Task DeleteAsync(int id)
        => await db.Utilisateurs.Where(u => u.Id == id).ExecuteDeleteAsync();

    public async Task DeleteCascadeAsync(int id)
    {
        // 1. Signatures liées aux documents des besoins de cet utilisateur
        var besoinIds = await db.Besoins
            .Where(b => b.UtilisateurId == id)
            .Select(b => b.Id)
            .ToListAsync();

        if (besoinIds.Count > 0)
        {
            var documentIds = await db.Documents
                .Where(d => besoinIds.Contains(d.BesoinId))
                .Select(d => d.Id)
                .ToListAsync();

            if (documentIds.Count > 0)
                await db.Signatures
                    .Where(s => documentIds.Contains(s.DocumentId))
                    .ExecuteDeleteAsync();

            await db.Documents.Where(d => besoinIds.Contains(d.BesoinId)).ExecuteDeleteAsync();
            await db.Historiques.Where(h => besoinIds.Contains(h.BesoinId)).ExecuteDeleteAsync();
            await db.Validations.Where(v => besoinIds.Contains(v.BesoinId)).ExecuteDeleteAsync();
            await db.Besoins.Where(b => b.UtilisateurId == id).ExecuteDeleteAsync();
        }

        // Validations où l'utilisateur est validateur (sur des besoins d'autres utilisateurs)
        await db.Validations.Where(v => v.ValidateurId == id).ExecuteDeleteAsync();

        // Signatures directes de l'utilisateur (sur des documents d'autres besoins)
        await db.Signatures.Where(s => s.UtilisateurId == id).ExecuteDeleteAsync();

        // 5. Permissions directes, logs utilisateur, notifications (cascade déjà configurée mais on force)
        await db.UtilisateurPermissions.Where(up => up.UtilisateurId == id).ExecuteDeleteAsync();
        await db.LogsUtilisateurs.Where(l => l.UtilisateurId == id).ExecuteDeleteAsync();
        await db.UtilisateurNotifications.Where(un => un.UtilisateurId == id).ExecuteDeleteAsync();

        // 6. Supprimer l'utilisateur
        await db.Utilisateurs.Where(u => u.Id == id).ExecuteDeleteAsync();
    }

    public Task<IEnumerable<LogUtilisateur>> GetLogsAsync(int utilisateurId)
        => Task.FromResult<IEnumerable<LogUtilisateur>>(
            db.LogsUtilisateurs
              .Where(l => l.UtilisateurId == utilisateurId)
              .OrderByDescending(l => l.DateAction)
              .AsEnumerable());

    public async Task AddLogAsync(LogUtilisateur log)
    {
        db.LogsUtilisateurs.Add(log);
        await db.SaveChangesAsync();
    }
}
