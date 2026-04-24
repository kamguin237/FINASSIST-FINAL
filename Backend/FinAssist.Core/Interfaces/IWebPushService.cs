namespace FinAssist.Core.Interfaces;

public interface IWebPushService
{
    Task SendToUserAsync(int utilisateurId, string titre, string message, string? url = null);
}
