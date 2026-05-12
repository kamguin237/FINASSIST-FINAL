namespace FinAssist.Core.DTOs.Auth;

public class ChangePasswordDTO
{
    public string AncienMotDePasse { get; set; } = string.Empty;
    public string NouveauMotDePasse { get; set; } = string.Empty;
}
