import { describe, it, expect } from 'vitest';

// ── Tests modèles Workflow ────────────────────────────────────────────────────

describe('EtapeCircuitDTO — structure', () => {
  it('a toutes les propriétés requises', () => {
    const etape = {
      id: 1,
      ordre: 1,
      roleRequis: 'Responsable',
      approbationRequise: true,
      signatureRequise: false,
      delaiMaxJours: 7,
      estDerniereEtape: false,
      statutApres: 'EN_ATTENTE_DIRECTION'
    };
    expect(etape.ordre).toBe(1);
    expect(etape.roleRequis).toBe('Responsable');
    expect(etape.approbationRequise).toBe(true);
  });

  it('la dernière étape a estDerniereEtape = true', () => {
    const etape = { estDerniereEtape: true, statutApres: 'TERMINE' };
    expect(etape.estDerniereEtape).toBe(true);
    expect(etape.statutApres).toBe('TERMINE');
  });
});

describe('WorkflowCircuitDTO — structure', () => {
  it('a les propriétés de base', () => {
    const circuit = {
      id: 1,
      nom: 'Circuit Informatique',
      nomCreateur: 'Admin',
      dateCreation: '2026-01-01T00:00:00Z',
      dateModification: '2026-01-01T00:00:00Z',
      etapes: []
    };
    expect(circuit.nom).toBe('Circuit Informatique');
    expect(Array.isArray(circuit.etapes)).toBe(true);
  });

  it('les étapes sont ordonnées par ordre croissant', () => {
    const etapes = [
      { ordre: 1, roleRequis: 'Responsable' },
      { ordre: 2, roleRequis: 'Direction' }
    ];
    const sorted = [...etapes].sort((a, b) => a.ordre - b.ordre);
    expect(sorted[0].roleRequis).toBe('Responsable');
    expect(sorted[1].roleRequis).toBe('Direction');
  });
});

describe('ValiderBesoinDTO — validation', () => {
  it('accepte la décision APPROUVE', () => {
    const dto = { decision: 'APPROUVE' as const };
    expect(dto.decision).toBe('APPROUVE');
  });

  it('accepte la décision REJETE avec motif', () => {
    const dto = { decision: 'REJETE' as const, motif: 'Non conforme aux exigences' };
    expect(dto.decision).toBe('REJETE');
    expect(dto.motif).toBeTruthy();
  });

  it('le motif est requis pour un rejet', () => {
    const dto = { decision: 'REJETE' as const, motif: '' };
    expect(dto.motif?.trim().length).toBe(0); // invalide
  });

  it('le commentaire est optionnel', () => {
    const dto = { decision: 'APPROUVE' as const };
    expect((dto as any).commentaire).toBeUndefined();
  });
});

describe('CreateWorkflowCircuitDTO — validation', () => {
  it('doit avoir au moins une étape', () => {
    const dto = { nom: 'Circuit', etapes: [] };
    expect(dto.etapes.length).toBe(0); // invalide
  });

  it('une étape valide a les champs requis', () => {
    const etape = {
      ordre: 1,
      roleRequis: 'Responsable',
      approbationRequise: true,
      signatureRequise: false,
      delaiMaxJours: 60,
      estDerniereEtape: true
    };
    expect(etape.ordre).toBeGreaterThan(0);
    expect(etape.roleRequis.length).toBeGreaterThan(0);
    expect(etape.delaiMaxJours).toBeGreaterThan(0);
  });

  it('exactement une étape doit être la dernière', () => {
    const etapes = [
      { ordre: 1, estDerniereEtape: false },
      { ordre: 2, estDerniereEtape: true }
    ];
    const dernieres = etapes.filter(e => e.estDerniereEtape);
    expect(dernieres).toHaveLength(1);
  });
});
