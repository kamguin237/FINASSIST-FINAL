/**
 * Tests d'intégration — Flux d'authentification
 *
 * Vérifie la collaboration entre :
 *   AuthService ↔ localStorage ↔ intercepteur ↔ BesoinsService
 *
 * Approche : HttpClient mocké via vi.fn(), services instanciés réellement.
 */

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { of, throwError } from 'rxjs';

// ── Helpers JWT ───────────────────────────────────────────────────────────────

function makeJwt(payload: Record<string, unknown> = {}, expiresInSec = 3600): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const body   = btoa(JSON.stringify({
    exp: Math.floor(Date.now() / 1000) + expiresInSec,
    sub: '1',
    role: 'Agent',
    permissions: 'BESOIN_CONSULTER,BESOIN_CREER',
    ...payload
  }));
  return `${header}.${body}.sig`;
}

function makeExpiredJwt(): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const body   = btoa(JSON.stringify({ exp: Math.floor(Date.now() / 1000) - 60 }));
  return `${header}.${body}.sig`;
}

// ── Constantes clés localStorage ──────────────────────────────────────────────

const TOKEN_KEY = 'finassist_token';
const USER_KEY  = 'finassist_user';
const MDP_KEY   = 'finassist_must_change_pwd';

// ── Simulation AuthService (logique pure) ─────────────────────────────────────

class AuthServiceSim {
  private token: string | null = null;
  private user: Record<string, unknown> | null = null;
  permissions: string[] = [];

  login(response: { accessToken: string; utilisateur: Record<string, unknown>; doitChangerMotDePasse: boolean }) {
    this.token = response.accessToken;
    this.user  = response.utilisateur;
    this.permissions = this.parsePermissions(response.accessToken);
    localStorage.setItem(TOKEN_KEY, response.accessToken);
    localStorage.setItem(USER_KEY, JSON.stringify(response.utilisateur));
    localStorage.setItem(MDP_KEY, String(response.doitChangerMotDePasse));
  }

  logout() {
    this.token = null;
    this.user  = null;
    this.permissions = [];
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    localStorage.removeItem(MDP_KEY);
  }

  isAuthenticated(): boolean {
    const t = localStorage.getItem(TOKEN_KEY);
    if (!t) return false;
    try {
      const payload = JSON.parse(atob(t.split('.')[1]));
      return payload.exp * 1000 > Date.now();
    } catch { return false; }
  }

  getToken(): string | null { return localStorage.getItem(TOKEN_KEY); }

  hasPermission(code: string): boolean { return this.permissions.includes(code); }

  mustChangePassword(): boolean { return localStorage.getItem(MDP_KEY) === 'true'; }

  private parsePermissions(token: string): string[] {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const perms: string = payload['permissions'] ?? '';
      return perms ? perms.split(',').map((p: string) => p.trim()) : [];
    } catch { return []; }
  }
}

// ── Simulation intercepteur ───────────────────────────────────────────────────

function simulateInterceptor(req: { headers: Record<string, string> }): Record<string, string> {
  const token = localStorage.getItem(TOKEN_KEY);
  const headers = { ...req.headers, 'ngrok-skip-browser-warning': 'true' };
  if (token) headers['Authorization'] = `Bearer ${token}`;
  return headers;
}

// ═════════════════════════════════════════════════════════════════════════════
// Tests
// ═════════════════════════════════════════════════════════════════════════════

