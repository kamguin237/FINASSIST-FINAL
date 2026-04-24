namespace FinAssist.Core.DTOs.Signatures;

public class QrSignatureResponseDTO
{
    public string Token { get; set; } = string.Empty;
    public string UrlVerification { get; set; } = string.Empty;
    public string Nom { get; set; } = string.Empty;
    public string Prenom { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime GenereLe { get; set; }
    public DateTime Expiration { get; set; }
    public bool Valide { get; set; }
}

public class QrVerificationResultDTO
{
    public bool Valide { get; set; }
    public string Statut { get; set; } = string.Empty; // "valide" | "expire" | "invalide"
    public string? Nom { get; set; }
    public string? Prenom { get; set; }
    public string? Role { get; set; }
    public DateTime? GenereLe { get; set; }
    public DateTime? Expiration { get; set; }
    public string? Message { get; set; }
}
