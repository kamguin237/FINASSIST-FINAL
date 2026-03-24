using FinAssist.Core.DTOs.Signature;

namespace FinAssist.Core.Interfaces;

public interface ISignatureService
{
    Task<SignatureDTO> SignerAsync(int documentId, int utilisateurId);
    Task<SignatureDTO> SignerParBesoinAsync(int besoinId, int utilisateurId);
    Task<VerificationDTO> VerifierAsync(int signatureId);
}
