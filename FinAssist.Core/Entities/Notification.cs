namespace FinAssist.Core.Entities;

public class Notification
{
    public int Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public TypeNotification Type { get; set; }
    public bool Lu { get; set; } = false;
    public DateTime DateEnvoi { get; set; } = DateTime.UtcNow;

    // Table de jonction N:N
    public ICollection<UtilisateurNotification> UtilisateurNotifications { get; set; } = [];
}
