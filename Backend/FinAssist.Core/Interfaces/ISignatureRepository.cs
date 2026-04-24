using FinAssist.Core.Entities;

namespace FinAssist.Core.Interfaces;

public interface ISignatureRepository
{
    Task<SignatureElectronique?> GetByIdAsync(int id);
    Task<SignatureElectronique?> GetByDocumentIdAsync(int documentId);
    Task<SignatureElectronique?> GetByDocumentAndUtilisateurAsync(int documentId, int utilisateurId);
    Task<SignatureElectronique?> GetByBesoinIdAsync(int besoinId);
    Task<SignatureElectronique> AddAsync(SignatureElectronique signature);
    Task<SignatureElectronique> UpdateAsync(SignatureElectronique signature);
}
