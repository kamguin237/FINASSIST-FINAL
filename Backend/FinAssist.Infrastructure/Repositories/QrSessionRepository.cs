using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;
using FinAssist.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinAssist.Infrastructure.Repositories;

public class QrSessionRepository(AppDbContext db) : IQrSessionRepository
{
    public async Task<QrSignatureSession> CreateAsync(QrSignatureSession session)
    {
        db.QrSignatureSessions.Add(session);
        await db.SaveChangesAsync();
        return session;
    }

    public Task<QrSignatureSession?> GetByTokenAsync(string token)
        => db.QrSignatureSessions
             .Include(s => s.Utilisateur)
             .FirstOrDefaultAsync(s => s.Token == token);

    public async Task UpdateAsync(QrSignatureSession session)
    {
        db.QrSignatureSessions.Update(session);
        await db.SaveChangesAsync();
    }
}
