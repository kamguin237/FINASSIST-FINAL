using FinAssist.Core.DTOs.Besoins;

namespace FinAssist.Core.Interfaces;

public interface IBesoinsService
{
    Task<IEnumerable<BesoinDTO>> GetAllAsync(int utilisateurId, string roleCode);
    Task<BesoinDTO> GetByIdAsync(int id, int utilisateurId, string roleCode);
    Task<BesoinDTO> CreateAsync(CreateBesoinDTO dto, int utilisateurId);
    Task<BesoinDTO> UpdateAsync(int id, UpdateBesoinDTO dto, int utilisateurId);
    Task<BesoinDTO> EnregistrerAsync(int id, int utilisateurId);
    Task<BesoinDTO> SoumettreAsync(int id, int utilisateurId);
    Task<DocumentDTO> AjouterPieceJointeAsync(int id, string nom, string type, byte[] contenu);
    Task<IEnumerable<HistoriqueDTO>> GetHistoriqueAsync(int id);
    Task<IEnumerable<CategorieDTO>> GetAllCategoriesAsync();
    Task<CategorieDetailDTO> GetCategorieByIdAsync(int id);
    Task<CategorieDTO> CreateCategorieAsync(CreateCategorieDTO dto);
    Task<CategorieDTO> UpdateCategorieAsync(int id, UpdateCategorieDTO dto);
    Task DeleteCategorieAsync(int id);
    Task<CategorieDTO> AssignerCircuitAsync(int categorieId, int workflowCircuitId);
    Task DeleteAsync(int id);
}
