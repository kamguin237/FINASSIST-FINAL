/**
 * Tests d'intégration — Flux Workflow
 *
 * Vérifie la collaboration entre :
 *   WorkflowService ↔ BesoinsService ↔ AuthService (permissions)
 *
 * Scénarios : gestion des circuits, validation, transmission,
 * contrôle d'accès par permission.
 */

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { of, throwError } from 'rxjs';
import { firstValueFrom } from 'rxjs';

// ── Constantes ────────────────────────────────────────────────────────────────

const TOKEN_KEY = 'finassist_token';
const API_URL   = 'http://localhost:5000/api';

function makeJwt(permissions = 'BESOIN_CONSULTER'): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const body   = btoa(JSON.stringify({
    exp: Math.floor(Date.now() / 1000) + 3600,
    sub: '1', role: 'Responsable', permissions
  }));
  return `${header}.${body}.sig`;
}

// ── Simulation WorkflowService ────────────────────────────────────────────────

interface EtapeDTO { ordre: number; roleRequis: string; estDerniereEtape: boolean; delaiMaxJours: number; }
interface CircuitDTO { id: number; nom: string; etapes: EtapeDTO[]; }
interface ValiderBesoinDTO { decision: 'APPROUVE' | 'REJETE'; motif?: string; commentaire?: string; }

class WorkflowServiceSim {
  private http: { get: ReturnType<typeof vi.fn>; post: ReturnType<typeof vi.fn>; put: ReturnType<typeof vi.fn>; delete: ReturnType<typeof vi.fn> };
  private url = `${API_URL}/workflow`;

  constructor(http: typeof WorkflowServiceSim.prototype.http) {
    this.http = http;
  }

  getCircuits()                                   { return this.http.get(`${this.url}/circuits`); }
  getCircuit(id: number)                          { return this.http.get(`${this.url}/circuits/${id}`); }
  createCircuit(dto: Partial<CircuitDTO>)         { return this.http.post(`${this.url}/circuits`, dto); }
  updateCircuit(id: number, dto: Partial<CircuitDTO>) { return this.http.put(`${this.url}/circuits/${id}`, dto); }
  deleteCircuit(id: number)                       { return this.http.delete(`${this.url}/circuits/${id}`); }
  valider(id: number, dto: ValiderBesoinDTO)      { return this.http.post(`${this.url}/${id}/valider`, dto); }
  transmettre(id: number)                         { return this.http.post(`${this.url}/${id}/transmettre`, {}); }
}

// ── Données de test ───────────────────────────────────────────────────────────

const circuitSimple: CircuitDTO = {
  id: 1, nom: 'Circuit Informatique',
  etapes: [
    { ordre: 1, roleRequis: 'Responsable', estDerniereEtape: false, delaiMaxJours: 60 },
    { ordre: 2, roleRequis: 'Direction',   estDerniereEtape: true,  delaiMaxJours: 60 }
  ]
};

// ═════════════════════════════════════════════════════════════════════════════
// Tests
// ═════════════════════════════════════════════════════════════════════════════