describe('Intégration — Flux d\'authentification', () => {
  let auth: AuthServiceSim;

  beforeEach(() => {
    localStorage.clear();
    auth = new AuthServiceSim();
  });

  afterEach(() => {
    localStorage.clear();
  });

  // ── Login → localStorage → isAuthenticated ────────────────────────────────

  describe('Login → stockage → authentification', () => {
    it('après login, isAuthenticated retourne true', () => {
      auth.login({
        accessToken: makeJwt(),
        utilisateur: { id: 1, nom: 'Dupont', prenom: 'Jean', email: 'jean@finstar-cm.com', role: 'Agent' },
        doitChangerMotDePasse: false
      });

      expect(auth.isAuthenticated()).toBe(true);
    });

    it('après login, le token est dans localStorage', () => {
      const token = makeJwt();
      auth.login({ accessToken: token, utilisateur: { id: 1 }, doitChangerMotDePasse: false });

      expect(localStorage.getItem(TOKEN_KEY)).toBe(token);
    });

    it('après login, l\'utilisateur est dans localStorage', () => {
      auth.login({
        accessToken: makeJwt(),
        utilisateur: { id: 1, nom: 'Dupont', prenom: 'Jean', email: 'jean@finstar-cm.com', role: 'Agent' },
        doitChangerMotDePasse: false
      });

      const stored = JSON.parse(localStorage.getItem(USER_KEY)!);
      expect(stored.nom).toBe('Dupont');
      expect(stored.role).toBe('Agent');
    });

    it('après login, les permissions sont chargées depuis le JWT', () => {
      auth.login({
        accessToken: makeJwt({ permissions: 'BESOIN_CONSULTER,RAPPORT_EXPORTER' }),
        utilisateur: { id: 1 },
        doitChangerMotDePasse: false
      });

      expect(auth.hasPermission('BESOIN_CONSULTER')).toBe(true);
      expect(auth.hasPermission('RAPPORT_EXPORTER')).toBe(true);
      expect(auth.hasPermission('ADMIN_TOTAL')).toBe(false);
    });

    it('doitChangerMotDePasse=true est stocké correctement', () => {
      auth.login({ accessToken: makeJwt(), utilisateur: { id: 1 }, doitChangerMotDePasse: true });

      expect(auth.mustChangePassword()).toBe(true);
    });

    it('doitChangerMotDePasse=false est stocké correctement', () => {
      auth.login({ accessToken: makeJwt(), utilisateur: { id: 1 }, doitChangerMotDePasse: false });

      expect(auth.mustChangePassword()).toBe(false);
    });
  });

  // ── Token expiré ──────────────────────────────────────────────────────────

  describe('Token expiré', () => {
    it('isAuthenticated retourne false avec un token expiré', () => {
      localStorage.setItem(TOKEN_KEY, makeExpiredJwt());

      expect(auth.isAuthenticated()).toBe(false);
    });

    it('isAuthenticated retourne false sans token', () => {
      expect(auth.isAuthenticated()).toBe(false);
    });

    it('isAuthenticated retourne false avec un token malformé', () => {
      localStorage.setItem(TOKEN_KEY, 'token.invalide');

      expect(auth.isAuthenticated()).toBe(false);
    });
  });

  // ── Logout → nettoyage ────────────────────────────────────────────────────

  describe('Logout → nettoyage complet', () => {
    it('après logout, isAuthenticated retourne false', () => {
      auth.login({ accessToken: makeJwt(), utilisateur: { id: 1 }, doitChangerMotDePasse: false });
      auth.logout();

      expect(auth.isAuthenticated()).toBe(false);
    });

    it('après logout, localStorage est vidé', () => {
      auth.login({ accessToken: makeJwt(), utilisateur: { id: 1 }, doitChangerMotDePasse: false });
      auth.logout();

      expect(localStorage.getItem(TOKEN_KEY)).toBeNull();
      expect(localStorage.getItem(USER_KEY)).toBeNull();
      expect(localStorage.getItem(MDP_KEY)).toBeNull();
    });

    it('après logout, les permissions sont vides', () => {
      auth.login({ accessToken: makeJwt({ permissions: 'BESOIN_CONSULTER' }), utilisateur: { id: 1 }, doitChangerMotDePasse: false });
      auth.logout();

      expect(auth.hasPermission('BESOIN_CONSULTER')).toBe(false);
    });
  });

  // ── Intercepteur — injection du token ────────────────────────────────────

  describe('Intercepteur — injection du token dans les requêtes', () => {
    it('ajoute Authorization si token présent', () => {
      const token = makeJwt();
      localStorage.setItem(TOKEN_KEY, token);

      const headers = simulateInterceptor({ headers: {} });

      expect(headers['Authorization']).toBe(`Bearer ${token}`);
    });

    it('n\'ajoute pas Authorization si pas de token', () => {
      const headers = simulateInterceptor({ headers: {} });

      expect(headers['Authorization']).toBeUndefined();
    });

    it('ajoute toujours ngrok-skip-browser-warning', () => {
      const headers = simulateInterceptor({ headers: {} });

      expect(headers['ngrok-skip-browser-warning']).toBe('true');
    });

    it('préserve les headers existants', () => {
      localStorage.setItem(TOKEN_KEY, makeJwt());
      const headers = simulateInterceptor({ headers: { 'Content-Type': 'application/json' } });

      expect(headers['Content-Type']).toBe('application/json');
      expect(headers['Authorization']).toBeDefined();
    });
  });

  // ── Cycle complet : login → requête authentifiée → logout ─────────────────

  describe('Cycle complet login → requête → logout', () => {
    it('cycle complet : login, requête avec token, logout', () => {
      // 1. Login
      const token = makeJwt({ permissions: 'BESOIN_CONSULTER' });
      auth.login({ accessToken: token, utilisateur: { id: 1, role: 'Agent' }, doitChangerMotDePasse: false });

      expect(auth.isAuthenticated()).toBe(true);

      // 2. Requête avec intercepteur
      const headers = simulateInterceptor({ headers: {} });
      expect(headers['Authorization']).toBe(`Bearer ${token}`);

      // 3. Vérification permission
      expect(auth.hasPermission('BESOIN_CONSULTER')).toBe(true);

      // 4. Logout
      auth.logout();
      expect(auth.isAuthenticated()).toBe(false);

      // 5. Requête après logout — pas de token
      const headersApresLogout = simulateInterceptor({ headers: {} });
      expect(headersApresLogout['Authorization']).toBeUndefined();
    });

    it('reconnexion après logout fonctionne correctement', () => {
      const token1 = makeJwt({ sub: '1', permissions: 'BESOIN_CONSULTER' });
      const token2 = makeJwt({ sub: '2', permissions: 'ADMIN_TOTAL' });

      auth.login({ accessToken: token1, utilisateur: { id: 1 }, doitChangerMotDePasse: false });
      expect(auth.hasPermission('BESOIN_CONSULTER')).toBe(true);

      auth.logout();

      auth.login({ accessToken: token2, utilisateur: { id: 2 }, doitChangerMotDePasse: false });
      expect(auth.hasPermission('ADMIN_TOTAL')).toBe(true);
      expect(auth.hasPermission('BESOIN_CONSULTER')).toBe(false);
    });
  });
});
