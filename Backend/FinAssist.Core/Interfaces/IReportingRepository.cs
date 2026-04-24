using FinAssist.Core.DTOs.Reporting;
using FinAssist.Core.Entities;

namespace FinAssist.Core.Interfaces;

public interface IReportingRepository
{
    Task<IEnumerable<Besoin>> GetAllBesoinsAsync();
    Task<IEnumerable<Besoin>> GetBesoinsFiltrésAsync(FiltreRapportDTO? filtres);
    Task<IEnumerable<Besoin>> GetBesoinsParUtilisateurAsync(int utilisateurId);
    Task<IEnumerable<Utilisateur>> GetAllUtilisateursAsync();
    Task<int> CountSignaturesAsync();
    Task<int> CountNotificationsAsync();
    Task<int> CountNotificationsNonLuesAsync(int utilisateurId);
}
