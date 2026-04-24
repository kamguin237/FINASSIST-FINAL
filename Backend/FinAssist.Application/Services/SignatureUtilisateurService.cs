using FinAssist.Core.DTOs.Signatures;
using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;

namespace FinAssist.Application.Services;

public class SignatureUtilisateurService(ISignatureUtilisateurRepository repo) : ISignatureUtilisateurService
{
    public async Task<SignatureUtilisateurDTO?> GetMaSignatureAsync(int utilisateurId)
    {
        var sig = await repo.GetByUtilisateurIdAsync(utilisateurId);
        return sig is null ? null : ToDTO(sig);
    }

    public async Task<SignatureUtilisateurDTO> SauvegarderAsync(int utilisateurId, SaveSignatureUtilisateurDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ImageBase64))
            throw new ArgumentException("L'image de la signature est obligatoire.");

        var types = new[] { "manuscrite", "typographique", "upload" };
        if (!types.Contains(dto.Type))
            throw new ArgumentException($"Type invalide. Valeurs acceptées : {string.Join(", ", types)}.");

        var sig = new SignatureUtilisateur
        {
            UtilisateurId = utilisateurId,
            Type = dto.Type,
            ImageBase64 = dto.ImageBase64,
            Police = dto.Police,
            DateCreation = DateTime.UtcNow,
            DateModification = DateTime.UtcNow
        };

        var saved = await repo.SaveAsync(sig);
        return ToDTO(saved);
    }

    public Task SupprimerAsync(int utilisateurId)
        => repo.DeleteAsync(utilisateurId);

    private static SignatureUtilisateurDTO ToDTO(SignatureUtilisateur s) => new()
    {
        Id = s.Id,
        Type = s.Type,
        ImageBase64 = s.ImageBase64,
        Police = s.Police,
        DateCreation = s.DateCreation,
        DateModification = s.DateModification
    };
}
