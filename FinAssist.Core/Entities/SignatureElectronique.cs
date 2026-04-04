namespace FinAssist.Core.Entities;

public class SignatureElectronique
{
    public int Id { get; set; }
    public string Valeur { get; set; } = string.Empty;
    public DateTime Horodatage { get; set; } = DateTime.UtcNow;
    public string Empreinte { get; set; } = string.Empty;
    public bool Valide { get; set; } = true;

    // Signature manuscrite numérisée
    public string? SignatureBase64 { get; set; }
    public double? PositionX { get; set; }
    public double? PositionY { get; set; }
    public int? Largeur { get; set; }
    public int? Hauteur { get; set; }

    // PDF final avec signature incrustée (généré côté frontend via pdf-lib)
    public byte[]? PdfSigne { get; set; }

    public int UtilisateurId { get; set; }
    public Utilisateur Utilisateur { get; set; } = null!;

    public int DocumentId { get; set; }
    public Document Document { get; set; } = null!;
}
