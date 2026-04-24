using FinAssist.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace FinAssist.Infrastructure.Services;

/// <summary>
/// Stub Firebase Cloud Messaging.
/// Remplacer l'implémentation par l'intégration FCM réelle (FirebaseAdmin SDK).
/// </summary>
public class FirebaseNotificationService(ILogger<FirebaseNotificationService> logger) : IFirebaseNotificationService
{
    public Task SendAsync(IEnumerable<int> utilisateurIds, string titre, string corps)
    {
        // TODO: intégrer FirebaseAdmin SDK
        // var message = new MulticastMessage { Tokens = fcmTokens, Notification = new() { Title = titre, Body = corps } };
        // await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message);

        logger.LogInformation("[FCM-STUB] Push → utilisateurs [{Ids}] | {Titre} : {Corps}",
            string.Join(",", utilisateurIds), titre, corps);
        return Task.CompletedTask;
    }
}
