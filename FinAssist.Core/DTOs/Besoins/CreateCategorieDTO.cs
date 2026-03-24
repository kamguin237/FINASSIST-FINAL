using System.ComponentModel.DataAnnotations;

namespace FinAssist.Core.DTOs.Besoins;

public class CreateCategorieDTO
{
    [Required]
    public string Nom { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>FK vers le WorkflowCircuit actif — obligatoire pour que les besoins puissent être validés</summary>
    [Required]
    public int WorkflowCircuitId { get; set; }
}
