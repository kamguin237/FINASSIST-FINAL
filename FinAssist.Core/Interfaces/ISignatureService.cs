using FinAssist.Core.DTOs.Signature;

namespace FinAssist.Core.Interfaces;

public interface ISignatureService
{
    Task<SignatureDTO> SignerAsync(int documentId, int utilisateurId);
    Task<SignatureDTO> SignerParBesoinAsync(int besoinId, int utilisateurId, SignerBesoinDTO? dto = null);
    Task<VerificationDTO> VerifierAsync(int signatureId);
    Task<SignatureApercuDTO> GetApercuAsync(int besoinId);
    Task<(byte[] pdfBytes, string nomFichier)> GenererDocumentSigneAsync(int signatureId);
}
