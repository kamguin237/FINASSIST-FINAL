using FinAssist.Core.Entities;

namespace FinAssist.Core.Interfaces;

public interface INotificationRepository
{
    Task<Notification> CreateAsync(Notification notification, IEnumerable<int> destinataireIds);
    Task<IEnumerable<UtilisateurNotification>> GetByUtilisateurAsync(int utilisateurId);
    Task<UtilisateurNotification?> GetUtilisateurNotificationAsync(int notificationId, int utilisateurId);
    Task MarquerLuAsync(int notificationId, int utilisateurId);
    Task<IEnumerable<int>> GetUtilisateurIdsByRoleAsync(string roleCode);
}
