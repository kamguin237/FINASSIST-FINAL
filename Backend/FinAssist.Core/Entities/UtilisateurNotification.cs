namespace FinAssist.Core.Entities;

public class UtilisateurNotification
{
    public int UtilisateurId { get; set; }
    public Utilisateur Utilisateur { get; set; } = null!;

    public int NotificationId { get; set; }
    public Notification Notification { get; set; } = null!;

    public bool Lu { get; set; } = false;
    public DateTime? DateLecture { get; set; }
}
