using System.ComponentModel.DataAnnotations;

namespace FinAssist.Core.DTOs.Notifications;

public class CreateNotificationDTO
{
    [Required]
    public string Message { get; set; } = string.Empty;

    /// <summary>Type : ACCUSE_RECEPTION, VALIDATION, REJET, SIGNATURE_REQUISE</summary>
    [Required]
    public string Type { get; set; } = string.Empty;

    /// <summary>Liste des IDs utilisateurs destinataires</summary>
    [Required, MinLength(1)]
    public List<int> DestinataireIds { get; set; } = [];
}
