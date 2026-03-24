using FinAssist.API.Attributes;
using FinAssist.Core.DTOs.Reporting;
using FinAssist.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinAssist.API.Controllers;

[ApiController]
[Route("api/reporting")]
[Authorize]
public class ReportingController(IReportingService reportingService) : ControllerBase
{
    private int UtilisateurId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private string RoleCode =>
        User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    [HttpGet("statistiques")]
    [RequirePermission("RAPPORT_CONSULTER")]
    public async Task<IActionResult> GetStatistiques()
        => Ok(await reportingService.GetStatistiquesAsync());

    [HttpGet("besoins")]
    [RequirePermission("RAPPORT_CONSULTER")]
    public async Task<IActionResult> GetRapportBesoins([FromQuery] FiltreRapportDTO? filtres)
        => Ok(await reportingService.GetRapportBesoinsAsync(filtres, UtilisateurId));

    [HttpGet("dashboard")]
    [RequirePermission("DASHBOARD_CONSULTER")]
    public async Task<IActionResult> GetDashboard()
        => Ok(await reportingService.GetDashboardAsync(UtilisateurId, RoleCode));

    [HttpPost("exporter")]
    [RequirePermission("RAPPORT_EXPORTER")]
    public async Task<IActionResult> Exporter([FromBody] ExportRequestDTO request)
    {
        var (contenu, contentType, nomFichier) =
            await reportingService.ExporterAsync(request, UtilisateurId);
        return File(contenu, contentType, nomFichier);
    }
}
