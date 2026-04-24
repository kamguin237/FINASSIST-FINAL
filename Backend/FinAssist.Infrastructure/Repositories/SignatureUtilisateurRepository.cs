using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;
using FinAssist.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinAssist.Infrastructure.Repositories;

public class SignatureUtilisateurRepository(AppDbContext db) : ISignatureUtilisateurRepository
{
    public Task<SignatureUtilisateur?> GetByUtilisateurIdAsync(int utilisateurId)
        => db.SignaturesUtilisateurs.FirstOrDefaultAsync(s => s.UtilisateurId == utilisateurId);

    public async Task<SignatureUtilisateur> SaveAsync(SignatureUtilisateur signature)
    {
        var existing = await db.SignaturesUtilisateurs
            .FirstOrDefaultAsync(s => s.UtilisateurId == signature.UtilisateurId);

        if (existing is null)
        {
            db.SignaturesUtilisateurs.Add(signature);
        }
        else
        {
            existing.Type = signature.Type;
            existing.ImageBase64 = signature.ImageBase64;
            existing.Police = signature.Police;
            existing.DateModification = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        return existing ?? signature;
    }

    public async Task DeleteAsync(int utilisateurId)
    {
        await db.SignaturesUtilisateurs
            .Where(s => s.UtilisateurId == utilisateurId)
            .ExecuteDeleteAsync();
    }
}
