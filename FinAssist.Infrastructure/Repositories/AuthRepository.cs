using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;
using FinAssist.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinAssist.Infrastructure.Repositories;

public class AuthRepository(AppDbContext db) : IAuthRepository
{
    public Task<Utilisateur?> GetByEmailAsync(string email)
        => db.Utilisateurs.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email);

    public Task<Utilisateur?> GetByIdAsync(int id)
        => db.Utilisateurs.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id);
}
