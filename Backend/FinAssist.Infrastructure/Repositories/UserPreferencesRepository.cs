using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;
using FinAssist.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinAssist.Infrastructure.Repositories;

public class UserPreferencesRepository(AppDbContext db) : IUserPreferencesRepository
{
    public Task<UserPreferences?> GetByUtilisateurIdAsync(int utilisateurId)
        => db.UserPreferences.FirstOrDefaultAsync(p => p.UtilisateurId == utilisateurId);

    public async Task<UserPreferences> SaveAsync(UserPreferences prefs)
    {
        var existing = await db.UserPreferences
            .FirstOrDefaultAsync(p => p.UtilisateurId == prefs.UtilisateurId);

        if (existing is null)
        {
            db.UserPreferences.Add(prefs);
        }
        else
        {
            existing.NotifApp           = prefs.NotifApp;
            existing.NotifEmail         = prefs.NotifEmail;
            existing.AlertNouveauBesoin = prefs.AlertNouveauBesoin;
            existing.AlertValidation    = prefs.AlertValidation;
            existing.AlertEnAttente     = prefs.AlertEnAttente;
            existing.Langue             = prefs.Langue;
            existing.FormatDate         = prefs.FormatDate;
            existing.FuseauHoraire      = prefs.FuseauHoraire;
            existing.ItemsParPage       = prefs.ItemsParPage;
            existing.PageAccueil        = prefs.PageAccueil;
            existing.TriDefaut          = prefs.TriDefaut;
            existing.DateModification   = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        return existing ?? prefs;
    }
}
