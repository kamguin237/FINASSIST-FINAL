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

    // Soumission -> notifier les utilisateurs du role de la premiere etape
    public async Task NotifierSoumissionAsync(int besoinId, string titreBesoin, int soumetteurId, string roleEtape1, string nomSoumetteur)
    {
        var ids = (await notifRepo.GetUtilisateurIdsByRoleAsync(roleEtape1)).ToList();
        if (ids.Count == 0) return;

        var notif = new Notification
        {
            Message = $"La demande \u00ab {titreBesoin} \u00bb a \u00e9t\u00e9 soumise par \u00ab {nomSoumetteur} \u00bb et attend votre validation.",
            Type = TypeNotification.ACCUSE_RECEPTION,
            DateEnvoi = DateTime.UtcNow
        };
        await notifRepo.CreateAsync(notif, ids);
        await firebase.SendAsync(ids, "Nouvelle demande", notif.Message);
    }

    // Transmission -> notifier les utilisateurs du role de la prochaine etape
    public async Task NotifierTransmissionAsync(int besoinId, string titreBesoin, string roleProchaineEtape, string nomTransmetteur)
    {
        var ids = (await notifRepo.GetUtilisateurIdsByRoleAsync(roleProchaineEtape)).ToList();
        if (ids.Count == 0) return;

        var notif = new Notification
        {
            Message = $"La demande \u00ab {titreBesoin} \u00bb vous a \u00e9t\u00e9 transmise par \u00ab {nomTransmetteur} \u00bb et attend votre validation.",
            Type = TypeNotification.VALIDATION,
            DateEnvoi = DateTime.UtcNow
        };
        await notifRepo.CreateAsync(notif, ids);
        await firebase.SendAsync(ids, "Demande transmise", notif.Message);
    }

    // Rejet -> notifier le createur du besoin
    public async Task NotifierRejetAsync(int besoinId, string titreBesoin, int createurId, string roleEtape)
    {
        var notif = new Notification
        {
            Message = $"Votre demande \u00ab {titreBesoin} \u00bb a \u00e9t\u00e9 rejet\u00e9e par le r\u00f4le \u00ab {roleEtape} \u00bb.",
            Type = TypeNotification.REJET,
            DateEnvoi = DateTime.UtcNow
        };
        await notifRepo.CreateAsync(notif, [createurId]);
        await firebase.SendAsync([createurId], "Demande rejet\u00e9e", notif.Message);
    }

    // Signature apposee -> notifier le createur du besoin
    public async Task NotifierSignatureAsync(int besoinId, string titreBesoin, int agentId, string roleSignataire)
    {
        var notif = new Notification
        {
            Message = $"La demande \u00ab {titreBesoin} \u00bb a \u00e9t\u00e9 sign\u00e9e \u00e9lectroniquement par le \u00ab {roleSignataire} \u00bb.",
            Type = TypeNotification.SIGNATURE_REQUISE,
            DateEnvoi = DateTime.UtcNow
        };
        await notifRepo.CreateAsync(notif, [agentId]);
        await firebase.SendAsync([agentId], "Signature appos\u00e9e", notif.Message);
    }
}
