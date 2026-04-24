namespace FinAssist.Core.Interfaces;

public interface IPdfSignatureService
{
    byte[] IncrusterSignature(
        byte[] pdfOriginal,
        string signatureBase64,
        double positionXPct,
        double positionYPct,
        int largeur,
        int hauteur,
        string signataireNom,
        string signataireRole,
        DateTime horodatage);
}
