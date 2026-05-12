namespace FinAssist.Core.Interfaces;

public interface IBesoinsHubService
{
    Task NotifierHistoriqueAsync(int besoinId, string action, string description);
    Task NotifierUtilisateurAsync(int utilisateurId, string message, string type);
    Task NotifierStatutBesoinAsync(int besoinId, string nouveauStatut);
}
