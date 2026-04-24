using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;
using FinAssist.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinAssist.Infrastructure.Repositories;

public class SignatureRepository(AppDbContext db) : ISignatureRepository
{
    public Task<SignatureElectronique?> GetByIdAsync(int id)
        => db.Signatures
             .Include(s => s.Utilisateur).ThenInclude(u => u.Role)
             .Include(s => s.Document)
             .FirstOrDefaultAsync(s => s.Id == id);

    public Task<SignatureElectronique?> GetByDocumentIdAsync(int documentId)
        => db.Signatures
             .Include(s => s.Utilisateur)
             .Include(s => s.Document)
             .FirstOrDefaultAsync(s => s.DocumentId == documentId);

    public Task<SignatureElectronique?> GetByDocumentAndUtilisateurAsync(int documentId, int utilisateurId)
        => db.Signatures
             .Include(s => s.Utilisateur)
             .Include(s => s.Document)
             .FirstOrDefaultAsync(s => s.DocumentId == documentId && s.UtilisateurId == utilisateurId);

    public Task<SignatureElectronique?> GetByBesoinIdAsync(int besoinId)
        => db.Signatures
             .Include(s => s.Utilisateur).ThenInclude(u => u.Role)
             .Include(s => s.Document)
             .Where(s => s.Document.BesoinId == besoinId && s.Valide)
             .OrderByDescending(s => s.Horodatage)
             .FirstOrDefaultAsync();

    public async Task<SignatureElectronique> AddAsync(SignatureElectronique signature)
    {
        db.Signatures.Add(signature);
        await db.SaveChangesAsync();
        return signature;
    }

    public async Task<SignatureElectronique> UpdateAsync(SignatureElectronique signature)
    {
        db.Signatures.Update(signature);
        await db.SaveChangesAsync();
        return signature;
    }
}
