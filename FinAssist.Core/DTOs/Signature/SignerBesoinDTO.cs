namespace FinAssist.Core.DTOs.Signature;

public class SignerBesoinDTO
{
    public int DocumentId { get; set; }
    public string? SignatureBase64 { get; set; }
    public double? PositionX { get; set; }
    public double? PositionY { get; set; }
    public int? Largeur { get; set; }
    public int? Hauteur { get; set; }

    // PDF déjà signé côté frontend (pdf-lib) — si présent, stocké directement sans incrustation backend
    public string? PdfSigneBase64 { get; set; }
}
