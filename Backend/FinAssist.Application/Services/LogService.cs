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
        string? adresseIp = null,
        string? systemeExploitation = null,
        string? navigateur = null,
        string? pays = null,
        string? ville = null)
    {
        await logRepo.AjouterAsync(new LogActivite
        {
            Action              = action,
            EntiteType          = entiteType,
            EntiteId            = entiteId,
            AncienneValeur      = ancienneValeur,
            NouvelleValeur      = nouvelleValeur,
            UtilisateurId       = utilisateurId,
            Date                = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
            AdresseIp           = adresseIp,
            SystemeExploitation = systemeExploitation,
            Navigateur          = navigateur,
            Pays                = pays,
            Ville               = ville
        });
    }

    public async Task<(IEnumerable<LogActiviteDTO> items, int total)> GetLogsAsync(FiltreLogsDTO filtres)
    {
        var (items, total) = await logRepo.GetPagedAsync(filtres);

        var dtos = items.Select(l => new LogActiviteDTO
        {
            Id                  = l.Id,
            Action              = l.Action,
            EntiteType          = l.EntiteType,
            EntiteId            = l.EntiteId,
            AncienneValeur      = l.AncienneValeur,
            NouvelleValeur      = l.NouvelleValeur,
            UtilisateurId       = l.UtilisateurId,
            NomUtilisateur      = l.Utilisateur is not null
                ? $"{l.Utilisateur.Prenom} {l.Utilisateur.Nom}"
                : null,
            Date                = DateTime.SpecifyKind(l.Date, DateTimeKind.Utc),
            AdresseIp           = l.AdresseIp,
            SystemeExploitation = l.SystemeExploitation,
            Navigateur          = l.Navigateur,
            Pays                = l.Pays,
            Ville               = l.Ville
        });

        return (dtos, total);
    }
}
