namespace FinAssist.Core.DTOs.Auth;

public class LoginResponseDTO
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime Expiration { get; set; }
    public UtilisateurInfoDTO Utilisateur { get; set; } = null!;
}
