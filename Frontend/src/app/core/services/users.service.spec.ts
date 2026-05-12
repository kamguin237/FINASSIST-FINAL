import { describe, it, expect, beforeEach, vi } from 'vitest';
import { of } from 'rxjs';

const mockHttp = { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn(), patch: vi.fn() };
const BASE_URL = 'http://localhost:4200/api/users';

function createService() {
  return {
    getMe: () => mockHttp.get(`${BASE_URL}/me`),
    changePassword: (dto: any) => mockHttp.put(`${BASE_URL}/me/password`, dto),
    getAll: () => mockHttp.get(BASE_URL),
    getById: (id: number) => mockHttp.get(`${BASE_URL}/${id}`),
    create: (dto: any) => mockHttp.post(BASE_URL, dto),
    update: (id: number, dto: any) => mockHttp.put(`${BASE_URL}/${id}`, dto),
    deactivate: (id: number) => mockHttp.delete(`${BASE_URL}/${id}`),
    activate: (id: number) => mockHttp.patch(`${BASE_URL}/${id}/activer`, {}),
    deletePermanent: (id: number) => mockHttp.delete(`${BASE_URL}/${id}/supprimer`),
    changeRole: (id: number, roleId: number) => mockHttp.put(`${BASE_URL}/${id}/role`, { roleId }),
    getLogs: (id: number) => mockHttp.get(`${BASE_URL}/${id}/logs`),
    getPermissions: (id: number) => mockHttp.get(`${BASE_URL}/${id}/permissions`),
    addPermissions: (id: number, dto: any) => mockHttp.post(`${BASE_URL}/${id}/permissions`, dto),
    setPermissions: (id: number, dto: any) => mockHttp.put(`${BASE_URL}/${id}/permissions`, dto),
    removePermission: (id: number, permId: number) => mockHttp.delete(`${BASE_URL}/${id}/permissions/${permId}`)
  };
}

describe('UsersService — appels HTTP', () => {
  let service: ReturnType<typeof createService>;

  beforeEach(() => { vi.clearAllMocks(); service = createService(); });

  it('getMe appelle GET /users/me', () => {
    mockHttp.get.mockReturnValue(of({}));
    service.getMe();
    expect(mockHttp.get).toHaveBeenCalledWith(`${BASE_URL}/me`);
  });

  it('changePassword appelle PUT /users/me/password', () => {
    const dto = { ancienMotDePasse: 'ancien', nouveauMotDePasse: 'nouveau' };
    mockHttp.put.mockReturnValue(of({ message: 'OK' }));
    service.changePassword(dto);
    expect(mockHttp.put).toHaveBeenCalledWith(`${BASE_URL}/me/password`, dto);
  });

  it('getAll appelle GET /users', () => {
    mockHttp.get.mockReturnValue(of([]));
    service.getAll();
    expect(mockHttp.get).toHaveBeenCalledWith(BASE_URL);
  });

  it('getById appelle GET /users/:id', () => {
    mockHttp.get.mockReturnValue(of({}));
    service.getById(1);
    expect(mockHttp.get).toHaveBeenCalledWith(`${BASE_URL}/1`);
  });

  it('create appelle POST /users', () => {
    const dto = { nom: 'Dupont', prenom: 'Jean', email: 'jean@finstar-cm.com', roleId: 1 };
    mockHttp.post.mockReturnValue(of({ id: 1 }));
    service.create(dto);
    expect(mockHttp.post).toHaveBeenCalledWith(BASE_URL, dto);
  });

  it('update appelle PUT /users/:id', () => {
    const dto = { nom: 'Martin' };
    mockHttp.put.mockReturnValue(of({}));
    service.update(1, dto);
    expect(mockHttp.put).toHaveBeenCalledWith(`${BASE_URL}/1`, dto);
  });

  it('deactivate appelle DELETE /users/:id', () => {
    mockHttp.delete.mockReturnValue(of(undefined));
    service.deactivate(1);
    expect(mockHttp.delete).toHaveBeenCalledWith(`${BASE_URL}/1`);
  });

  it('activate appelle PATCH /users/:id/activer', () => {
    mockHttp.patch.mockReturnValue(of(undefined));
    service.activate(1);
    expect(mockHttp.patch).toHaveBeenCalledWith(`${BASE_URL}/1/activer`, {});
  });

  it('deletePermanent appelle DELETE /users/:id/supprimer', () => {
    mockHttp.delete.mockReturnValue(of(undefined));
    service.deletePermanent(1);
    expect(mockHttp.delete).toHaveBeenCalledWith(`${BASE_URL}/1/supprimer`);
  });

  it('changeRole appelle PUT /users/:id/role avec le roleId', () => {
    mockHttp.put.mockReturnValue(of({}));
    service.changeRole(1, 3);
    expect(mockHttp.put).toHaveBeenCalledWith(`${BASE_URL}/1/role`, { roleId: 3 });
  });

  it('getLogs appelle GET /users/:id/logs', () => {
    mockHttp.get.mockReturnValue(of([]));
    service.getLogs(1);
    expect(mockHttp.get).toHaveBeenCalledWith(`${BASE_URL}/1/logs`);
  });

  it('getPermissions appelle GET /users/:id/permissions', () => {
    mockHttp.get.mockReturnValue(of({}));
    service.getPermissions(1);
    expect(mockHttp.get).toHaveBeenCalledWith(`${BASE_URL}/1/permissions`);
  });

  it('removePermission appelle DELETE /users/:id/permissions/:permId', () => {
    mockHttp.delete.mockReturnValue(of(undefined));
    service.removePermission(1, 5);
    expect(mockHttp.delete).toHaveBeenCalledWith(`${BASE_URL}/1/permissions/5`);
  });
});
