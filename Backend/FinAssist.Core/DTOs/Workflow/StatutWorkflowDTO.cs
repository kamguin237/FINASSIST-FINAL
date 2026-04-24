namespace FinAssist.Core.DTOs.Workflow;

public class StatutWorkflowDTO
{
    public int BesoinId { get; set; }
    public string Titre { get; set; } = string.Empty;
    public string Statut { get; set; } = string.Empty;
    public IEnumerable<ValidationDTO> Validations { get; set; } = [];
}
