namespace FinAssist.Core.DTOs.Settings;

public class UserPreferencesDTO
{
    public bool NotifApp { get; set; }
    public bool NotifEmail { get; set; }
    public bool AlertNouveauBesoin { get; set; }
    public bool AlertValidation { get; set; }
    public bool AlertEnAttente { get; set; }
    public string Langue { get; set; } = "fr";
    public string FormatDate { get; set; } = "dd/MM/yyyy";
    public string FuseauHoraire { get; set; } = "Africa/Douala";
    public int ItemsParPage { get; set; }
    public string PageAccueil { get; set; } = "/dashboard";
    public string TriDefaut { get; set; } = "dateDesc";
}
