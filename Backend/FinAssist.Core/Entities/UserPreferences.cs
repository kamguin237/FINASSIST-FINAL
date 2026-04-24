namespace FinAssist.Core.Entities;

public class UserPreferences
{
    public int Id { get; set; }
    public int UtilisateurId { get; set; }
    public Utilisateur Utilisateur { get; set; } = null!;

    // Notifications
    public bool NotifApp { get; set; } = true;
    public bool NotifEmail { get; set; } = false;
    public bool AlertNouveauBesoin { get; set; } = true;
    public bool AlertValidation { get; set; } = true;
    public bool AlertEnAttente { get; set; } = true;

    // Apparence
    public string Langue { get; set; } = "fr";
    public string FormatDate { get; set; } = "dd/MM/yyyy";
    public string FuseauHoraire { get; set; } = "Africa/Douala";

    // Préférences de travail
    public int ItemsParPage { get; set; } = 20;
    public string PageAccueil { get; set; } = "/dashboard";
    public string TriDefaut { get; set; } = "dateDesc";

    public DateTime DateModification { get; set; } = DateTime.UtcNow;
}
