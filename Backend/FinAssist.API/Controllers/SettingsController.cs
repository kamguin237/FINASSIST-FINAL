using FinAssist.Core.DTOs.Settings;
using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinAssist.API.Controllers;

[ApiController]
[Route("api/settings")]
[Authorize]
public class SettingsController(IUserPreferencesRepository repo) : ControllerBase
{
    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var prefs = await repo.GetByUtilisateurIdAsync(CurrentUserId);
        if (prefs is null)
            return Ok(new UserPreferencesDTO()); // retourne les valeurs par défaut
        return Ok(ToDTO(prefs));
    }

    [HttpPut]
    public async Task<IActionResult> Save([FromBody] UserPreferencesDTO dto)
    {
        var prefs = new UserPreferences
        {
            UtilisateurId       = CurrentUserId,
            NotifApp            = dto.NotifApp,
            NotifEmail          = dto.NotifEmail,
            AlertNouveauBesoin  = dto.AlertNouveauBesoin,
            AlertValidation     = dto.AlertValidation,
            AlertEnAttente      = dto.AlertEnAttente,
            Langue              = dto.Langue,
            FormatDate          = dto.FormatDate,
            FuseauHoraire       = dto.FuseauHoraire,
            ItemsParPage        = dto.ItemsParPage,
            PageAccueil         = dto.PageAccueil,
            TriDefaut           = dto.TriDefaut,
            DateModification    = DateTime.UtcNow
        };

        var saved = await repo.SaveAsync(prefs);
        return Ok(ToDTO(saved));
    }

    private static UserPreferencesDTO ToDTO(UserPreferences p) => new()
    {
        NotifApp           = p.NotifApp,
        NotifEmail         = p.NotifEmail,
        AlertNouveauBesoin = p.AlertNouveauBesoin,
        AlertValidation    = p.AlertValidation,
        AlertEnAttente     = p.AlertEnAttente,
        Langue             = p.Langue,
        FormatDate         = p.FormatDate,
        FuseauHoraire      = p.FuseauHoraire,
        ItemsParPage       = p.ItemsParPage,
        PageAccueil        = p.PageAccueil,
        TriDefaut          = p.TriDefaut
    };
}
