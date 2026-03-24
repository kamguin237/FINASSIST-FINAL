namespace FinAssist.Core.DTOs.Reporting;

public class DashboardDTO
{
    public int BesoinsEnAttente { get; set; }
    public int BesoinsApprouves { get; set; }
    public int BesoinsRejetes { get; set; }
    public int BesoinsSoumis { get; set; }
    public int BesoinsSignes { get; set; }
    public int NotificationsNonLues { get; set; }
    public IEnumerable<BesoinRecent> DerniersBesoins { get; set; } = [];
}

public class BesoinRecent
{
    public int Id { get; set; }
    public string Titre { get; set; } = string.Empty;
    public string Statut { get; set; } = string.Empty;
    public DateTime DateModification { get; set; }
}
