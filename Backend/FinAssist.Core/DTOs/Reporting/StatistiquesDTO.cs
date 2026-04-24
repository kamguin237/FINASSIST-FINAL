namespace FinAssist.Core.DTOs.Reporting;

public class StatistiquesDTO
{
    public int TotalBesoins { get; set; }
    public int TotalUtilisateurs { get; set; }
    public int TotalActifs { get; set; }
    public Dictionary<string, int> BesoinsByStatut { get; set; } = [];
    public Dictionary<string, int> BesoinsByCategorie { get; set; } = [];
    public int SignaturesApposees { get; set; }
    public int NotificationsEnvoyees { get; set; }
}
