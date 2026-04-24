namespace FinAssist.Core.Entities;

public class QrSignatureSession
{
    public int Id { get; set; }
    public string Token { get; set; } = string.Empty;   // UUID unique
    public int UtilisateurId { get; set; }
    public Utilisateur Utilisateur { get; set; } = null!;
    public DateTime CreeLe { get; set; } = DateTime.UtcNow;
    public DateTime Expiration { get; set; }
    public bool Completed { get; set; } = false;
    public string? SignatureBase64 { get; set; }         // reçue depuis le mobile
    public DateTime? CompletedAt { get; set; }
}
