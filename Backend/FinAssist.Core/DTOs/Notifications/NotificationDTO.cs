namespace FinAssist.Core.DTOs.Notifications;

public class NotificationDTO
{
    public int Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool Lu { get; set; }
    public DateTime DateEnvoi { get; set; }
}
