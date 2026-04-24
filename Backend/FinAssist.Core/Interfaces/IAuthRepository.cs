using FinAssist.Core.Entities;

namespace FinAssist.Core.Interfaces;

public interface IAuthRepository
{
    Task<Utilisateur?> GetByEmailAsync(string email);
    Task<Utilisateur?> GetByIdAsync(int id);
}
