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
        var publicKey  = config["Vapid:PublicKey"]!;
        var privateKey = config["Vapid:PrivateKey"]!;
        var subject    = config["Vapid:Subject"] ?? "mailto:admin@finassist.com";

        var client = new WebPushClient();
        client.SetVapidDetails(subject, publicKey, privateKey);

        var payload = System.Text.Json.JsonSerializer.Serialize(new
        {
            titre,
            message,
            url = url ?? "/notifications",
            icon = "/favicon.svg"
        });

        foreach (var sub in subs)
        {
            try
            {
                var pushSub = new WebPush.PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);
                await client.SendNotificationAsync(pushSub, payload);
            }
            catch (WebPushException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Gone)
            {
                // Subscription expirée — supprimer
                await repo.DeleteAsync(sub.Endpoint);
            }
            catch (Exception ex)
            {
                logger.LogWarning("Push échoué pour {Endpoint}: {Error}", sub.Endpoint, ex.Message);
            }
        }
    }
}
