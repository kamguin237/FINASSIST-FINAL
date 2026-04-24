using FinAssist.Core.DTOs.Notifications;
using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;

namespace FinAssist.Application.Services;

public class NotificationService(
    INotificationRepository notifRepo,
    IUserPreferencesRepository prefsRepo,
    IFirebaseNotificationService firebase,
    IWebPushService webPush) : INotificationService
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
        foreach (var id in dto.DestinataireIds)
            _ = webPush.SendToUserAsync(id, "Notification FinAssist", dto.Message).ConfigureAwait(false);

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
        foreach (var id in idsFiltered)
            _ = webPush.SendToUserAsync(id, "Nouvelle demande", notif.Message).ConfigureAwait(false);
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
        foreach (var id in idsFiltered)
            _ = webPush.SendToUserAsync(id, "Demande transmise", notif.Message).ConfigureAwait(false);
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
        _ = webPush.SendToUserAsync(createurId, "Demande rejetée", notif.Message).ConfigureAwait(false);
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
        _ = webPush.SendToUserAsync(agentId, "Signature apposée", notif.Message).ConfigureAwait(false);
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
        _ = webPush.SendToUserAsync(utilisateurId, niveau >= 2 ? "⚠ Urgent" : "🔔 Rappel", message).ConfigureAwait(false);
    }
}
