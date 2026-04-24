namespace FinAssist.Core.DTOs.Signature;

public class VerificationDTO
{
    public int SignatureId { get; set; }
    public bool Authentique { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Horodatage { get; set; }
    public string SignataireNom { get; set; } = string.Empty;
}
