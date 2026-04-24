namespace FinAssist.Core.Entities;

/// <summary>
/// Signature personnelle d'un utilisateur, réutilisable lors de la signature de documents.
/// </summary>
public class SignatureUtilisateur
{
    public int Id { get; set; }
    public int UtilisateurId { get; set; }
    public Utilisateur Utilisateur { get; set; } = null!;

    /// <summary>manuscrite | typographique | upload</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Image base64 PNG de la signature</summary>
    public string ImageBase64 { get; set; } = string.Empty;

    /// <summary>Police choisie (pour type typographique)</summary>
    public string? Police { get; set; }

    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public DateTime DateModification { get; set; } = DateTime.UtcNow;
}
