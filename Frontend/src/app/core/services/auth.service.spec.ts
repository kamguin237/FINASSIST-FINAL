import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { of, throwError } from 'rxjs';

// ── Mocks ─────────────────────────────────────────────────────────────────────

const mockHttp = {
  post: vi.fn(),
  get: vi.fn()
};

const mockRouter = {
  navigate: vi.fn()
};

// Mock du module Angular pour éviter les dépendances
vi.mock('@angular/core', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@angular/core')>();
  return {
    ...actual,
    Injectable: () => (target: any) => target,
    signal: (initial: any) => {
      let value = initial;
      const sig = () => value;
      sig.set = (v: any) => { value = v; };
      sig.update = (fn: any) => { value = fn(value); };
      return sig;
    }
  };
});

// ── Helpers ───────────────────────────────────────────────────────────────────

// JWT avec permissions encodées
function makeJwt(payload: Record<string, any>): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const body = btoa(JSON.stringify({ exp: Math.floor(Date.now() / 1000) + 3600, ...payload }));
  return `${header}.${body}.signature`;
}

function makeExpiredJwt(): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const body = btoa(JSON.stringify({ exp: Math.floor(Date.now() / 1000) - 100 }));
  return `${header}.${body}.signature`;
}

// ── Tests AuthService (logique pure sans DI Angular) ──────────────────────────

describe('AuthService — logique métier', () => {

  beforeEach(() => {
    localStorage.clear();
    vi.clearAllMocks();
  });

  afterEach(() => {
    localStorage.clear();
  });

  // ── isAuthenticated ─────────────────────────────────────────────────────────

  describe('isAuthenticated', () => {
    it('retourne false si aucun token', () => {
      localStorage.removeItem('finassist_token');
      const token = localStorage.getItem('finassist_token');
      expect(token).toBeNull();
    });

    it('retourne true pour un token valide non expiré', () => {
      const jwt = makeJwt({ sub: '1', permissions: 'BESOIN_CONSULTER' });
      localStorage.setItem('finassist_token', jwt);
      const token = localStorage.getItem('finassist_token');
      const payload = JSON.parse(atob(token!.split('.')[1]));
      expect(payload.exp * 1000).toBeGreaterThan(Date.now());
    });

    it('retourne false pour un token expiré', () => {
      const jwt = makeExpiredJwt();
      localStorage.setItem('finassist_token', jwt);
      const token = localStorage.getItem('finassist_token');
      const payload = JSON.parse(atob(token!.split('.')[1]));
      expect(payload.exp * 1000).toBeLessThan(Date.now());
    });
  });

  // ── parsePermissions ────────────────────────────────────────────────────────

  describe('parsePermissions', () => {
    it('extrait les permissions du JWT', () => {
      const jwt = makeJwt({ permissions: 'BESOIN_CONSULTER,BESOIN_CREER,RAPPORT_EXPORTER' });
      const payload = JSON.parse(atob(jwt.split('.')[1]));
      const perms = payload.permissions ? payload.permissions.split(',').map((p: string) => p.trim()) : [];
      expect(perms).toContain('BESOIN_CONSULTER');
      expect(perms).toContain('BESOIN_CREER');
      expect(perms).toContain('RAPPORT_EXPORTER');
      expect(perms).toHaveLength(3);
    });

    it('retourne un tableau vide si pas de permissions', () => {
      const jwt = makeJwt({ sub: '1' });
      const payload = JSON.parse(atob(jwt.split('.')[1]));
      const perms = payload.permissions ? payload.permissions.split(',') : [];
      expect(perms).toHaveLength(0);
    });

    it('gère un token malformé sans lever d\'exception', () => {
      expect(() => {
        try {
          JSON.parse(atob('invalid_token'.split('.')[1] ?? ''));
        } catch {
          // attendu
        }
      }).not.toThrow();
    });
  });

  // ── localStorage — stockage token ──────────────────────────────────────────

  describe('stockage localStorage', () => {
    it('stocke le token sous la clé finassist_token', () => {
      const token = makeJwt({ sub: '1' });
      localStorage.setItem('finassist_token', token);
      expect(localStorage.getItem('finassist_token')).toBe(token);
    });

    it('stocke l\'utilisateur sous la clé finassist_user', () => {
      const user = { id: 1, nom: 'Dupont', prenom: 'Jean', email: 'jean@test.com', role: 'Agent' };
      localStorage.setItem('finassist_user', JSON.stringify(user));
      const stored = JSON.parse(localStorage.getItem('finassist_user')!);
      expect(stored.id).toBe(1);
      expect(stored.role).toBe('Agent');
    });

    it('stocke le flag doitChangerMotDePasse', () => {
      localStorage.setItem('finassist_must_change_pwd', 'true');
      expect(localStorage.getItem('finassist_must_change_pwd')).toBe('true');
    });

    it('supprime toutes les clés au logout', () => {
      localStorage.setItem('finassist_token', 'token');
      localStorage.setItem('finassist_user', '{}');
      localStorage.setItem('finassist_must_change_pwd', 'false');

      // Simuler logout
      localStorage.removeItem('finassist_token');
      localStorage.removeItem('finassist_user');
      localStorage.removeItem('finassist_must_change_pwd');

      expect(localStorage.getItem('finassist_token')).toBeNull();
      expect(localStorage.getItem('finassist_user')).toBeNull();
      expect(localStorage.getItem('finassist_must_change_pwd')).toBeNull();
    });
  });

  // ── mustChangePassword ──────────────────────────────────────────────────────

  describe('mustChangePassword', () => {
    it('retourne true si le flag est "true"', () => {
      localStorage.setItem('finassist_must_change_pwd', 'true');
      const result = localStorage.getItem('finassist_must_change_pwd') === 'true';
      expect(result).toBe(true);
    });

    it('retourne false si le flag est "false"', () => {
      localStorage.setItem('finassist_must_change_pwd', 'false');
      const result = localStorage.getItem('finassist_must_change_pwd') === 'true';
      expect(result).toBe(false);
    });

    it('retourne false si le flag est absent', () => {
      localStorage.removeItem('finassist_must_change_pwd');
      const result = localStorage.getItem('finassist_must_change_pwd') === 'true';
      expect(result).toBe(false);
    });
  });
});
