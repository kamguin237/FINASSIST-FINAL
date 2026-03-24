using FinAssist.Core.Entities;

namespace FinAssist.Core.Interfaces;

public interface IRolesRepository
{
    Task<IEnumerable<Role>> GetAllAsync();
    Task<Role?> GetByIdAsync(int id);
    Task<bool> ExistsAsync(string code);
    Task<Role> CreateAsync(Role role);
    Task<Role> UpdateAsync(Role role);
    Task DeleteAsync(int id);
}
