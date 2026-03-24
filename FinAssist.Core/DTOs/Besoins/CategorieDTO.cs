using FinAssist.Core.DTOs.Workflow;

namespace FinAssist.Core.DTOs.Besoins;

public class CategorieDTO
{
    public int Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime DateCreation { get; set; }
    public int? WorkflowCircuitId { get; set; }
    public string? WorkflowCircuitNom { get; set; }
}

public class CategorieDetailDTO : CategorieDTO
{
    public WorkflowCircuitDTO? Circuit { get; set; }
}
