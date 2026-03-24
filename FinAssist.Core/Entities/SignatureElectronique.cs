namespace FinAssist.Core.Entities;

public class SignatureElectronique
{
    public int Id { get; set; }
    public string Valeur { get; set; } = string.Empty;      // HMAC-SHA256 en base64
    public DateTime Horodatage { get; set; } = DateTime.UtcNow;
    public string Empreinte { get; set; } = string.Empty;   // SHA256 du contenu du document
    public bool Valide { get; set; } = true;

    public int UtilisateurId { get; set; }
    public Utilisateur Utilisateur { get; set; } = null!;

    public int DocumentId { get; set; }
    public Document Document { get; set; } = null!;
}
