using FinAssist.Core.DTOs.Auth;

namespace FinAssist.Core.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDTO> LoginAsync(LoginRequestDTO request);
    Task LogoutAsync(int userId);
    Task ChangerMotDePasseAsync(int userId, ChangePasswordDTO dto);
}
