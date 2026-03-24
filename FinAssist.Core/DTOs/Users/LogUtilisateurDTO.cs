namespace FinAssist.Core.DTOs.Users;

public class LogUtilisateurDTO
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime DateAction { get; set; }
}
