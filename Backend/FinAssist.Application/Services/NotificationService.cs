using FinAssist.Core.DTOs.Notifications;
using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;

namespace FinAssist.Application.Services;

public class NotificationService(
    INotificationRepository notifRepo,
    IUserPreferencesRepository prefsRepo,
    IFirebaseNotificationService firebase,
    IWebPushService webPush,
    IBesoinsHubService hubService) : INotificationService
{
    // Vérifie si un utilisateur a activé les notifs in-app
    private async Task<bool> NotifAppActive(int userId)
    {
        var p = await prefsRepo.GetByUtilisateurIdAsync(userId);
        return p?.NotifApp ?? true; // par défaut actif
    }

    // Filtre une liste d'IDs selon leur préférence notifApp
    private async Task<List<int>> FiltrerNotifApp(IEnumerable<int> ids)
    {
        var result = new List<int>();
        foreach (var id in ids)
            if (await NotifAppActive(id)) result.Add(id);
        return result;
    }

    // Vérifie un flag spécifique pour un utilisateur
    private async Task<bool> FlagActif(int userId, Func<UserPreferences, bool> selector)
    {
        var p = await prefsRepo.GetByUtilisateurIdAsync(userId);
        return p is null || selector(p); // par défaut actif si pas de préfs
    }

    private async Task<List<int>> FiltrerParFlag(IEnumerable<int> ids, Func<UserPreferences, bool> selector)
    {
        var result = new List<int>();
        foreach (var id in ids)
            if (await FlagActif(id, selector)) result.Add(id);
        return result;
    }
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
        var pushTasks = dto.DestinataireIds.Select(id =>
            webPush.SendToUserAsync(id, "Notification FinAssist", dto.Message));
        var hubTasks = dto.DestinataireIds.Select(id =>
            hubService.NotifierUtilisateurAsync(id, dto.Message, type.ToString()));
        await Task.WhenAll(pushTasks.Concat(hubTasks));

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

        // Filtrer selon alertNouveauBesoin + notifApp
        var idsFiltered = await FiltrerParFlag(ids, p => p.AlertNouveauBesoin && p.NotifApp);
        if (idsFiltered.Count == 0) return;

        var notif = new Notification
        {
            Message = $"La demande \u00ab {titreBesoin} \u00bb a \u00e9t\u00e9 soumise par \u00ab {nomSoumetteur} \u00bb et attend votre validation.",
            Type = TypeNotification.ACCUSE_RECEPTION,
            DateEnvoi = DateTime.UtcNow
        };
        await notifRepo.CreateAsync(notif, idsFiltered);
        await firebase.SendAsync(idsFiltered, "Nouvelle demande", notif.Message);
        await Task.WhenAll(
            Task.WhenAll(idsFiltered.Select(id => webPush.SendToUserAsync(id, "Nouvelle demande", notif.Message))),
            Task.WhenAll(idsFiltered.Select(id => hubService.NotifierUtilisateurAsync(id, notif.Message, notif.Type.ToString())))
        );
    }

    // Transmission -> notifier les utilisateurs du role de la prochaine etape
    public async Task NotifierTransmissionAsync(int besoinId, string titreBesoin, string roleProchaineEtape, string nomTransmetteur)
    {
        var ids = (await notifRepo.GetUtilisateurIdsByRoleAsync(roleProchaineEtape)).ToList();
        if (ids.Count == 0) return;

        // Filtrer selon alertEnAttente + notifApp
        var idsFiltered = await FiltrerParFlag(ids, p => p.AlertEnAttente && p.NotifApp);
        if (idsFiltered.Count == 0) return;

        var notif = new Notification
        {
            Message = $"La demande « {titreBesoin} » vous a été transmise par « {nomTransmetteur} » et attend votre validation.",
            Type = TypeNotification.VALIDATION,
            DateEnvoi = DateTime.UtcNow
        };
        await notifRepo.CreateAsync(notif, idsFiltered);
        await firebase.SendAsync(idsFiltered, "Demande transmise", notif.Message);
        await Task.WhenAll(
            Task.WhenAll(idsFiltered.Select(id => webPush.SendToUserAsync(id, "Demande transmise", notif.Message))),
            Task.WhenAll(idsFiltered.Select(id => hubService.NotifierUtilisateurAsync(id, notif.Message, notif.Type.ToString())))
        );
    }

    // Rejet -> notifier le createur du besoin
    public async Task NotifierRejetAsync(int besoinId, string titreBesoin, int createurId, string roleEtape)
    {
        if (!await FlagActif(createurId, p => p.AlertValidation && p.NotifApp)) return;

        var notif = new Notification
        {
            Message = $"Votre demande \u00ab {titreBesoin} \u00bb a \u00e9t\u00e9 rejet\u00e9e par le r\u00f4le \u00ab {roleEtape} \u00bb.",
            Type = TypeNotification.REJET,
            DateEnvoi = DateTime.UtcNow
        };
        await notifRepo.CreateAsync(notif, [createurId]);
        await firebase.SendAsync([createurId], "Demande rejetée", notif.Message);
        await webPush.SendToUserAsync(createurId, "Demande rejetée", notif.Message);
        await hubService.NotifierUtilisateurAsync(createurId, notif.Message, notif.Type.ToString());
    }

    // Signature apposee -> notifier le createur du besoin
    public async Task NotifierSignatureAsync(int besoinId, string titreBesoin, int agentId, string roleSignataire)
    {
        if (!await FlagActif(agentId, p => p.AlertValidation && p.NotifApp)) return;

        var notif = new Notification
        {
            Message = $"La demande \u00ab {titreBesoin} \u00bb a \u00e9t\u00e9 sign\u00e9e \u00e9lectroniquement par le \u00ab {roleSignataire} \u00bb.",
            Type = TypeNotification.SIGNATURE_REQUISE,
            DateEnvoi = DateTime.UtcNow
        };
        await notifRepo.CreateAsync(notif, [agentId]);
        await firebase.SendAsync([agentId], "Signature apposée", notif.Message);
        await webPush.SendToUserAsync(agentId, "Signature apposée", notif.Message);
        await hubService.NotifierUtilisateurAsync(agentId, notif.Message, notif.Type.ToString());
    }

    public async Task EnvoyerRappelAsync(int utilisateurId, int besoinId, string titreBesoin, int niveau, string message)
    {
        if (!await FlagActif(utilisateurId, p => p.AlertEnAttente && p.NotifApp)) return;

        var notif = new Notification
        {
            Message = message,
            Type = TypeNotification.RAPPEL,
            DateEnvoi = DateTime.UtcNow
        };
        await notifRepo.CreateAsync(notif, [utilisateurId]);

        var titre = niveau >= 3 ? "🚨 Expiration imminente" : niveau >= 2 ? "⚠ Urgent" : "🔔 Rappel";
        await firebase.SendAsync([utilisateurId], titre, message);
        await webPush.SendToUserAsync(utilisateurId, titre, message);
        await hubService.NotifierUtilisateurAsync(utilisateurId, message, TypeNotification.RAPPEL.ToString());
    }

    public async Task EnvoyerAlertExpirationAsync(int utilisateurId, int besoinId, string titreBesoin, string message)
    {
        if (!await FlagActif(utilisateurId, p => p.AlertEnAttente && p.NotifApp)) return;

        var notif = new Notification
        {
            Message = message,
            Type = TypeNotification.RAPPEL,
            DateEnvoi = DateTime.UtcNow
        };
        await notifRepo.CreateAsync(notif, [utilisateurId]);

        // SignalR uniquement — pas de WebPush, l'email est géré séparément par ValidationDeadlineService
        _ = hubService.NotifierUtilisateurAsync(utilisateurId, message, TypeNotification.RAPPEL.ToString()).ConfigureAwait(false);
    }
}
