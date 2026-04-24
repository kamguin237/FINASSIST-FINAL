namespace FinAssist.Core.Entities;

public class Categorie
{
    public int Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;

    // FK vers le circuit de validation actif
    public int? WorkflowCircuitId { get; set; }
    public WorkflowCircuit? WorkflowCircuit { get; set; }

    public ICollection<Besoin> Besoins { get; set; } = [];
}
