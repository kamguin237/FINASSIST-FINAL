using FinAssist.Core.DTOs.Logs;
using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;

namespace FinAssist.Application.Services;

public class LogService(ILogRepository logRepo) : ILogService
{
    public async Task LoggerAsync(
        string action,
        string entiteType,
        int? entiteId = null,
        string? ancienneValeur = null,
        string? nouvelleValeur = null,
        int? utilisateurId = null,
        string? adresseIp = null)
    {
        var log = new LogActivite
        {
            Action = action,
            EntiteType = entiteType,
            EntiteId = entiteId,
            AncienneValeur = ancienneValeur,
            NouvelleValeur = nouvelleValeur,
            UtilisateurId = utilisateurId,
            AdresseIp = adresseIp,
            Date = DateTime.UtcNow
        };

        await logRepo.AjouterAsync(log);
    }

    public async Task<(IEnumerable<LogActiviteDTO> items, int total)> GetLogsAsync(FiltreLogsDTO filtres)
    {
        var (items, total) = await logRepo.GetPagedAsync(filtres);

        var dtos = items.Select(l => new LogActiviteDTO
        {
            Id = l.Id,
            Action = l.Action,
            EntiteType = l.EntiteType,
            EntiteId = l.EntiteId,
            AncienneValeur = l.AncienneValeur,
            NouvelleValeur = l.NouvelleValeur,
            UtilisateurId = l.UtilisateurId,
            NomUtilisateur = l.Utilisateur is not null
                ? $"{l.Utilisateur.Prenom} {l.Utilisateur.Nom}"
                : null,
            AdresseIp = l.AdresseIp,
            Date = l.Date
        });

        return (dtos, total);
    }
}
