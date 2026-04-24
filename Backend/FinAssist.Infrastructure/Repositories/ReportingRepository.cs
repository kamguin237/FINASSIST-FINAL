using FinAssist.Core.DTOs.Reporting;
using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;
using FinAssist.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinAssist.Infrastructure.Repositories;

public class ReportingRepository(AppDbContext db) : IReportingRepository
{
    public async Task<IEnumerable<Besoin>> GetAllBesoinsAsync() =>
        await db.Besoins
            .Include(b => b.Categorie)
                .ThenInclude(c => c!.WorkflowCircuit)
                    .ThenInclude(wc => wc!.Etapes)
            .Include(b => b.Utilisateur)
            .AsNoTracking()
            .ToListAsync();

    public async Task<IEnumerable<Besoin>> GetBesoinsFiltrésAsync(FiltreRapportDTO? filtres)
    {
        var query = db.Besoins
            .Include(b => b.Categorie)
            .Include(b => b.Utilisateur)
            .AsNoTracking()
            .AsQueryable();

        if (filtres is null) return await query.ToListAsync();

        if (filtres.DateDebut.HasValue)
            query = query.Where(b => b.DateCreation >= filtres.DateDebut.Value);

        if (filtres.DateFin.HasValue)
            query = query.Where(b => b.DateCreation <= filtres.DateFin.Value);

        if (!string.IsNullOrWhiteSpace(filtres.Statut))
            query = query.Where(b => b.Statut == filtres.Statut);

        return await query.ToListAsync();
    }

    public async Task<IEnumerable<Besoin>> GetBesoinsParUtilisateurAsync(int utilisateurId) =>
        await db.Besoins
            .Include(b => b.Categorie)
            .Where(b => b.UtilisateurId == utilisateurId)
            .AsNoTracking()
            .ToListAsync();

    public async Task<IEnumerable<Utilisateur>> GetAllUtilisateursAsync() =>
        await db.Utilisateurs.AsNoTracking().ToListAsync();

    public async Task<int> CountSignaturesAsync() =>
        await db.Signatures.CountAsync();

    public async Task<int> CountNotificationsAsync() =>
        await db.Notifications.CountAsync();

    public async Task<int> CountNotificationsNonLuesAsync(int utilisateurId) =>
        await db.UtilisateurNotifications
            .Where(un => un.UtilisateurId == utilisateurId && !un.Lu)
            .CountAsync();
}
