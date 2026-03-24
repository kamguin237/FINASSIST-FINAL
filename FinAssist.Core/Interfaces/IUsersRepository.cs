using FinAssist.Core.Entities;

namespace FinAssist.Core.Interfaces;

public interface IUsersRepository
{
    Task<IEnumerable<Utilisateur>> GetAllAsync();
    Task<Utilisateur?> GetByIdAsync(int id);
    Task<Utilisateur?> GetByEmailAsync(string email);
    Task<Utilisateur> CreateAsync(Utilisateur utilisateur);
    Task<Utilisateur> UpdateAsync(Utilisateur utilisateur);
    Task<IEnumerable<LogUtilisateur>> GetLogsAsync(int utilisateurId);
    Task AddLogAsync(LogUtilisateur log);
}
