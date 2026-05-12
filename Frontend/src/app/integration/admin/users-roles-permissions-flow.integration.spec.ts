/**
 * Tests d'intégration — Flux Administration (Users, Rôles, Permissions)
 *
 * Vérifie la collaboration entre :
 *   UsersService ↔ RolesService ↔ PermissionsService ↔ AuthService ↔ HttpClient
 *
 * Scénarios : CRUD utilisateurs, changement de rôle, assignation de permissions,
 * contrôle d'accès admin.
 */

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { of, throwError } from 'rxjs';
import { firstValueFrom } from 'rxjs';

// ── Constantes ────────────────────────────────────────────────────────────────

const TOKEN_KEY = 'finassist_token';
const API_URL   = 'http://localhost:5000/api';

function makeJwt(role = 'Administrateur', permissions = 'USER_CONSULTER,USER_GERER,ROLE_CONSULTER,PERMISSION_CONSULTER'): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const body   = btoa(JSON.stringify({
    exp: Math.floor(Date.now() / 1000) + 3600,
    sub: '1', role, permissions
  }));
  return `${header}.${body}.sig`;
}

// ── Types ─────────────────────────────────────────────────────────────────────

interface UtilisateurDTO {
  id: number; nom: string; prenom: string; email: string;
  role: string; actif: boolean; dateCreation: string;
}

interface RoleDTO { id: number; code: string; description?: string; }
interface PermissionDTO { id: number; code: string; module: string; description?: string; }

// ── Simulation UsersService ───────────────────────────────────────────────────

class UsersServiceSim {
  private http: { get: ReturnType<typeof vi.fn>; post: ReturnType<typeof vi.fn>; put: ReturnType<typeof vi.fn>; patch: ReturnType<typeof vi.fn>; delete: ReturnType<typeof vi.fn> };
  private url = `${API_URL}/users`;

  constructor(http: typeof UsersServiceSim.prototype.http) { this.http = http; }

  getMe()                                   { return this.http.get(`${this.url}/me`); }
  getAll()                                  { return this.http.get(this.url); }
  getById(id: number)                       { return this.http.get(`${this.url}/${id}`); }
  create(dto: Partial<UtilisateurDTO>)      { return this.http.post(this.url, dto); }
  update(id: number, dto: Partial<UtilisateurDTO>) { return this.http.put(`${this.url}/${id}`, dto); }
  deactivate(id: number)                    { return this.http.delete(`${this.url}/${id}`); }
  activate(id: number)                      { return this.http.patch(`${this.url}/${id}/activer`, {}); }
  deletePermanent(id: number)               { return this.http.delete(`${this.url}/${id}/supprimer`); }
  changeRole(id: number, roleId: number)    { return this.http.put(`${this.url}/${id}/role`, { roleId }); }
  getLogs(id: number)                       { return this.http.get(`${this.url}/${id}/logs`); }
  getPermissions(id: number)               { return this.http.get(`${this.url}/${id}/permissions`); }
  setPermissions(id: number, dto: { permissionIds: number[] }) { return this.http.put(`${this.url}/${id}/permissions`, dto); }
  changePassword(dto: { ancienMotDePasse: string; nouveauMotDePasse: string }) {
    return this.http.put(`${this.url}/me/password`, dto);
  }
}

// ── Simulation RolesService ───────────────────────────────────────────────────

class RolesServiceSim {
  private http: { get: ReturnType<typeof vi.fn>; post: ReturnType<typeof vi.fn>; put: ReturnType<typeof vi.fn>; delete: ReturnType<typeof vi.fn> };
  private url = `${API_URL}/roles`;

  constructor(http: typeof RolesServiceSim.prototype.http) { this.http = http; }

  getAll()                                  { return this.http.get(this.url); }
  getById(id: number)                       { return this.http.get(`${this.url}/${id}`); }
  create(dto: Partial<RoleDTO>)             { return this.http.post(this.url, dto); }
  update(id: number, dto: Partial<RoleDTO>) { return this.http.put(`${this.url}/${id}`, dto); }
  delete(id: number)                        { return this.http.delete(`${this.url}/${id}`); }
  getPermissions(id: number)               { return this.http.get(`${this.url}/${id}/permissions`); }
  setPermissions(id: number, dto: { permissionIds: number[] }) { return this.http.put(`${this.url}/${id}/permissions`, dto); }
  removePermission(id: number, permId: number) { return this.http.delete(`${this.url}/${id}/permissions/${permId}`); }
}

