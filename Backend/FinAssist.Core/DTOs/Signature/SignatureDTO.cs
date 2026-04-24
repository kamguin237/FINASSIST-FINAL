namespace FinAssist.Core.DTOs.Signature;

public class SignatureDTO
{
    public int Id { get; set; }
    public string Valeur { get; set; } = string.Empty;
    public string Empreinte { get; set; } = string.Empty;
    public DateTime Horodatage { get; set; }
    public bool Valide { get; set; }
    public int UtilisateurId { get; set; }
    public string SignataireNom { get; set; } = string.Empty;
    public int DocumentId { get; set; }
    public string DocumentNom { get; set; } = string.Empty;
}
