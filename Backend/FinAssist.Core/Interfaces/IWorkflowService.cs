using FinAssist.Core.DTOs.Workflow;

namespace FinAssist.Core.Interfaces;

public interface IWorkflowService
{
    Task<ValidationDTO> ValiderAsync(int besoinId, ValiderBesoinDTO dto, int validateurId, string roleCode, string nomValidateur);
    Task<ValidationDTO> TransmettreAsync(int besoinId, int validateurId, string nomTransmetteur);

    Task<WorkflowCircuitDTO> GetCircuitByIdAsync(int id);
    Task<IEnumerable<WorkflowCircuitDTO>> GetAllCircuitsAsync();
    Task<WorkflowCircuitDTO> CreateCircuitAsync(CreateWorkflowCircuitDTO dto, string nomCreateur);
    Task<WorkflowCircuitDTO> UpdateCircuitAsync(int id, UpdateWorkflowCircuitDTO dto);
    Task DeleteCircuitAsync(int id);
}
