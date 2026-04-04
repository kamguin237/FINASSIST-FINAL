using FinAssist.Core.DTOs.Users;

namespace FinAssist.Core.Interfaces;

public interface IUsersService
{
    Task<IEnumerable<UtilisateurDTO>> GetAllAsync();
    Task<UtilisateurDTO> GetByIdAsync(int id);
    Task<UtilisateurDTO> CreateAsync(CreateUtilisateurDTO dto);
    Task<UtilisateurDTO> UpdateAsync(int id, UpdateUtilisateurDTO dto);
    Task DeactivateAsync(int id);
    Task ActivateAsync(int id);
    Task DeleteAsync(int id);
    Task<UtilisateurDTO> ChangeRoleAsync(int id, int roleId);
    Task<IEnumerable<LogUtilisateurDTO>> GetLogsAsync(int id);
    Task ChangePasswordAsync(int id, string ancienMotDePasse, string nouveauMotDePasse);
}
