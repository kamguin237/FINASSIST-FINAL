import { describe, it, expect, beforeEach, vi } from 'vitest';
import { of } from 'rxjs';

const mockHttp = { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() };
const BASE_URL = 'http://localhost:4200/api/workflow';

function createService() {
  return {
    getCircuits: () => mockHttp.get(`${BASE_URL}/circuits`),
    getCircuit: (id: number) => mockHttp.get(`${BASE_URL}/circuits/${id}`),
    createCircuit: (dto: any) => mockHttp.post(`${BASE_URL}/circuits`, dto),
    updateCircuit: (id: number, dto: any) => mockHttp.put(`${BASE_URL}/circuits/${id}`, dto),
    deleteCircuit: (id: number) => mockHttp.delete(`${BASE_URL}/circuits/${id}`),
    valider: (id: number, dto: any) => mockHttp.post(`${BASE_URL}/${id}/valider`, dto),
    transmettre: (id: number) => mockHttp.post(`${BASE_URL}/${id}/transmettre`, {})
  };
}

describe('WorkflowService — appels HTTP', () => {
  let service: ReturnType<typeof createService>;

  beforeEach(() => { vi.clearAllMocks(); service = createService(); });

  it('getCircuits appelle GET /workflow/circuits', () => {
    mockHttp.get.mockReturnValue(of([]));
    service.getCircuits();
    expect(mockHttp.get).toHaveBeenCalledWith(`${BASE_URL}/circuits`);
  });

  it('getCircuit appelle GET /workflow/circuits/:id', () => {
    mockHttp.get.mockReturnValue(of({}));
    service.getCircuit(1);
    expect(mockHttp.get).toHaveBeenCalledWith(`${BASE_URL}/circuits/1`);
  });

  it('createCircuit appelle POST /workflow/circuits', () => {
    const dto = { nom: 'Circuit test', etapes: [] };
    mockHttp.post.mockReturnValue(of({ id: 1 }));
    service.createCircuit(dto);
    expect(mockHttp.post).toHaveBeenCalledWith(`${BASE_URL}/circuits`, dto);
  });

  it('updateCircuit appelle PUT /workflow/circuits/:id', () => {
    const dto = { nom: 'Circuit modifié' };
    mockHttp.put.mockReturnValue(of({}));
    service.updateCircuit(1, dto);
    expect(mockHttp.put).toHaveBeenCalledWith(`${BASE_URL}/circuits/1`, dto);
  });

  it('deleteCircuit appelle DELETE /workflow/circuits/:id', () => {
    mockHttp.delete.mockReturnValue(of(undefined));
    service.deleteCircuit(1);
    expect(mockHttp.delete).toHaveBeenCalledWith(`${BASE_URL}/circuits/1`);
  });

  it('valider appelle POST /workflow/:id/valider avec le DTO', () => {
    const dto = { decision: 'APPROUVE', motif: '' };
    mockHttp.post.mockReturnValue(of({}));
    service.valider(1, dto);
    expect(mockHttp.post).toHaveBeenCalledWith(`${BASE_URL}/1/valider`, dto);
  });

  it('transmettre appelle POST /workflow/:id/transmettre', () => {
    mockHttp.post.mockReturnValue(of({}));
    service.transmettre(1);
    expect(mockHttp.post).toHaveBeenCalledWith(`${BASE_URL}/1/transmettre`, {});
  });
});
