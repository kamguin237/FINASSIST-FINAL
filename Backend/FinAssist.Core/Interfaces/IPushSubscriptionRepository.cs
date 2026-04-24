using FinAssist.Core.Entities;

namespace FinAssist.Core.Interfaces;

public interface IPushSubscriptionRepository
{
    Task SaveAsync(PushSubscription sub);
    Task<IEnumerable<PushSubscription>> GetByUtilisateurAsync(int utilisateurId);
    Task DeleteAsync(string endpoint);
}
