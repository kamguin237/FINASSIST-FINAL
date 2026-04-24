namespace FinAssist.Core.Interfaces;

/// <summary>
/// Abstraction pour l'envoi de push notifications via Firebase Cloud Messaging.
/// </summary>
public interface IFirebaseNotificationService
{
    Task SendAsync(IEnumerable<int> utilisateurIds, string titre, string corps);
}
