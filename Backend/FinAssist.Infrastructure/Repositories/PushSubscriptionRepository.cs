using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;
using FinAssist.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinAssist.Infrastructure.Repositories;

public class PushSubscriptionRepository(AppDbContext db) : IPushSubscriptionRepository
{
    public async Task SaveAsync(PushSubscription sub)
    {
        var existing = await db.PushSubscriptions
            .FirstOrDefaultAsync(s => s.Endpoint == sub.Endpoint);
        if (existing is null)
            db.PushSubscriptions.Add(sub);
        else
        {
            existing.P256dh = sub.P256dh;
            existing.Auth   = sub.Auth;
        }
        await db.SaveChangesAsync();
    }

    public async Task<IEnumerable<PushSubscription>> GetByUtilisateurAsync(int utilisateurId)
        => await db.PushSubscriptions
            .Where(s => s.UtilisateurId == utilisateurId)
            .ToListAsync();

    public async Task DeleteAsync(string endpoint)
    {
        await db.PushSubscriptions.Where(s => s.Endpoint == endpoint).ExecuteDeleteAsync();
    }
}
