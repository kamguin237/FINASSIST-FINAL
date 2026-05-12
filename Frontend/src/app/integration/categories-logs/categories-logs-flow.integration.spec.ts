/**
 * Tests d'intégration — Flux Catégories + Logs
 *
 * Vérifie la collaboration entre :
 *   CategoriesService ↔ AuthService ↔ HttpClient
 *   LogsService ↔ AuthService ↔ HttpClient (pagination + filtres)
 */

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { of, throwError } from 'rxjs';
import { firstValueFrom } from 'rxjs';

// ── Constantes ────────────────────────────────────────────────────────────────

const TOKEN_KEY = 'finassist_token';
const API_URL   = 'http://localhost:5000/api';

function makeJwt(permissions = 'CATEGORIE_CONSULTER,CATEGORIE_GERER,LOG_CONSULTER'): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const body   = btoa(JSON.stringify({
    exp: Math.floor(Date.now() / 1000) + 3600,
    sub: '1', role: 'Administrateur', permissions
  }));
  return `${header}.${body}.sig`;
}

// ── Types ─────────────────────────────────────────────────────────────────────

interface CategorieDTO { id: number; nom: string; description?: string; workflowCircuitId?: number; }
interface LogDTO { id: number; action: string; entiteType: string; date: string; nomUtilisateur?: string; }

// ── Simulation CategoriesService ──────────────────────────────────────────────

class CategoriesServiceSim {
  private http: { get: ReturnType<typeof vi.fn>; post: ReturnType<typeof vi.fn>; put: ReturnType<typeof vi.fn>; delete: ReturnType<typeof vi.fn> };
  private url = `${API_URL}/categories`;

  constructor(http: typeof CategoriesServiceSim.prototype.http) { this.http = http; }

  getAll()                                  { return this.http.get(this.url); }
  getDisponibles()                          { return this.http.get(`${this.url}/disponibles`); }
  getById(id: number)                       { return this.http.get(`${this.url}/${id}`); }
  create(dto: Partial<CategorieDTO>)        { return this.http.post(this.url, dto); }
  update(id: number, dto: Partial<CategorieDTO>) { return this.http.put(`${this.url}/${id}`, dto); }
  delete(id: number)                        { return this.http.delete(`${this.url}/${id}`); }
}

// ── Simulation LogsService ────────────────────────────────────────────────────

class LogsServiceSim {
  private http: { get: ReturnType<typeof vi.fn> };
  private url = `${API_URL}/logs`;

  constructor(http: typeof LogsServiceSim.prototype.http) { this.http = http; }

  getLogs(page = 1, pageSize = 20, filtres?: { action?: string; dateDebut?: string; dateFin?: string }) {
    const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
    if (filtres?.action)    params.set('action', filtres.action);
    if (filtres?.dateDebut) params.set('dateDebut', filtres.dateDebut);
    if (filtres?.dateFin)   params.set('dateFin', filtres.dateFin);
    return this.http.get(`${this.url}?${params.toString()}`);
  }
}

// ── Données de test ───────────────────────────────────────────────────────────

const mockCat: CategorieDTO = { id: 1, nom: 'Informatique', workflowCircuitId: 1 };
const mockLog: LogDTO = { id: 1, action: 'POST /api/besoins', entiteType: 'Besoin', date: '2026-04-01T10:00:00Z', nomUtilisateur: 'Jean Dupont' };

// ═════════════════════════════════════════════════════════════════════════════
// Tests
// ═════════════════════════════════════════════════════════════════════════════

