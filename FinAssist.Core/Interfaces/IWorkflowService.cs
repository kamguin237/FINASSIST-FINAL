using FinAssist.Core.DTOs.Workflow;

namespace FinAssist.Core.Interfaces;

public interface IWorkflowService
{
    Task<ValidationDTO> ValiderAsync(int besoinId, ValiderBesoinDTO dto, int validateurId, string roleCode);
    Task<ValidationDTO> TransmettreAsync(int besoinId, int validateurId);

    Task<WorkflowCircuitDTO> GetCircuitByIdAsync(int id);
    Task<IEnumerable<WorkflowCircuitDTO>> GetAllCircuitsAsync();
    Task<WorkflowCircuitDTO> CreateCircuitAsync(CreateWorkflowCircuitDTO dto, string nomCreateur);
    Task<WorkflowCircuitDTO> UpdateCircuitAsync(int id, UpdateWorkflowCircuitDTO dto);
    Task DeleteCircuitAsync(int id);
}
