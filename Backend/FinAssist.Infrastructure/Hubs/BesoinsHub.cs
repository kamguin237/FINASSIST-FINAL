using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace FinAssist.Infrastructure.Hubs;

[Authorize]
public class BesoinsHub : Hub
{
    // À la connexion, rejoindre automatiquement le groupe personnel de l'utilisateur
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        }
        await base.OnConnectedAsync();
    }

    public async Task JoinBesoinGroup(int besoinId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"besoin_{besoinId}");
    }

    public async Task LeaveBesoinGroup(int besoinId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"besoin_{besoinId}");
    }
}
