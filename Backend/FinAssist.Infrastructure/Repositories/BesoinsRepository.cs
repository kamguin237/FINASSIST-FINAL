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
                  .ThenInclude(c => c!.WorkflowCircuit)
                      .ThenInclude(wc => wc!.Etapes)
              .AsEnumerable());

    public Task<Besoin?> GetByIdAsync(int id)
        => db.Besoins
             .Include(b => b.Utilisateur).ThenInclude(u => u.Role)
             .Include(b => b.Categorie)
                 .ThenInclude(c => c!.WorkflowCircuit)
                     .ThenInclude(wc => wc!.Etapes)
             .Include(b => b.Documents)
             .Include(b => b.Historiques)
             .FirstOrDefaultAsync(b => b.Id == id);

    public Task<IEnumerable<Besoin>> GetByUtilisateurAsync(int utilisateurId)
        => Task.FromResult<IEnumerable<Besoin>>(
            db.Besoins
              .Include(b => b.Utilisateur).ThenInclude(u => u.Role)
              .Include(b => b.Categorie)
                  .ThenInclude(c => c!.WorkflowCircuit)
                      .ThenInclude(wc => wc!.Etapes)
              .Where(b => b.UtilisateurId == utilisateurId)
              .AsEnumerable());

    public Task<IEnumerable<Validation>> GetValidationsParUtilisateurAsync(int utilisateurId)
        => Task.FromResult<IEnumerable<Validation>>(
            db.Validations
            .Where(v => v.ValidateurId == utilisateurId)
            .AsEnumerable());
            

    public Task<IEnumerable<int>> GetBesoinIdsValidesParUtilisateurAsync(int utilisateurId)
        => Task.FromResult<IEnumerable<int>>(
            db.Validations
              .Where(v => v.ValidateurId == utilisateurId)
              .Select(v => v.BesoinId)
              .Distinct()
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

    public async Task DeleteDocumentAsync(int documentId)
        => await db.Documents.Where(d => d.Id == documentId).ExecuteDeleteAsync();

    public async Task DeleteBesoinAsync(Besoin besoin)
    {
        db.Besoins.Remove(besoin);
        await db.SaveChangesAsync();
    }

    public Task<bool> UtilisateurADejaValideAsync(int besoinId, int utilisateurId)
    => Task.FromResult(
        db.Validations.Any(v =>
            v.BesoinId == besoinId &&
            v.ValidateurId == utilisateurId));

    public Task<IEnumerable<string>> GetCodesPermissionsUtilisateurAsync(int utilisateurId)
    => Task.FromResult<IEnumerable<string>>(
        // Permissions du rôle de l'utilisateur
        db.RolePermissions
            .Where(rp => db.Utilisateurs.Any(u => u.Id == utilisateurId && u.RoleId == rp.RoleId))
            .Select(rp => rp.Permission.Code)
        // Union avec les permissions individuelles de l'utilisateur
        .Union(
            db.UtilisateurPermissions
                .Where(up => up.UtilisateurId == utilisateurId)
                .Select(up => up.Permission.Code)
        )
        .Distinct()
        .AsEnumerable());

    public async Task AddValidationAsync(Validation validation)
    {
        db.Validations.Add(validation);
        await db.SaveChangesAsync();
    }

    public async Task<Utilisateur?> GetUtilisateurByIdAsync(int id)
        => await db.Utilisateurs.FindAsync(id);

    public async Task<IEnumerable<int>> GetUtilisateurIdsByRoleAsync(string roleCode)
        => await db.Utilisateurs
            .Where(u => u.Role.Code == roleCode && u.Actif)
            .Select(u => u.Id)
            .ToListAsync();
}
