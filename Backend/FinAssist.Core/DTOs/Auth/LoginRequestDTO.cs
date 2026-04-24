namespace FinAssist.Core.DTOs.Auth;

public class LoginRequestDTO
{
    public string Email { get; set; } = string.Empty;
    public string MotDePasse { get; set; } = string.Empty;
}
