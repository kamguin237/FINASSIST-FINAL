import { describe, it, expect, beforeEach, vi } from 'vitest';
import { of } from 'rxjs';

const mockHttp = { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() };
const BASE_URL = 'http://localhost:4200/api/roles';

function createService() {
  return {
    getAll: () => mockHttp.get(BASE_URL),
    getById: (id: number) => mockHttp.get(`${BASE_URL}/${id}`),
    create: (dto: any) => mockHttp.post(BASE_URL, dto),
    update: (id: number, dto: any) => mockHttp.put(`${BASE_URL}/${id}`, dto),
    delete: (id: number) => mockHttp.delete(`${BASE_URL}/${id}`),
    getPermissions: (id: number) => mockHttp.get(`${BASE_URL}/${id}/permissions`),
    addPermissions: (id: number, dto: any) => mockHttp.post(`${BASE_URL}/${id}/permissions`, dto),
    setPermissions: (id: number, dto: any) => mockHttp.put(`${BASE_URL}/${id}/permissions`, dto),
    removePermission: (id: number, permId: number) => mockHttp.delete(`${BASE_URL}/${id}/permissions/${permId}`)
  };
}

describe('RolesService — appels HTTP', () => {
  let service: ReturnType<typeof createService>;

  beforeEach(() => { vi.clearAllMocks(); service = createService(); });

  it('getAll appelle GET /roles', () => {
    mockHttp.get.mockReturnValue(of([]));
    service.getAll();
    expect(mockHttp.get).toHaveBeenCalledWith(BASE_URL);
  });

  it('getById appelle GET /roles/:id', () => {
    mockHttp.get.mockReturnValue(of({}));
    service.getById(1);
    expect(mockHttp.get).toHaveBeenCalledWith(`${BASE_URL}/1`);
  });

  it('create appelle POST /roles', () => {
    const dto = { code: 'NouveauRole', description: 'Desc' };
    mockHttp.post.mockReturnValue(of({ id: 1 }));
    service.create(dto);
    expect(mockHttp.post).toHaveBeenCalledWith(BASE_URL, dto);
  });

  it('update appelle PUT /roles/:id', () => {
    const dto = { code: 'RoleModifié', description: 'Desc' };
    mockHttp.put.mockReturnValue(of({}));
    service.update(1, dto);
    expect(mockHttp.put).toHaveBeenCalledWith(`${BASE_URL}/1`, dto);
  });

  it('delete appelle DELETE /roles/:id', () => {
    mockHttp.delete.mockReturnValue(of(undefined));
    service.delete(1);
    expect(mockHttp.delete).toHaveBeenCalledWith(`${BASE_URL}/1`);
  });

  it('getPermissions appelle GET /roles/:id/permissions', () => {
    mockHttp.get.mockReturnValue(of([]));
    service.getPermissions(1);
    expect(mockHttp.get).toHaveBeenCalledWith(`${BASE_URL}/1/permissions`);
  });

  it('setPermissions appelle PUT /roles/:id/permissions', () => {
    const dto = { permissionIds: [1, 2, 3] };
    mockHttp.put.mockReturnValue(of({}));
    service.setPermissions(1, dto);
    expect(mockHttp.put).toHaveBeenCalledWith(`${BASE_URL}/1/permissions`, dto);
  });

  it('removePermission appelle DELETE /roles/:id/permissions/:permId', () => {
    mockHttp.delete.mockReturnValue(of(undefined));
    service.removePermission(1, 5);
    expect(mockHttp.delete).toHaveBeenCalledWith(`${BASE_URL}/1/permissions/5`);
  });
});
