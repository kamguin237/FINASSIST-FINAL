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
