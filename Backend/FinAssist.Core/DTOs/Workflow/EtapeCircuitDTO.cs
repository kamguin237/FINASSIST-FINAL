using System.ComponentModel.DataAnnotations;

namespace FinAssist.Core.DTOs.Workflow;

public class EtapeCircuitDTO
{
    public int Id { get; set; }
    public int Ordre { get; set; }
    public string RoleRequis { get; set; } = string.Empty;
    public bool ApprobationRequise { get; set; }
    public bool SignatureRequise { get; set; }
    public int DelaiMaxJours { get; set; }
    public bool EstDerniereEtape { get; set; }

    /// <summary>Statut généré dynamiquement après cette étape</summary>
    public string StatutApres { get; set; } = string.Empty;
}

public class CreateEtapeCircuitDTO
{
    [Required, Range(1, 100)]
    public int Ordre { get; set; }

    [Required, MaxLength(50)]
    public string RoleRequis { get; set; } = string.Empty;

    public bool ApprobationRequise { get; set; } = true;
    public bool SignatureRequise { get; set; } = false;

    /// <summary>Délai en minutes (ex: 90 = 1h30)</summary>
    [Required, Range(1, int.MaxValue)]
    public int DelaiMaxJours { get; set; } = 60;

    public bool EstDerniereEtape { get; set; } = false;
}
