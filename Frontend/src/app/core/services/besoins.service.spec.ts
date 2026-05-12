import { describe, it, expect, beforeEach, vi } from 'vitest';
import { of } from 'rxjs';

// ── Mock HttpClient ───────────────────────────────────────────────────────────

const mockHttp = {
  get: vi.fn(),
  post: vi.fn(),
  put: vi.fn(),
  delete: vi.fn()
};

// Simulation du service sans DI Angular
const BASE_URL = 'http://localhost:4200/api/besoins';

function createBesoinsService() {
  return {
    getAll: () => mockHttp.get(BASE_URL),
    getById: (id: number) => mockHttp.get(`${BASE_URL}/${id}`),
    update: (id: number, dto: any) => mockHttp.put(`${BASE_URL}/${id}`, dto),
    delete: (id: number) => mockHttp.delete(`${BASE_URL}/${id}`),
    enregistrer: (id: number) => mockHttp.post(`${BASE_URL}/${id}/enregistrer`, {}),
    soumettre: (id: number) => mockHttp.post(`${BASE_URL}/${id}/soumettre`, {}),
    getHistorique: (id: number) => mockHttp.get(`${BASE_URL}/${id}/historique`),
    getDocuments: (id: number) => mockHttp.get(`${BASE_URL}/${id}/pieces-jointes`),
    getDocumentUrl: (besoinId: number, documentId: number) =>
      `${BASE_URL}/${besoinId}/pieces-jointes/${documentId}`,
    supprimerDocument: (besoinId: number, documentId: number) =>
      mockHttp.delete(`${BASE_URL}/${besoinId}/pieces-jointes/${documentId}`)
  };
}

// ── Tests ─────────────────────────────────────────────────────────────────────

describe('BesoinsService — appels HTTP', () => {

  let service: ReturnType<typeof createBesoinsService>;

  beforeEach(() => {
    vi.clearAllMocks();
    service = createBesoinsService();
  });

  // ── getAll ──────────────────────────────────────────────────────────────────

  it('getAll appelle GET /besoins', () => {
    mockHttp.get.mockReturnValue(of([]));
    service.getAll();
    expect(mockHttp.get).toHaveBeenCalledWith(BASE_URL);
  });

  // ── getById ─────────────────────────────────────────────────────────────────

  it('getById appelle GET /besoins/:id', () => {
    mockHttp.get.mockReturnValue(of({ id: 1 }));
    service.getById(1);
    expect(mockHttp.get).toHaveBeenCalledWith(`${BASE_URL}/1`);
  });

  // ── update ──────────────────────────────────────────────────────────────────

  it('update appelle PUT /besoins/:id avec le DTO', () => {
    const dto = { titre: 'Nouveau titre' };
    mockHttp.put.mockReturnValue(of({ id: 1, titre: 'Nouveau titre' }));
    service.update(1, dto);
    expect(mockHttp.put).toHaveBeenCalledWith(`${BASE_URL}/1`, dto);
  });

  // ── delete ──────────────────────────────────────────────────────────────────

  it('delete appelle DELETE /besoins/:id', () => {
    mockHttp.delete.mockReturnValue(of(undefined));
    service.delete(1);
    expect(mockHttp.delete).toHaveBeenCalledWith(`${BASE_URL}/1`);
  });

  // ── enregistrer ─────────────────────────────────────────────────────────────

  it('enregistrer appelle POST /besoins/:id/enregistrer', () => {
    mockHttp.post.mockReturnValue(of({ id: 1, statut: 'ENREGISTRE' }));
    service.enregistrer(1);
    expect(mockHttp.post).toHaveBeenCalledWith(`${BASE_URL}/1/enregistrer`, {});
  });

  // ── soumettre ───────────────────────────────────────────────────────────────

  it('soumettre appelle POST /besoins/:id/soumettre', () => {
    mockHttp.post.mockReturnValue(of({ id: 1, statut: 'EN_ATTENTE_RESPONSABLE' }));
    service.soumettre(1);
    expect(mockHttp.post).toHaveBeenCalledWith(`${BASE_URL}/1/soumettre`, {});
  });

  // ── getHistorique ───────────────────────────────────────────────────────────

  it('getHistorique appelle GET /besoins/:id/historique', () => {
    mockHttp.get.mockReturnValue(of([]));
    service.getHistorique(1);
    expect(mockHttp.get).toHaveBeenCalledWith(`${BASE_URL}/1/historique`);
  });

  // ── getDocuments ────────────────────────────────────────────────────────────

  it('getDocuments appelle GET /besoins/:id/pieces-jointes', () => {
    mockHttp.get.mockReturnValue(of([]));
    service.getDocuments(1);
    expect(mockHttp.get).toHaveBeenCalledWith(`${BASE_URL}/1/pieces-jointes`);
  });

  // ── getDocumentUrl ──────────────────────────────────────────────────────────

  it('getDocumentUrl retourne l\'URL correcte', () => {
    const url = service.getDocumentUrl(1, 5);
    expect(url).toBe(`${BASE_URL}/1/pieces-jointes/5`);
  });

  // ── supprimerDocument ───────────────────────────────────────────────────────

  it('supprimerDocument appelle DELETE /besoins/:id/pieces-jointes/:docId', () => {
    mockHttp.delete.mockReturnValue(of(undefined));
    service.supprimerDocument(1, 5);
    expect(mockHttp.delete).toHaveBeenCalledWith(`${BASE_URL}/1/pieces-jointes/5`);
  });
});
