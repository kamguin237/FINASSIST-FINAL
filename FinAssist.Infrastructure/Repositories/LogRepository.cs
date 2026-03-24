using FinAssist.Core.DTOs.Logs;
using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;
using FinAssist.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinAssist.Infrastructure.Repositories;

public class LogRepository(AppDbContext db) : ILogRepository
{
    public async Task AjouterAsync(LogActivite log)
    {
        db.LogsActivites.Add(log);
        await db.SaveChangesAsync();
    }

    public async Task<(IEnumerable<LogActivite> items, int total)> GetPagedAsync(FiltreLogsDTO filtres)
    {
        var query = db.LogsActivites
            .Include(l => l.Utilisateur)
            .AsNoTracking()
            .AsQueryable();

        if (filtres.DateDebut.HasValue)
            query = query.Where(l => l.Date >= filtres.DateDebut.Value);

        if (filtres.DateFin.HasValue)
            query = query.Where(l => l.Date <= filtres.DateFin.Value);

        if (!string.IsNullOrWhiteSpace(filtres.Action))
            query = query.Where(l => l.Action.Contains(filtres.Action));

        if (!string.IsNullOrWhiteSpace(filtres.EntiteType))
            query = query.Where(l => l.EntiteType == filtres.EntiteType);

        if (filtres.UtilisateurId.HasValue)
            query = query.Where(l => l.UtilisateurId == filtres.UtilisateurId.Value);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(l => l.Date)
            .Skip((filtres.Page - 1) * filtres.PageSize)
            .Take(filtres.PageSize)
            .ToListAsync();

        return (items, total);
    }
}
