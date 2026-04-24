using FinAssist.Core.DTOs.Reporting;

namespace FinAssist.Core.Interfaces;

public interface IReportingService
{
    Task<StatistiquesDTO> GetStatistiquesAsync();
    Task<RapportDTO> GetRapportBesoinsAsync(FiltreRapportDTO? filtres, int generateurId);
    Task<DashboardDTO> GetDashboardAsync(int utilisateurId, string roleCode);
    Task<IEnumerable<EvolutionPointDTO>> GetEvolutionBesoinsAsync(string periode, int utilisateurId, string roleCode);
    Task<(byte[] contenu, string contentType, string nomFichier)> ExporterAsync(ExportRequestDTO request, int generateurId);
   
}
