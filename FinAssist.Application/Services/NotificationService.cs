using FinAssist.Core.DTOs.Notifications;
using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;

namespace FinAssist.Application.Services;

public class NotificationService(
    INotificationRepository notifRepo,
    IFirebaseNotificationService firebase) : INotificationService
{
    public async Task<IEnumerable<NotificationDTO>> GetMesNotificationsAsync(int utilisateurId)
    {
        var items = await notifRepo.GetByUtilisateurAsync(utilisateurId);
        return items.Select(un => new NotificationDTO
        {
            Id = un.NotificationId,
            Message = un.Notification.Message,
            Type = un.Notification.Type.ToString(),
            Lu = un.Lu,
            DateEnvoi = un.Notification.DateEnvoi
        });
    }

    public async Task MarquerLuAsync(int notificationId, int utilisateurId)
        => await notifRepo.MarquerLuAsync(notificationId, utilisateurId);

    public async Task<NotificationDTO> CreerEtEnvoyerAsync(CreateNotificationDTO dto)
    {
        if (!Enum.TryParse<TypeNotification>(dto.Type, ignoreCase: true, out var type))
            throw new ArgumentException(
                $"Type invalide : '{dto.Type}'. Valeurs acceptées : {string.Join(", ", Enum.GetNames<TypeNotification>())}.");

        var notif = new Notification
        {
            Message = dto.Message,
            Type = type,
            DateEnvoi = DateTime.UtcNow
        };

        await notifRepo.CreateAsync(notif, dto.DestinataireIds);
        await firebase.SendAsync(dto.DestinataireIds, "Notification", dto.Message);

        return new NotificationDTO
        {
            Id = notif.Id,
            Message = notif.Message,
            Type = notif.Type.ToString(),
            Lu = false,
            DateEnvoi = notif.DateEnvoi
        };
    }

    // Soumission → notifier tous les Responsables
    public async Task NotifierSoumissionAsync(int besoinId, int soumetteurId)
    {
        var responsables = await notifRepo.GetUtilisateurIdsByRoleAsync("Responsable");
        var ids = responsables.ToList();
        if (ids.Count == 0) return;

        var notif = new Notification
        {
            Message = $"Nouvelle demande #{besoinId} soumise et en attente de validation N1.",
            Type = TypeNotification.ACCUSE_RECEPTION,
            DateEnvoi = DateTime.UtcNow
        };
        await notifRepo.CreateAsync(notif, ids);
        await firebase.SendAsync(ids, "Nouvelle demande", notif.Message);
    }

    // Validation N1 → APPROUVE : notifier Direction | REJETE : notifier l'agent
    public async Task NotifierValidationN1Async(int besoinId, DecisionValidation decision, int agentId)
    {
        if (decision == DecisionValidation.APPROUVE)
        {
            var direction = await notifRepo.GetUtilisateurIdsByRoleAsync("Direction");
            var ids = direction.ToList();
            if (ids.Count == 0) return;

            var notif = new Notification
            {
                Message = $"Demande #{besoinId} validée N1 — en attente de votre approbation (N2).",
                Type = TypeNotification.VALIDATION,
                DateEnvoi = DateTime.UtcNow
            };
            await notifRepo.CreateAsync(notif, ids);
            await firebase.SendAsync(ids, "Validation N1 approuvée", notif.Message);
        }
        else if (decision == DecisionValidation.REJETE)
        {
            var notif = new Notification
            {
                Message = $"Votre demande #{besoinId} a été rejetée au niveau N1.",
                Type = TypeNotification.REJET,
                DateEnvoi = DateTime.UtcNow
            };
            await notifRepo.CreateAsync(notif, [agentId]);
            await firebase.SendAsync([agentId], "Demande rejetée", notif.Message);
        }
    }

    // Validation N2 → notifier l'agent + les Responsables
    public async Task NotifierValidationN2Async(int besoinId, DecisionValidation decision, int agentId)
    {
        var responsables = await notifRepo.GetUtilisateurIdsByRoleAsync("Responsable");
        var destinataires = responsables.Append(agentId).Distinct().ToList();

        var (message, type) = decision == DecisionValidation.APPROUVE
            ? ($"Demande #{besoinId} approuvée — signature électronique requise.", TypeNotification.SIGNATURE_REQUISE)
            : ($"Demande #{besoinId} rejetée au niveau N2.", TypeNotification.REJET);

        var notif = new Notification { Message = message, Type = type, DateEnvoi = DateTime.UtcNow };
        await notifRepo.CreateAsync(notif, destinataires);
        await firebase.SendAsync(destinataires, decision == DecisionValidation.APPROUVE ? "Demande approuvée" : "Demande rejetée", message);
    }

    // Signature apposée → notifier l'agent initiateur
    public async Task NotifierSignatureAsync(int besoinId, int agentId)
    {
        var notif = new Notification
        {
            Message = $"La demande #{besoinId} a été signée électroniquement.",
            Type = TypeNotification.SIGNATURE_REQUISE,
            DateEnvoi = DateTime.UtcNow
        };
        await notifRepo.CreateAsync(notif, [agentId]);
        await firebase.SendAsync([agentId], "Signature apposée", notif.Message);
    }
}
