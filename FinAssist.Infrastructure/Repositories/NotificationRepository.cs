using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;
using FinAssist.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinAssist.Infrastructure.Repositories;

public class NotificationRepository(AppDbContext db) : INotificationRepository
{
    public async Task<Notification> CreateAsync(Notification notification, IEnumerable<int> destinataireIds)
    {
        db.Notifications.Add(notification);
        await db.SaveChangesAsync();

        foreach (var uid in destinataireIds)
        {
            db.UtilisateurNotifications.Add(new UtilisateurNotification
            {
                UtilisateurId = uid,
                NotificationId = notification.Id
            });
        }
        await db.SaveChangesAsync();
        return notification;
    }

    public Task<IEnumerable<UtilisateurNotification>> GetByUtilisateurAsync(int utilisateurId)
        => Task.FromResult<IEnumerable<UtilisateurNotification>>(
            db.UtilisateurNotifications
              .Include(un => un.Notification)
              .Where(un => un.UtilisateurId == utilisateurId)
              .OrderByDescending(un => un.Notification.DateEnvoi)
              .AsEnumerable());

    public Task<UtilisateurNotification?> GetUtilisateurNotificationAsync(int notificationId, int utilisateurId)
        => db.UtilisateurNotifications
             .Include(un => un.Notification)
             .FirstOrDefaultAsync(un => un.NotificationId == notificationId && un.UtilisateurId == utilisateurId);

    public async Task MarquerLuAsync(int notificationId, int utilisateurId)
    {
        var un = await GetUtilisateurNotificationAsync(notificationId, utilisateurId)
            ?? throw new KeyNotFoundException($"Notification {notificationId} introuvable pour cet utilisateur.");
        un.Lu = true;
        un.DateLecture = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public Task<IEnumerable<int>> GetUtilisateurIdsByRoleAsync(string roleCode)
        => Task.FromResult<IEnumerable<int>>(
            db.Utilisateurs
              .Include(u => u.Role)
              .Where(u => u.Role.Code == roleCode && u.Actif)
              .Select(u => u.Id)
              .AsEnumerable());
}
