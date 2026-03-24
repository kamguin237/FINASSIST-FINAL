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
    Task NotifierSoumissionAsync(int besoinId, int soumetteurId);
    Task NotifierValidationN1Async(int besoinId, DecisionValidation decision, int agentId);
    Task NotifierValidationN2Async(int besoinId, DecisionValidation decision, int agentId);
    Task NotifierSignatureAsync(int besoinId, int agentId);
}
