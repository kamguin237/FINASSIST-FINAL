using System.ComponentModel.DataAnnotations;

namespace FinAssist.Core.DTOs.Workflow;

public class WorkflowCircuitDTO
{
    public int Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string NomCreateur { get; set; } = string.Empty;
    public DateTime DateCreation { get; set; }
    public DateTime DateModification { get; set; }
    public List<EtapeCircuitDTO> Etapes { get; set; } = [];
}

public class CreateWorkflowCircuitDTO
{
    [Required, MaxLength(100)]
    public string Nom { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required, MinLength(1)]
    public List<CreateEtapeCircuitDTO> Etapes { get; set; } = [];
}
