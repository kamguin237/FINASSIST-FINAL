using System.ComponentModel.DataAnnotations;

namespace FinAssist.Core.Entities;

public class WorkflowCircuit
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Nom { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string NomCreateur { get; set; } = string.Empty;
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public DateTime DateModification { get; set; } = DateTime.UtcNow;

    public ICollection<EtapeCircuit> Etapes { get; set; } = [];
}
