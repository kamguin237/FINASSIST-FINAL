using FinAssist.API.Attributes;
using FinAssist.Core.DTOs.Logs;
using FinAssist.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinAssist.API.Controllers;

[ApiController]
[Route("api/logs")]
[Authorize]
public class LogsController(ILogService logService) : ControllerBase
{
    [HttpGet]
    [RequirePermission("LOG_CONSULTER")]
    public async Task<IActionResult> GetLogs([FromQuery] FiltreLogsDTO filtres)
    {
        var (items, total) = await logService.GetLogsAsync(filtres);
        return Ok(new { total, page = filtres.Page, pageSize = filtres.PageSize, items });
    }
}
