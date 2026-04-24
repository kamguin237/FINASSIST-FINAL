namespace FinAssist.Core.DTOs.Signatures;

public class SignatureUtilisateurDTO
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string ImageBase64 { get; set; } = string.Empty;
    public string? Police { get; set; }
    public DateTime DateCreation { get; set; }
    public DateTime DateModification { get; set; }
}

public class SaveSignatureUtilisateurDTO
{
    /// <summary>manuscrite | typographique | upload</summary>
    public string Type { get; set; } = string.Empty;
    public string ImageBase64 { get; set; } = string.Empty;
    public string? Police { get; set; }
}
