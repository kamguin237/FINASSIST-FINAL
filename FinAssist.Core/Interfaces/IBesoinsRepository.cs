using FinAssist.Core.Entities;

namespace FinAssist.Core.Interfaces;

public interface IBesoinsRepository
{
    Task<IEnumerable<Besoin>> GetAllAsync();
    Task<Besoin?> GetByIdAsync(int id);
    Task<IEnumerable<Besoin>> GetByUtilisateurAsync(int utilisateurId);
    Task<IEnumerable<int>> GetBesoinIdsValidesParUtilisateurAsync(int utilisateurId);
    Task<Besoin> CreateAsync(Besoin besoin);
    Task<Besoin> UpdateAsync(Besoin besoin);
    Task AddHistoriqueAsync(Historique historique);
    Task<IEnumerable<Historique>> GetHistoriqueAsync(int besoinId);
    Task AddDocumentAsync(Document document);
    Task<IEnumerable<Document>> GetDocumentsAsync(int besoinId);
    Task<Document?> GetDocumentByIdAsync(int documentId);
    Task DeleteDocumentAsync(int documentId);
    Task<IEnumerable<Validation>> GetValidationsParUtilisateurAsync(int utilisateurId);
    Task<IEnumerable<string>> GetCodesPermissionsUtilisateurAsync(int utilisateurId);

    // Catégories
    Task<IEnumerable<Categorie>> GetAllCategoriesAsync();
    Task<Categorie?> GetCategorieByIdAsync(int id);
    Task<Categorie?> GetCategorieWithCircuitAsync(int id);
    Task<bool> CategorieNomExistsAsync(string nom, int? excludeId = null);
    Task<Categorie> CreateCategorieAsync(Categorie categorie);
    Task<Categorie> UpdateCategorieAsync(Categorie categorie);
    Task DeleteCategorieAsync(Categorie categorie);
    Task DeleteBesoinAsync(Besoin besoin);
}
