using FinAssist.Core.Interfaces;
using FinAssist.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace FinAssist.Infrastructure.Services;

public class BesoinsHubService(IHubContext<BesoinsHub> hubContext) : IBesoinsHubService
{
    public async Task NotifierHistoriqueAsync(int besoinId, string action, string description)
    {
        await hubContext.Clients.Group($"besoin_{besoinId}").SendAsync("HistoriqueUpdated", new
        {
            BesoinId = besoinId,
            Action = action,
            Description = description,
            DateAction = DateTime.UtcNow
        });
    }

    public async Task NotifierUtilisateurAsync(int utilisateurId, string message, string type)
    {
        await hubContext.Clients.Group($"user_{utilisateurId}").SendAsync("NouvelleNotification", new
        {
            Message = message,
            Type = type,
            DateEnvoi = DateTime.UtcNow,
            Lu = false
        });
    }

    public async Task NotifierStatutBesoinAsync(int besoinId, string nouveauStatut)
    {
        // Diffuse à tous les clients connectés pour mettre à jour la liste des besoins
        await hubContext.Clients.All.SendAsync("BesoinStatutUpdated", new
        {
            BesoinId = besoinId,
            NouveauStatut = nouveauStatut
        });
    }
}
