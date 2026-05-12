using FinAssist.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WebPush;

namespace FinAssist.Infrastructure.Services;

public class WebPushService(
    IPushSubscriptionRepository repo,
    IConfiguration config,
    ILogger<WebPushService> logger) : IWebPushService
{
    public async Task SendToUserAsync(int utilisateurId, string titre, string message, string? url = null)
    {
        var subs = await repo.GetByUtilisateurAsync(utilisateurId);
        var subList = subs.ToList();
        if (subList.Count == 0)
        {
            logger.LogDebug("Aucune subscription push pour l'utilisateur {UserId}", utilisateurId);
            return;
        }

        var publicKey  = config["Vapid:PublicKey"];
        var privateKey = config["Vapid:PrivateKey"];
        var subject    = config["Vapid:Subject"] ?? "mailto:admin@finassist.com";

        if (string.IsNullOrEmpty(publicKey) || string.IsNullOrEmpty(privateKey))
        {
            logger.LogError("Clés VAPID manquantes dans la configuration. Push non envoyé pour l'utilisateur {UserId}", utilisateurId);
            return;
        }

        var client = new WebPushClient();
        client.SetVapidDetails(subject, publicKey, privateKey);

        var payload = System.Text.Json.JsonSerializer.Serialize(new
        {
            titre,
            message,
            url = url ?? "/notifications",
            icon = "/favicon.svg"
        });

        foreach (var sub in subList)
        {
            try
            {
                var pushSub = new WebPush.PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);
                await client.SendNotificationAsync(pushSub, payload);
                logger.LogInformation("Push envoyé à l'utilisateur {UserId} via {Endpoint}", utilisateurId, sub.Endpoint[..Math.Min(40, sub.Endpoint.Length)]);
            }
            catch (WebPushException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Gone)
            {
                logger.LogInformation("Subscription expirée supprimée pour {Endpoint}", sub.Endpoint[..Math.Min(40, sub.Endpoint.Length)]);
                await repo.DeleteAsync(sub.Endpoint);
            }
            catch (Exception ex)
            {
                logger.LogWarning("Push échoué pour {Endpoint}: {Error}", sub.Endpoint[..Math.Min(40, sub.Endpoint.Length)], ex.Message);
            }
        }
    }
}
