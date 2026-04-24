using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;
using FinAssist.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinAssist.Infrastructure.Repositories;

public class RolesRepository(AppDbContext db) : IRolesRepository
{
    public Task<IEnumerable<Role>> GetAllAsync()
        => Task.FromResult<IEnumerable<Role>>(db.Roles.AsEnumerable());

    public Task<Role?> GetByIdAsync(int id)
        => db.Roles.FirstOrDefaultAsync(r => r.Id == id);

    public Task<bool> ExistsAsync(string code)
        => db.Roles.AnyAsync(r => r.Code.ToLower() == code.ToLower());

    public async Task<Role> CreateAsync(Role role)
    {
        db.Roles.Add(role);
        await db.SaveChangesAsync();
        return role;
    }

    public async Task<Role> UpdateAsync(Role role)
    {
        db.Roles.Update(role);
        await db.SaveChangesAsync();
        return role;
    }

    public async Task DeleteAsync(int id)
    {
        await db.Roles.Where(r => r.Id == id).ExecuteDeleteAsync();
    }
}
