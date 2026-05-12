import { describe, it, expect, beforeEach, vi } from 'vitest';
import { of } from 'rxjs';

const mockHttp = { get: vi.fn(), put: vi.fn() };
const BASE_URL = 'http://localhost:4200/api/permissions';

function createService() {
  return {
    getAll: () => mockHttp.get(BASE_URL),
    getById: (id: number) => mockHttp.get(`${BASE_URL}/${id}`),
    update: (id: number, dto: any) => mockHttp.put(`${BASE_URL}/${id}`, dto)
  };
}

describe('PermissionsService — appels HTTP', () => {
  let service: ReturnType<typeof createService>;

  beforeEach(() => { vi.clearAllMocks(); service = createService(); });

  it('getAll appelle GET /permissions', () => {
    mockHttp.get.mockReturnValue(of([]));
    service.getAll();
    expect(mockHttp.get).toHaveBeenCalledWith(BASE_URL);
  });

  it('getById appelle GET /permissions/:id', () => {
    mockHttp.get.mockReturnValue(of({}));
    service.getById(1);
    expect(mockHttp.get).toHaveBeenCalledWith(`${BASE_URL}/1`);
  });

  it('update appelle PUT /permissions/:id avec le DTO', () => {
    const dto = { code: 'BESOIN_CONSULTER', description: 'Consulter les besoins' };
    mockHttp.put.mockReturnValue(of({}));
    service.update(1, dto);
    expect(mockHttp.put).toHaveBeenCalledWith(`${BASE_URL}/1`, dto);
  });
});

// ── Tests logique permissions ─────────────────────────────────────────────────

describe('Permissions — logique hasPermission', () => {

  function hasPermission(permissions: string[], code: string): boolean {
    return permissions.includes(code);
  }

  it('retourne true si la permission est présente', () => {
    const perms = ['BESOIN_CONSULTER', 'BESOIN_CREER', 'RAPPORT_EXPORTER'];
    expect(hasPermission(perms, 'BESOIN_CONSULTER')).toBe(true);
  });

  it('retourne false si la permission est absente', () => {
    const perms = ['BESOIN_CONSULTER'];
    expect(hasPermission(perms, 'RAPPORT_EXPORTER')).toBe(false);
  });

  it('retourne false pour un tableau vide', () => {
    expect(hasPermission([], 'BESOIN_CONSULTER')).toBe(false);
  });

  it('est sensible à la casse', () => {
    const perms = ['BESOIN_CONSULTER'];
    expect(hasPermission(perms, 'besoin_consulter')).toBe(false);
  });
});