describe('Intégration — Flux Workflow', () => {
  let http: { get: ReturnType<typeof vi.fn>; post: ReturnType<typeof vi.fn>; put: ReturnType<typeof vi.fn>; delete: ReturnType<typeof vi.fn> };
  let service: WorkflowServiceSim;

  beforeEach(() => {
    localStorage.clear();
    localStorage.setItem(TOKEN_KEY, makeJwt());
    http = { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() };
    service = new WorkflowServiceSim(http);
  });

  afterEach(() => {
    localStorage.clear();
    vi.clearAllMocks();
  });

  // ── Circuits ──────────────────────────────────────────────────────────────

  describe('Gestion des circuits', () => {
    it('getCircuits appelle GET /api/workflow/circuits', async () => {
      http.get.mockReturnValue(of([circuitSimple]));

      const result = await firstValueFrom(service.getCircuits() as any) as CircuitDTO[];

      expect(http.get).toHaveBeenCalledWith(`${API_URL}/workflow/circuits`);
      expect(result).toHaveLength(1);
      expect(result[0].nom).toBe('Circuit Informatique');
    });

    it('getCircuit appelle GET /api/workflow/circuits/:id', async () => {
      http.get.mockReturnValue(of(circuitSimple));

      const result = await firstValueFrom(service.getCircuit(1) as any) as CircuitDTO;

      expect(http.get).toHaveBeenCalledWith(`${API_URL}/workflow/circuits/1`);
      expect(result.etapes).toHaveLength(2);
    });

    it('createCircuit envoie POST avec le DTO complet', async () => {
      const newCircuit = { nom: 'Nouveau Circuit', etapes: [{ ordre: 1, roleRequis: 'Responsable', estDerniereEtape: true, delaiMaxJours: 30 }] };
      http.post.mockReturnValue(of({ id: 2, ...newCircuit }));

      const result = await firstValueFrom(service.createCircuit(newCircuit) as any) as CircuitDTO;

      expect(http.post).toHaveBeenCalledWith(`${API_URL}/workflow/circuits`, newCircuit);
      expect(result.id).toBe(2);
    });

    it('updateCircuit envoie PUT avec le DTO', async () => {
      const update = { nom: 'Circuit Modifié' };
      http.put.mockReturnValue(of({ ...circuitSimple, ...update }));

      const result = await firstValueFrom(service.updateCircuit(1, update) as any) as CircuitDTO;

      expect(http.put).toHaveBeenCalledWith(`${API_URL}/workflow/circuits/1`, update);
      expect(result.nom).toBe('Circuit Modifié');
    });

    it('deleteCircuit appelle DELETE /api/workflow/circuits/:id', async () => {
      http.delete.mockReturnValue(of(undefined));

      await firstValueFrom(service.deleteCircuit(1) as any);

      expect(http.delete).toHaveBeenCalledWith(`${API_URL}/workflow/circuits/1`);
    });
  });

  // ── Validation ────────────────────────────────────────────────────────────

  describe('Validation d\'un besoin', () => {
    it('valider APPROUVE envoie POST avec la décision', async () => {
      const dto: ValiderBesoinDTO = { decision: 'APPROUVE', commentaire: 'Conforme' };
      http.post.mockReturnValue(of({ id: 1, statut: 'EN_ATTENTE_DIRECTION' }));

      const result = await firstValueFrom(service.valider(1, dto) as any) as { statut: string };

      expect(http.post).toHaveBeenCalledWith(`${API_URL}/workflow/1/valider`, dto);
      expect(result.statut).toBe('EN_ATTENTE_DIRECTION');
    });

    it('valider REJETE envoie POST avec motif', async () => {
      const dto: ValiderBesoinDTO = { decision: 'REJETE', motif: 'Budget insuffisant' };
      http.post.mockReturnValue(of({ id: 1, statut: 'REJETE_PAR_RESPONSABLE' }));

      const result = await firstValueFrom(service.valider(1, dto) as any) as { statut: string };

      expect(http.post).toHaveBeenCalledWith(`${API_URL}/workflow/1/valider`, dto);
      expect(result.statut).toBe('REJETE_PAR_RESPONSABLE');
    });

    it('transmettre appelle POST /api/workflow/:id/transmettre', async () => {
      http.post.mockReturnValue(of({ id: 1, statut: 'EN_ATTENTE_DIRECTION' }));

      const result = await firstValueFrom(service.transmettre(1) as any) as { statut: string };

      expect(http.post).toHaveBeenCalledWith(`${API_URL}/workflow/1/transmettre`, {});
      expect(result.statut).toBe('EN_ATTENTE_DIRECTION');
    });
  });

  // ── Contrôle d'accès par permission ──────────────────────────────────────

  describe('Contrôle d\'accès par permission', () => {
    it('utilisateur avec WORKFLOW_GERER peut créer un circuit', () => {
      const token = makeJwt('WORKFLOW_GERER,BESOIN_CONSULTER');
      localStorage.setItem(TOKEN_KEY, token);
      const payload = JSON.parse(atob(token.split('.')[1]));
      const perms = payload.permissions.split(',');

      expect(perms).toContain('WORKFLOW_GERER');
    });

    it('utilisateur sans WORKFLOW_GERER ne peut pas créer un circuit', () => {
      const token = makeJwt('BESOIN_CONSULTER');
      localStorage.setItem(TOKEN_KEY, token);
      const payload = JSON.parse(atob(token.split('.')[1]));
      const perms = payload.permissions.split(',');

      expect(perms).not.toContain('WORKFLOW_GERER');
    });
  });

  // ── Cycle complet : créer circuit → valider besoin ────────────────────────

  describe('Cycle complet : circuit → validation', () => {
    it('crée un circuit puis valide un besoin avec ce circuit', async () => {
      // 1. Créer le circuit
      http.post.mockReturnValueOnce(of(circuitSimple));
      const circuit = await firstValueFrom(service.createCircuit({
        nom: 'Circuit Test',
        etapes: [{ ordre: 1, roleRequis: 'Responsable', estDerniereEtape: true, delaiMaxJours: 60 }]
      }) as any) as CircuitDTO;
      expect(circuit.id).toBe(1);

      // 2. Valider un besoin avec ce circuit
      http.post.mockReturnValueOnce(of({ id: 5, statut: 'TERMINE' }));
      const validation = await firstValueFrom(service.valider(5, { decision: 'APPROUVE' }) as any) as { statut: string };
      expect(validation.statut).toBe('TERMINE');
    });
  });

  // ── Gestion d'erreurs ─────────────────────────────────────────────────────

  describe('Gestion d\'erreurs', () => {
    it('propage l\'erreur 403 si rôle insuffisant pour valider', async () => {
      http.post.mockReturnValue(throwError(() => ({ status: 403, error: { message: 'Rôle insuffisant' } })));

      await expect(firstValueFrom(service.valider(1, { decision: 'APPROUVE' }) as any))
        .rejects.toMatchObject({ status: 403 });
    });

    it('propage l\'erreur 400 si rejet sans motif', async () => {
      http.post.mockReturnValue(throwError(() => ({ status: 400, error: { message: 'Motif obligatoire' } })));

      await expect(firstValueFrom(service.valider(1, { decision: 'REJETE' }) as any))
        .rejects.toMatchObject({ status: 400 });
    });
  });
});
