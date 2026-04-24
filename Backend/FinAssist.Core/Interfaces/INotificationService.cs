using FinAssist.Core.DTOs.Notifications;
using FinAssist.Core.Entities;

namespace FinAssist.Core.Interfaces;

public interface INotificationService
{
    Task<IEnumerable<NotificationDTO>> GetMesNotificationsAsync(int utilisateurId);
    Task MarquerLuAsync(int notificationId, int utilisateurId);

    // Création et envoi manuel
    Task<NotificationDTO> CreerEtEnvoyerAsync(CreateNotificationDTO dto);

    // Déclencheurs automatiques
    Task NotifierSoumissionAsync(int besoinId, string titreBesoin, int soumetteurId, string roleEtape1, string nomSoumetteur);
    Task NotifierTransmissionAsync(int besoinId, string titreBesoin, string roleProchaineEtape, string nomTransmetteur);
    Task NotifierRejetAsync(int besoinId, string titreBesoin, int createurId, string roleEtape);
    Task NotifierSignatureAsync(int besoinId, string titreBesoin, int agentId, string roleSignataire);
    Task EnvoyerRappelAsync(int utilisateurId, int besoinId, string titreBesoin, int niveau, string message);
}