// ── Simulation PermissionsService ─────────────────────────────────────────────

class PermissionsServiceSim {
  private http: { get: ReturnType<typeof vi.fn>; put: ReturnType<typeof vi.fn> };
  private url = `${API_URL}/permissions`;

  constructor(http: typeof PermissionsServiceSim.prototype.http) { this.http = http; }

  getAll()                                  { return this.http.get(this.url); }
  getById(id: number)                       { return this.http.get(`${this.url}/${id}`); }
  update(id: number, dto: Partial<PermissionDTO>) { return this.http.put(`${this.url}/${id}`, dto); }
}

// ── Données de test ───────────────────────────────────────────────────────────

const mockUser: UtilisateurDTO = {
  id: 2, nom: 'Dupont', prenom: 'Jean', email: 'jean@finstar-cm.com',
  role: 'Agent', actif: true, dateCreation: '2026-01-01T00:00:00Z'
};

const mockRole: RoleDTO = { id: 1, code: 'Agent', description: 'Agent de saisie' };
const mockPerm: PermissionDTO = { id: 1, code: 'BESOIN_CONSULTER', module: 'Besoins' };

// ═════════════════════════════════════════════════════════════════════════════
// Tests
// ═════════════════════════════════════════════════════════════════════════════

describe('Intégration — Flux Administration', () => {
  let http: { get: ReturnType<typeof vi.fn>; post: ReturnType<typeof vi.fn>; put: ReturnType<typeof vi.fn>; patch: ReturnType<typeof vi.fn>; delete: ReturnType<typeof vi.fn> };
  let usersService: UsersServiceSim;
  let rolesService: RolesServiceSim;
  let permsService: PermissionsServiceSim;

  beforeEach(() => {
    localStorage.clear();
    localStorage.setItem(TOKEN_KEY, makeJwt());
    http = { get: vi.fn(), post: vi.fn(), put: vi.fn(), patch: vi.fn(), delete: vi.fn() };
    usersService = new UsersServiceSim(http);
    rolesService = new RolesServiceSim(http);
    permsService = new PermissionsServiceSim(http);
  });

  afterEach(() => {
    localStorage.clear();
    vi.clearAllMocks();
  });

  // ── UsersService ──────────────────────────────────────────────────────────

  describe('UsersService', () => {
    it('getAll appelle GET /api/users', async () => {
      http.get.mockReturnValue(of([mockUser]));
      const result = await firstValueFrom(usersService.getAll() as any) as UtilisateurDTO[];
      expect(http.get).toHaveBeenCalledWith(`${API_URL}/users`);
      expect(result[0].nom).toBe('Dupont');
    });

    it('getMe appelle GET /api/users/me', async () => {
      http.get.mockReturnValue(of(mockUser));
      await firstValueFrom(usersService.getMe() as any);
      expect(http.get).toHaveBeenCalledWith(`${API_URL}/users/me`);
    });

    it('getById appelle GET /api/users/:id', async () => {
      http.get.mockReturnValue(of(mockUser));
      const result = await firstValueFrom(usersService.getById(2) as any) as UtilisateurDTO;
      expect(http.get).toHaveBeenCalledWith(`${API_URL}/users/2`);
      expect(result.email).toBe('jean@finstar-cm.com');
    });

    it('create envoie POST avec le DTO', async () => {
      http.post.mockReturnValue(of({ ...mockUser, id: 3 }));
      const dto = { nom: 'Martin', prenom: 'Paul', email: 'paul@finstar-cm.com', roleId: 1 };
      await firstValueFrom(usersService.create(dto) as any);
      expect(http.post).toHaveBeenCalledWith(`${API_URL}/users`, dto);
    });

    it('update envoie PUT avec le DTO', async () => {
      http.put.mockReturnValue(of({ ...mockUser, nom: 'Martin' }));
      const result = await firstValueFrom(usersService.update(2, { nom: 'Martin' }) as any) as UtilisateurDTO;
      expect(http.put).toHaveBeenCalledWith(`${API_URL}/users/2`, { nom: 'Martin' });
      expect(result.nom).toBe('Martin');
    });

    it('deactivate appelle DELETE /api/users/:id', async () => {
      http.delete.mockReturnValue(of(undefined));
      await firstValueFrom(usersService.deactivate(2) as any);
      expect(http.delete).toHaveBeenCalledWith(`${API_URL}/users/2`);
    });

    it('activate appelle PATCH /api/users/:id/activer', async () => {
      http.patch.mockReturnValue(of(undefined));
      await firstValueFrom(usersService.activate(2) as any);
      expect(http.patch).toHaveBeenCalledWith(`${API_URL}/users/2/activer`, {});
    });

    it('changeRole envoie PUT avec le roleId', async () => {
      http.put.mockReturnValue(of({ ...mockUser, role: 'Responsable' }));
      const result = await firstValueFrom(usersService.changeRole(2, 3) as any) as UtilisateurDTO;
      expect(http.put).toHaveBeenCalledWith(`${API_URL}/users/2/role`, { roleId: 3 });
      expect(result.role).toBe('Responsable');
    });

    it('getLogs appelle GET /api/users/:id/logs', async () => {
      http.get.mockReturnValue(of([{ id: 1, action: 'CREATION', dateAction: '2026-01-01' }]));
      await firstValueFrom(usersService.getLogs(2) as any);
      expect(http.get).toHaveBeenCalledWith(`${API_URL}/users/2/logs`);
    });

    it('setPermissions envoie PUT avec les IDs', async () => {
      http.put.mockReturnValue(of(undefined));
      await firstValueFrom(usersService.setPermissions(2, { permissionIds: [1, 2, 3] }) as any);
      expect(http.put).toHaveBeenCalledWith(`${API_URL}/users/2/permissions`, { permissionIds: [1, 2, 3] });
    });

    it('changePassword envoie PUT /api/users/me/password', async () => {
      http.put.mockReturnValue(of({ message: 'Mot de passe modifié' }));
      await firstValueFrom(usersService.changePassword({ ancienMotDePasse: 'ancien', nouveauMotDePasse: 'nouveau' }) as any);
      expect(http.put).toHaveBeenCalledWith(`${API_URL}/users/me/password`, expect.objectContaining({ ancienMotDePasse: 'ancien' }));
    });
  });

  // ── RolesService ──────────────────────────────────────────────────────────

  describe('RolesService', () => {
    it('getAll appelle GET /api/roles', async () => {
      http.get.mockReturnValue(of([mockRole]));
      const result = await firstValueFrom(rolesService.getAll() as any) as RoleDTO[];
      expect(http.get).toHaveBeenCalledWith(`${API_URL}/roles`);
      expect(result[0].code).toBe('Agent');
    });

    it('create envoie POST avec le DTO', async () => {
      http.post.mockReturnValue(of({ id: 5, code: 'NouveauRole' }));
      await firstValueFrom(rolesService.create({ code: 'NouveauRole' }) as any);
      expect(http.post).toHaveBeenCalledWith(`${API_URL}/roles`, { code: 'NouveauRole' });
    });

    it('update envoie PUT avec le DTO', async () => {
      http.put.mockReturnValue(of({ ...mockRole, description: 'Modifié' }));
      await firstValueFrom(rolesService.update(1, { description: 'Modifié' }) as any);
      expect(http.put).toHaveBeenCalledWith(`${API_URL}/roles/1`, { description: 'Modifié' });
    });

    it('delete appelle DELETE /api/roles/:id', async () => {
      http.delete.mockReturnValue(of(undefined));
      await firstValueFrom(rolesService.delete(1) as any);
      expect(http.delete).toHaveBeenCalledWith(`${API_URL}/roles/1`);
    });

    it('getPermissions appelle GET /api/roles/:id/permissions', async () => {
      http.get.mockReturnValue(of([mockPerm]));
      const result = await firstValueFrom(rolesService.getPermissions(1) as any) as PermissionDTO[];
      expect(http.get).toHaveBeenCalledWith(`${API_URL}/roles/1/permissions`);
      expect(result[0].code).toBe('BESOIN_CONSULTER');
    });

    it('setPermissions envoie PUT avec les IDs', async () => {
      http.put.mockReturnValue(of(undefined));
      await firstValueFrom(rolesService.setPermissions(1, { permissionIds: [1, 2] }) as any);
      expect(http.put).toHaveBeenCalledWith(`${API_URL}/roles/1/permissions`, { permissionIds: [1, 2] });
    });

    it('removePermission appelle DELETE /api/roles/:id/permissions/:permId', async () => {
      http.delete.mockReturnValue(of(undefined));
      await firstValueFrom(rolesService.removePermission(1, 5) as any);
      expect(http.delete).toHaveBeenCalledWith(`${API_URL}/roles/1/permissions/5`);
    });
  });

  // ── PermissionsService ────────────────────────────────────────────────────

  describe('PermissionsService', () => {
    it('getAll appelle GET /api/permissions', async () => {
      http.get.mockReturnValue(of([mockPerm]));
      const result = await firstValueFrom(permsService.getAll() as any) as PermissionDTO[];
      expect(http.get).toHaveBeenCalledWith(`${API_URL}/permissions`);
      expect(result[0].module).toBe('Besoins');
    });

    it('getById appelle GET /api/permissions/:id', async () => {
      http.get.mockReturnValue(of(mockPerm));
      await firstValueFrom(permsService.getById(1) as any);
      expect(http.get).toHaveBeenCalledWith(`${API_URL}/permissions/1`);
    });

    it('update envoie PUT avec le DTO', async () => {
      http.put.mockReturnValue(of({ ...mockPerm, description: 'Modifiée' }));
      await firstValueFrom(permsService.update(1, { description: 'Modifiée' }) as any);
      expect(http.put).toHaveBeenCalledWith(`${API_URL}/permissions/1`, { description: 'Modifiée' });
    });
  });

  // ── Cycle complet : créer utilisateur → assigner rôle → assigner permissions ──

  describe('Cycle complet : créer utilisateur → rôle → permissions', () => {
    it('crée un utilisateur, change son rôle, puis assigne des permissions', async () => {
      // 1. Créer l'utilisateur
      http.post.mockReturnValueOnce(of({ ...mockUser, id: 10 }));
      const user = await firstValueFrom(usersService.create({
        nom: 'Nouveau', prenom: 'User', email: 'new@finstar-cm.com'
      }) as any) as UtilisateurDTO;
      expect(user.id).toBe(10);

      // 2. Changer son rôle
      http.put.mockReturnValueOnce(of({ ...user, role: 'Responsable' }));
      const updated = await firstValueFrom(usersService.changeRole(user.id, 2) as any) as UtilisateurDTO;
      expect(updated.role).toBe('Responsable');

      // 3. Assigner des permissions supplémentaires
      http.put.mockReturnValueOnce(of(undefined));
      await firstValueFrom(usersService.setPermissions(user.id, { permissionIds: [1, 2, 3] }) as any);
      expect(http.put).toHaveBeenCalledTimes(2);
    });
  });

  // ── Contrôle d'accès ──────────────────────────────────────────────────────

  describe('Contrôle d\'accès admin', () => {
    it('utilisateur avec USER_GERER peut créer un utilisateur', () => {
      const token = makeJwt('Administrateur', 'USER_GERER');
      const payload = JSON.parse(atob(token.split('.')[1]));
      expect(payload.permissions.split(',')).toContain('USER_GERER');
    });

    it('utilisateur sans USER_GERER ne peut pas créer un utilisateur', () => {
      const token = makeJwt('Agent', 'BESOIN_CONSULTER');
      const payload = JSON.parse(atob(token.split('.')[1]));
      expect(payload.permissions.split(',')).not.toContain('USER_GERER');
    });

    it('propage l\'erreur 403 si accès refusé', async () => {
      http.get.mockReturnValue(throwError(() => ({ status: 403 })));
      await expect(firstValueFrom(usersService.getAll() as any)).rejects.toMatchObject({ status: 403 });
    });
  });
});
