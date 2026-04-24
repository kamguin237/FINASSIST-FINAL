using FinAssist.Core.DTOs.Signatures;

namespace FinAssist.Core.Interfaces;

public interface ISignatureUtilisateurService
{
    Task<SignatureUtilisateurDTO?> GetMaSignatureAsync(int utilisateurId);
    Task<SignatureUtilisateurDTO> SauvegarderAsync(int utilisateurId, SaveSignatureUtilisateurDTO dto);
    Task SupprimerAsync(int utilisateurId);
}
