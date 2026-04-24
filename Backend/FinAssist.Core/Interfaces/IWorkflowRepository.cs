using FinAssist.Core.Entities;

namespace FinAssist.Core.Interfaces;

public interface IWorkflowRepository
{
    // Validations
    Task<Validation?> GetValidationByIdAsync(int id);
    Task<IEnumerable<Validation>> GetValidationsByBesoinAsync(int besoinId);
    Task<Validation> AddValidationAsync(Validation validation);

    // Circuits dynamiques
    Task<WorkflowCircuit?> GetCircuitByIdAsync(int id);
    Task<IEnumerable<WorkflowCircuit>> GetAllCircuitsAsync();
    Task<bool> CircuitNomExistsAsync(string nom, int? excludeId = null);
    Task<WorkflowCircuit> CreateCircuitAsync(WorkflowCircuit circuit);
    Task<WorkflowCircuit> UpdateCircuitAsync(WorkflowCircuit circuit);
    Task DeleteCircuitAsync(WorkflowCircuit circuit);
}
