using System.ComponentModel.DataAnnotations;

namespace FinAssist.Core.Entities;

public class EtapeCircuit
{
    public int Id { get; set; }

    /// <summary>Ordre de l'étape dans le circuit (1, 2, 3...)</summary>
    [Required]
    public int Ordre { get; set; }

    /// <summary>Rôle requis pour valider cette étape (ex: "Responsable", "Direction")</summary>
    [Required, MaxLength(50)]
    public string RoleRequis { get; set; } = string.Empty;

    /// <summary>Toujours true — une étape implique toujours une approbation</summary>
    public bool ApprobationRequise { get; set; } = true;

    /// <summary>Si true, une signature est requise après approbation</summary>
    public bool SignatureRequise { get; set; } = false;

    [Required]
    public int DelaiMaxJours { get; set; } = 7;

    /// <summary>Indique si c'est la dernière étape du circuit</summary>
    public bool EstDerniereEtape { get; set; } = false;
}
