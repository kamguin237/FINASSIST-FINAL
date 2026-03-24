using FinAssist.API.Attributes;
using FinAssist.Core.DTOs.Notifications;
using FinAssist.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinAssist.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController(INotificationService notificationService) : ControllerBase
{
    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    [RequirePermission("NOTIFICATION_LIRE")]
    public async Task<IActionResult> GetMesNotifications()
        => Ok(await notificationService.GetMesNotificationsAsync(CurrentUserId));

    [HttpPost]
    [RequirePermission("NOTIFICATION_ENVOYER")]
    public async Task<IActionResult> Creer([FromBody] CreateNotificationDTO dto)
    {
        try { return Ok(await notificationService.CreerEtEnvoyerAsync(dto)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPut("{id:int}/lire")]
    [RequirePermission("NOTIFICATION_LIRE")]
    public async Task<IActionResult> MarquerLu(int id)
    {
        try
        {
            await notificationService.MarquerLuAsync(id, CurrentUserId);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }
}
