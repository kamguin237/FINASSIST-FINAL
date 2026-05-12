import { describe, it, expect, beforeEach, vi } from 'vitest';
import { of } from 'rxjs';

const mockHttp = {
  get: vi.fn(),
  post: vi.fn(),
  put: vi.fn(),
  delete: vi.fn()
};

const BASE_URL = 'http://localhost:4200/api/categories';

function createCategoriesService() {
  return {
    getAll: () => mockHttp.get(BASE_URL),
    getDisponibles: () => mockHttp.get(`${BASE_URL}/disponibles`),
    getById: (id: number) => mockHttp.get(`${BASE_URL}/${id}`),
    create: (dto: any) => mockHttp.post(BASE_URL, dto),
    update: (id: number, dto: any) => mockHttp.put(`${BASE_URL}/${id}`, dto),
    delete: (id: number) => mockHttp.delete(`${BASE_URL}/${id}`)
  };
}

describe('CategoriesService — appels HTTP', () => {

  let service: ReturnType<typeof createCategoriesService>;

  beforeEach(() => {
    vi.clearAllMocks();
    service = createCategoriesService();
  });

  it('getAll appelle GET /categories', () => {
    mockHttp.get.mockReturnValue(of([]));
    service.getAll();
    expect(mockHttp.get).toHaveBeenCalledWith(BASE_URL);
  });

  it('getDisponibles appelle GET /categories/disponibles', () => {
    mockHttp.get.mockReturnValue(of([]));
    service.getDisponibles();
    expect(mockHttp.get).toHaveBeenCalledWith(`${BASE_URL}/disponibles`);
  });

  it('getById appelle GET /categories/:id', () => {
    mockHttp.get.mockReturnValue(of({ id: 1 }));
    service.getById(1);
    expect(mockHttp.get).toHaveBeenCalledWith(`${BASE_URL}/1`);
  });

  it('create appelle POST /categories avec le DTO', () => {
    const dto = { nom: 'Informatique', workflowCircuitId: 1 };
    mockHttp.post.mockReturnValue(of({ id: 1, nom: 'Informatique' }));
    service.create(dto);
    expect(mockHttp.post).toHaveBeenCalledWith(BASE_URL, dto);
  });

  it('update appelle PUT /categories/:id avec le DTO', () => {
    const dto = { nom: 'Informatique modifié' };
    mockHttp.put.mockReturnValue(of({ id: 1 }));
    service.update(1, dto);
    expect(mockHttp.put).toHaveBeenCalledWith(`${BASE_URL}/1`, dto);
  });

  it('delete appelle DELETE /categories/:id', () => {
    mockHttp.delete.mockReturnValue(of(undefined));
    service.delete(1);
    expect(mockHttp.delete).toHaveBeenCalledWith(`${BASE_URL}/1`);
  });
});
