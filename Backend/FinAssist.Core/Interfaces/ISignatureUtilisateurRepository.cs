using FinAssist.Core.Entities;

namespace FinAssist.Core.Interfaces;

public interface ISignatureUtilisateurRepository
{
    Task<SignatureUtilisateur?> GetByUtilisateurIdAsync(int utilisateurId);
    Task<SignatureUtilisateur> SaveAsync(SignatureUtilisateur signature);
    Task DeleteAsync(int utilisateurId);
}
