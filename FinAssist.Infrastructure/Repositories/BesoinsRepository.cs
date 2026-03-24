using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;
using FinAssist.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinAssist.Infrastructure.Repositories;

public class BesoinsRepository(AppDbContext db) : IBesoinsRepository
{
    public Task<IEnumerable<Besoin>> GetAllAsync()
        => Task.FromResult<IEnumerable<Besoin>>(
            db.Besoins
              .Include(b => b.Utilisateur).ThenInclude(u => u.Role)
              .Include(b => b.Categorie)
              .AsEnumerable());

    public Task<Besoin?> GetByIdAsync(int id)
        => db.Besoins
             .Include(b => b.Utilisateur).ThenInclude(u => u.Role)
             .Include(b => b.Categorie)
             .Include(b => b.Documents)
             .Include(b => b.Historiques)
             .FirstOrDefaultAsync(b => b.Id == id);

    public Task<IEnumerable<Besoin>> GetByUtilisateurAsync(int utilisateurId)
        => Task.FromResult<IEnumerable<Besoin>>(
            db.Besoins
              .Include(b => b.Utilisateur).ThenInclude(u => u.Role)
              .Include(b => b.Categorie)
              .Where(b => b.UtilisateurId == utilisateurId)
              .AsEnumerable());

    public async Task<Besoin> CreateAsync(Besoin besoin)
    {
        db.Besoins.Add(besoin);
        await db.SaveChangesAsync();
        return besoin;
    }

    public async Task<Besoin> UpdateAsync(Besoin besoin)
    {
        db.Besoins.Update(besoin);
        await db.SaveChangesAsync();
        return besoin;
    }

    public async Task AddHistoriqueAsync(Historique historique)
    {
        db.Historiques.Add(historique);
        await db.SaveChangesAsync();
    }

    public Task<IEnumerable<Historique>> GetHistoriqueAsync(int besoinId)
        => Task.FromResult<IEnumerable<Historique>>(
            db.Historiques
              .Where(h => h.BesoinId == besoinId)
              .OrderByDescending(h => h.DateAction)
              .AsEnumerable());

    public async Task AddDocumentAsync(Document document)
    {
        db.Documents.Add(document);
        await db.SaveChangesAsync();
    }

    public Task<IEnumerable<Document>> GetDocumentsAsync(int besoinId)
        => Task.FromResult<IEnumerable<Document>>(
            db.Documents.Where(d => d.BesoinId == besoinId).AsEnumerable());

    public Task<Document?> GetDocumentByIdAsync(int documentId)
        => db.Documents.FirstOrDefaultAsync(d => d.Id == documentId);

    public Task<IEnumerable<Categorie>> GetAllCategoriesAsync()
        => Task.FromResult<IEnumerable<Categorie>>(
            db.Categories
              .Include(c => c.WorkflowCircuit)
              .AsEnumerable());

    public Task<Categorie?> GetCategorieByIdAsync(int id)
        => db.Categories.FirstOrDefaultAsync(c => c.Id == id);

    public Task<bool> CategorieNomExistsAsync(string nom, int? excludeId = null)
        => db.Categories.AnyAsync(c => c.Nom == nom && (excludeId == null || c.Id != excludeId));

    public Task<Categorie?> GetCategorieWithCircuitAsync(int id)
        => db.Categories
             .Include(c => c.WorkflowCircuit)
                 .ThenInclude(wc => wc!.Etapes)
             .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<Categorie> CreateCategorieAsync(Categorie categorie)
    {
        db.Categories.Add(categorie);
        await db.SaveChangesAsync();
        return categorie;
    }

    public async Task<Categorie> UpdateCategorieAsync(Categorie categorie)
    {
        db.Categories.Update(categorie);
        await db.SaveChangesAsync();
        return categorie;
    }

    public async Task DeleteCategorieAsync(Categorie categorie)
    {
        db.Categories.Remove(categorie);
        await db.SaveChangesAsync();
    }

    public async Task DeleteBesoinAsync(Besoin besoin)
    {
        db.Besoins.Remove(besoin);
        await db.SaveChangesAsync();
    }
}
