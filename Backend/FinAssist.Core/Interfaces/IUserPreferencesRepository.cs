using FinAssist.Core.Entities;

namespace FinAssist.Core.Interfaces;

public interface IUserPreferencesRepository
{
    Task<UserPreferences?> GetByUtilisateurIdAsync(int utilisateurId);
    Task<UserPreferences> SaveAsync(UserPreferences prefs);
}
