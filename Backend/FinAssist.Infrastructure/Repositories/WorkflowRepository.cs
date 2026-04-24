using FinAssist.Core.Entities;
using FinAssist.Core.Interfaces;
using FinAssist.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinAssist.Infrastructure.Repositories;

public class WorkflowRepository(AppDbContext db) : IWorkflowRepository
{
    // ── Validations ──────────────────────────────────────────────────────────

    public Task<Validation?> GetValidationByIdAsync(int id)
        => db.Validations
             .Include(v => v.Validateur)
             .Include(v => v.Besoin)
             .FirstOrDefaultAsync(v => v.Id == id);

    public Task<IEnumerable<Validation>> GetValidationsByBesoinAsync(int besoinId)
        => Task.FromResult<IEnumerable<Validation>>(
            db.Validations
              .Include(v => v.Validateur)
              .Where(v => v.BesoinId == besoinId)
              .OrderBy(v => v.EtapeOrdre)
              .ThenBy(v => v.DateDecision)
              .AsEnumerable());

    public async Task<Validation> AddValidationAsync(Validation validation)
    {
        db.Validations.Add(validation);
        await db.SaveChangesAsync();
        return validation;
    }

    // ── Circuits dynamiques ──────────────────────────────────────────────────

    public Task<WorkflowCircuit?> GetCircuitByIdAsync(int id)
        => db.WorkflowCircuits
             .Include(c => c.Etapes.OrderBy(e => e.Ordre))
             .FirstOrDefaultAsync(c => c.Id == id);

    public Task<IEnumerable<WorkflowCircuit>> GetAllCircuitsAsync()
        => Task.FromResult<IEnumerable<WorkflowCircuit>>(
            db.WorkflowCircuits
              .Include(c => c.Etapes.OrderBy(e => e.Ordre))
              .AsEnumerable());

    public Task<bool> CircuitNomExistsAsync(string nom, int? excludeId = null)
        => db.WorkflowCircuits.AnyAsync(c =>
            c.Nom == nom &&
            (excludeId == null || c.Id != excludeId));

    public async Task<WorkflowCircuit> CreateCircuitAsync(WorkflowCircuit circuit)
    {
        db.WorkflowCircuits.Add(circuit);
        await db.SaveChangesAsync();
        return circuit;
    }

    public async Task<WorkflowCircuit> UpdateCircuitAsync(WorkflowCircuit circuit)
    {
        db.WorkflowCircuits.Update(circuit);
        await db.SaveChangesAsync();
        return circuit;
    }

    public async Task DeleteCircuitAsync(WorkflowCircuit circuit)
    {
        db.WorkflowCircuits.Remove(circuit);
        await db.SaveChangesAsync();
    }
}