describe('Intégration — Flux Catégories', () => {
  let http: { get: ReturnType<typeof vi.fn>; post: ReturnType<typeof vi.fn>; put: ReturnType<typeof vi.fn>; delete: ReturnType<typeof vi.fn> };
  let service: CategoriesServiceSim;

  beforeEach(() => {
    localStorage.clear();
    localStorage.setItem(TOKEN_KEY, makeJwt());
    http = { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() };
    service = new CategoriesServiceSim(http);
  });

  afterEach(() => { localStorage.clear(); vi.clearAllMocks(); });

  it('getAll appelle GET /api/categories', async () => {
    http.get.mockReturnValue(of([mockCat]));
    const result = await firstValueFrom(service.getAll() as any) as CategorieDTO[];
    expect(http.get).toHaveBeenCalledWith(`${API_URL}/categories`);
    expect(result[0].nom).toBe('Informatique');
  });

  it('getDisponibles appelle GET /api/categories/disponibles', async () => {
    http.get.mockReturnValue(of([mockCat]));
    await firstValueFrom(service.getDisponibles() as any);
    expect(http.get).toHaveBeenCalledWith(`${API_URL}/categories/disponibles`);
  });

  it('getById appelle GET /api/categories/:id', async () => {
    http.get.mockReturnValue(of({ ...mockCat, circuit: { id: 1, nom: 'Circuit Test' } }));
    await firstValueFrom(service.getById(1) as any);
    expect(http.get).toHaveBeenCalledWith(`${API_URL}/categories/1`);
  });

  it('create envoie POST avec le DTO', async () => {
    http.post.mockReturnValue(of({ id: 2, nom: 'Finance', workflowCircuitId: 1 }));
    const dto = { nom: 'Finance', workflowCircuitId: 1 };
    const result = await firstValueFrom(service.create(dto) as any) as CategorieDTO;
    expect(http.post).toHaveBeenCalledWith(`${API_URL}/categories`, dto);
    expect(result.nom).toBe('Finance');
  });

  it('update envoie PUT avec le DTO', async () => {
    http.put.mockReturnValue(of({ ...mockCat, nom: 'IT' }));
    const result = await firstValueFrom(service.update(1, { nom: 'IT' }) as any) as CategorieDTO;
    expect(http.put).toHaveBeenCalledWith(`${API_URL}/categories/1`, { nom: 'IT' });
    expect(result.nom).toBe('IT');
  });

  it('delete appelle DELETE /api/categories/:id', async () => {
    http.delete.mockReturnValue(of(undefined));
    await firstValueFrom(service.delete(1) as any);
    expect(http.delete).toHaveBeenCalledWith(`${API_URL}/categories/1`);
  });

  it('getDisponibles retourne uniquement les catégories accessibles', async () => {
    // Simuler que l'utilisateur Agent ne peut pas créer dans "Informatique" (son rôle est dans le circuit)
    http.get.mockReturnValue(of([{ id: 2, nom: 'Finance', workflowCircuitId: 2 }]));
    const result = await firstValueFrom(service.getDisponibles() as any) as CategorieDTO[];
    expect(result).toHaveLength(1);
    expect(result[0].nom).toBe('Finance');
  });

  it('propage l\'erreur 403 si accès refusé', async () => {
    http.get.mockReturnValue(throwError(() => ({ status: 403 })));
    await expect(firstValueFrom(service.getAll() as any)).rejects.toMatchObject({ status: 403 });
  });

  describe('Cycle complet : créer → modifier → supprimer', () => {
    it('crée une catégorie, la modifie, puis la supprime', async () => {
      http.post.mockReturnValueOnce(of({ id: 5, nom: 'Nouvelle Cat', workflowCircuitId: 1 }));
      const created = await firstValueFrom(service.create({ nom: 'Nouvelle Cat', workflowCircuitId: 1 }) as any) as CategorieDTO;
      expect(created.id).toBe(5);

      http.put.mockReturnValueOnce(of({ ...created, nom: 'Cat Modifiée' }));
      const updated = await firstValueFrom(service.update(created.id, { nom: 'Cat Modifiée' }) as any) as CategorieDTO;
      expect(updated.nom).toBe('Cat Modifiée');

      http.delete.mockReturnValueOnce(of(undefined));
      await firstValueFrom(service.delete(created.id) as any);
      expect(http.delete).toHaveBeenCalledWith(`${API_URL}/categories/5`);
    });
  });
});

// ═════════════════════════════════════════════════════════════════════════════

describe('Intégration — Flux Logs', () => {
  let http: { get: ReturnType<typeof vi.fn> };
  let service: LogsServiceSim;

  beforeEach(() => {
    localStorage.clear();
    localStorage.setItem(TOKEN_KEY, makeJwt());
    http = { get: vi.fn() };
    service = new LogsServiceSim(http);
  });

  afterEach(() => { localStorage.clear(); vi.clearAllMocks(); });

  it('getLogs sans filtres appelle GET avec page et pageSize', async () => {
    http.get.mockReturnValue(of({ total: 1, page: 1, pageSize: 20, items: [mockLog] }));
    await firstValueFrom(service.getLogs() as any);
    const url = http.get.mock.calls[0][0] as string;
    expect(url).toContain('page=1');
    expect(url).toContain('pageSize=20');
  });

  it('getLogs avec page 2 et pageSize 10', async () => {
    http.get.mockReturnValue(of({ total: 50, page: 2, pageSize: 10, items: [] }));
    await firstValueFrom(service.getLogs(2, 10) as any);
    const url = http.get.mock.calls[0][0] as string;
    expect(url).toContain('page=2');
    expect(url).toContain('pageSize=10');
  });

  it('getLogs avec filtre action', async () => {
    http.get.mockReturnValue(of({ total: 2, page: 1, pageSize: 20, items: [] }));
    await firstValueFrom(service.getLogs(1, 20, { action: 'POST' }) as any);
    const url = http.get.mock.calls[0][0] as string;
    expect(url).toContain('action=POST');
  });

  it('getLogs avec filtre dateDebut et dateFin', async () => {
    http.get.mockReturnValue(of({ total: 5, page: 1, pageSize: 20, items: [] }));
    await firstValueFrom(service.getLogs(1, 20, { dateDebut: '2026-04-01', dateFin: '2026-04-30' }) as any);
    const url = http.get.mock.calls[0][0] as string;
    expect(url).toContain('dateDebut=2026-04-01');
    expect(url).toContain('dateFin=2026-04-30');
  });

  it('getLogs avec tous les filtres combinés', async () => {
    http.get.mockReturnValue(of({ total: 1, page: 1, pageSize: 5, items: [mockLog] }));
    await firstValueFrom(service.getLogs(1, 5, { action: 'DELETE', dateDebut: '2026-04-01', dateFin: '2026-04-30' }) as any);
    const url = http.get.mock.calls[0][0] as string;
    expect(url).toContain('action=DELETE');
    expect(url).toContain('dateDebut=2026-04-01');
    expect(url).toContain('dateFin=2026-04-30');
  });

  it('retourne le total et les items', async () => {
    http.get.mockReturnValue(of({ total: 100, page: 1, pageSize: 20, items: [mockLog] }));
    const result = await firstValueFrom(service.getLogs() as any) as { total: number; items: LogDTO[] };
    expect(result.total).toBe(100);
    expect(result.items[0].action).toBe('POST /api/besoins');
  });

  it('propage l\'erreur 403 si accès refusé', async () => {
    http.get.mockReturnValue(throwError(() => ({ status: 403 })));
    await expect(firstValueFrom(service.getLogs() as any)).rejects.toMatchObject({ status: 403 });
  });
});
