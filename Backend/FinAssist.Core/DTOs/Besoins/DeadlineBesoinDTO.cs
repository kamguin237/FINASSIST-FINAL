namespace FinAssist.Core.DTOs.Besoins;

public class DeadlineBesoinDTO
{
    public int Id { get; set; }
    public string Titre { get; set; } = string.Empty;
    public string Statut { get; set; } = string.Empty;
    public string CategorieNom { get; set; } = string.Empty;
    public string UtilisateurNom { get; set; } = string.Empty;
    public DateTime? DateEntreeEnAttente { get; set; }
    public int DelaiMaxMinutes { get; set; }
    public double PourcentageEcoule { get; set; }
    public double MinutesRestantes { get; set; }
    public bool Rappel1Envoye { get; set; }
    public bool Rappel2Envoye { get; set; }
    public bool EmailRappelEnvoye { get; set; }
    public bool RejeteAutomatiquement { get; set; }
    public string EtapeRole { get; set; } = string.Empty;
    public string Urgence { get; set; } = "normal"; // normal | warning | danger | expired
}
