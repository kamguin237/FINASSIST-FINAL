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
        // Utiliser AddOrUpdate pour éviter les conflits de concurrence
        var existing = await db.UserPreferences
            .FirstOrDefaultAsync(p => p.UtilisateurId == prefs.UtilisateurId);

        if (existing is null)
        {
            // Vérifier une dernière fois avec un lock optimiste
            db.UserPreferences.Add(prefs);
            try
            {
                await db.SaveChangesAsync();
                return prefs;
            }
            catch (DbUpdateException)
            {
                db.Entry(prefs).State = EntityState.Detached;
                // Recharger l'entité créée par une autre requête concurrente
                existing = await db.UserPreferences
                    .FirstOrDefaultAsync(p => p.UtilisateurId == prefs.UtilisateurId);
                if (existing is null) throw;
            }
        }

        // Mise à jour atomique via ExecuteUpdateAsync (pas de conflit possible)
        await db.UserPreferences
            .Where(p => p.UtilisateurId == prefs.UtilisateurId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.NotifApp,           prefs.NotifApp)
                .SetProperty(p => p.NotifEmail,         prefs.NotifEmail)
                .SetProperty(p => p.AlertNouveauBesoin, prefs.AlertNouveauBesoin)
                .SetProperty(p => p.AlertValidation,    prefs.AlertValidation)
                .SetProperty(p => p.AlertEnAttente,     prefs.AlertEnAttente)
                .SetProperty(p => p.Langue,             prefs.Langue)
                .SetProperty(p => p.FormatDate,         prefs.FormatDate)
                .SetProperty(p => p.FuseauHoraire,      prefs.FuseauHoraire)
                .SetProperty(p => p.ItemsParPage,       prefs.ItemsParPage)
                .SetProperty(p => p.PageAccueil,        prefs.PageAccueil)
                .SetProperty(p => p.TriDefaut,          prefs.TriDefaut)
                .SetProperty(p => p.DateModification,   DateTime.UtcNow));

        // Retourner l'entité mise à jour
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
        return existing;
    }
}
