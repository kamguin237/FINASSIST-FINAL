using FinAssist.Core.DTOs.Logs;
using FinAssist.Core.Entities;

namespace FinAssist.Core.Interfaces;

public interface ILogRepository
{
    Task AjouterAsync(LogActivite log);
    Task<(IEnumerable<LogActivite> items, int total)> GetPagedAsync(FiltreLogsDTO filtres);
}
