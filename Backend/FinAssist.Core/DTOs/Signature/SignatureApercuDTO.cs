namespace FinAssist.Core.DTOs.Signature;

public class SignatureApercuDTO
{
    public int Id { get; set; }
    public string? SignatureBase64 { get; set; }
    public double? PositionX { get; set; }
    public double? PositionY { get; set; }
    public int? Largeur { get; set; }
    public int? Hauteur { get; set; }
    public DateTime Horodatage { get; set; }
    public string Empreinte { get; set; } = string.Empty;
    public bool Valide { get; set; }
    public SignataireInfoDTO Signataire { get; set; } = new();
}

public class SignataireInfoDTO
{
    public string Nom { get; set; } = string.Empty;
    public string Prenom { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
