using FinAssist.Core.DTOs.Logs;

namespace FinAssist.Core.Interfaces;

public interface ILogService
{
    Task LoggerAsync(
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
        string? ville = null);

    Task<(IEnumerable<LogActiviteDTO> items, int total)> GetLogsAsync(FiltreLogsDTO filtres);
}
