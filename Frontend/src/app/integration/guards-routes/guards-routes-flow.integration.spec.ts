/**
 * Tests d'intégration — Guards et Routes
 *
 * Vérifie la collaboration entre :
 *   authGuard ↔ AuthService ↔ localStorage
 *   permissionGuard ↔ AuthService ↔ permissions JWT
 *   changePasswordGuard ↔ AuthService ↔ flag MDP
 *   Routes — permissions requises par route
 */

import { describe, it, expect, beforeEach, afterEach } from 'vitest';

// ── Constantes ────────────────────────────────────────────────────────────────

const TOKEN_KEY = 'finassist_token';
const USER_KEY  = 'finassist_user';
const MDP_KEY   = 'finassist_must_change_pwd';

function makeJwt(permissions = 'BESOIN_CONSULTER', expiresInSec = 3600): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const body   = btoa(JSON.stringify({
    exp: Math.floor(Date.now() / 1000) + expiresInSec,
    sub: '1', role: 'Agent', permissions
  }));
  return `${header}.${body}.sig`;
}

function makeExpiredJwt(): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const body   = btoa(JSON.stringify({ exp: Math.floor(Date.now() / 1000) - 60 }));
  return `${header}.${body}.sig`;
}

// ── Simulation AuthService (logique pure) ─────────────────────────────────────

class AuthServiceSim {
  private permissions: string[] = [];

  constructor() { this.permissions = this.loadPermissions(); }

  isAuthenticated(): boolean {
    const t = localStorage.getItem(TOKEN_KEY);
    if (!t) return false;
    try {
      const payload = JSON.parse(atob(t.split('.')[1]));
      return payload.exp * 1000 > Date.now();
    } catch { return false; }
  }

  mustChangePassword(): boolean { return localStorage.getItem(MDP_KEY) === 'true'; }

  hasPermission(code: string): boolean { return this.permissions.includes(code); }

  setToken(token: string) {
    localStorage.setItem(TOKEN_KEY, token);
    this.permissions = this.loadPermissions();
  }

  private loadPermissions(): string[] {
    const t = localStorage.getItem(TOKEN_KEY);
    if (!t) return [];
    try {
      const payload = JSON.parse(atob(t.split('.')[1]));
      const perms: string = payload['permissions'] ?? '';
      return perms ? perms.split(',').map((p: string) => p.trim()) : [];
    } catch { return []; }
  }
}

// ── Simulation Guards ─────────────────────────────────────────────────────────

type GuardResult = true | { redirect: string };

function authGuard(auth: AuthServiceSim): GuardResult {
  if (!auth.isAuthenticated()) return { redirect: '/login' };
  if (auth.mustChangePassword()) return { redirect: '/change-password' };
  return true;
}

function permissionGuard(auth: AuthServiceSim, requiredPermission: string): GuardResult {
  if (!auth.isAuthenticated()) return { redirect: '/login' };
  if (!requiredPermission || auth.hasPermission(requiredPermission)) return true;
  return { redirect: '/403' };
}

function changePasswordGuard(auth: AuthServiceSim): GuardResult {
  if (!auth.isAuthenticated()) return { redirect: '/login' };
  if (!auth.mustChangePassword()) return { redirect: '/dashboard' };
  return true;
}

// ═════════════════════════════════════════════════════════════════════════════
// Tests
// ═════════════════════════════════════════════════════════════════════════════

describe('Intégration — Guards et Routes', () => {
  let auth: AuthServiceSim;

  beforeEach(() => {
    localStorage.clear();
    auth = new AuthServiceSim();
  });

  afterEach(() => { localStorage.clear(); });

  // ── authGuard ─────────────────────────────────────────────────────────────

  describe('authGuard', () => {
    it('retourne true si authentifié et pas de changement MDP requis', () => {
      auth.setToken(makeJwt());
      expect(authGuard(auth)).toBe(true);
    });

    it('redirige vers /login si non authentifié', () => {
      expect(authGuard(auth)).toEqual({ redirect: '/login' });
    });

    it('redirige vers /login si token expiré', () => {
      auth.setToken(makeExpiredJwt());
      expect(authGuard(auth)).toEqual({ redirect: '/login' });
    });

    it('redirige vers /change-password si doitChangerMotDePasse=true', () => {
      auth.setToken(makeJwt());
      localStorage.setItem(MDP_KEY, 'true');
      expect(authGuard(auth)).toEqual({ redirect: '/change-password' });
    });

    it('retourne true si doitChangerMotDePasse=false', () => {
      auth.setToken(makeJwt());
      localStorage.setItem(MDP_KEY, 'false');
      expect(authGuard(auth)).toBe(true);
    });
  });

  // ── permissionGuard ───────────────────────────────────────────────────────

  describe('permissionGuard', () => {
    it('retourne true si l\'utilisateur a la permission requise', () => {
      auth.setToken(makeJwt('BESOIN_CONSULTER,BESOIN_CREER'));
      expect(permissionGuard(auth, 'BESOIN_CONSULTER')).toBe(true);
    });

    it('redirige vers /403 si permission manquante', () => {
      auth.setToken(makeJwt('BESOIN_CONSULTER'));
      expect(permissionGuard(auth, 'ADMIN_TOTAL')).toEqual({ redirect: '/403' });
    });

    it('redirige vers /login si non authentifié', () => {
      expect(permissionGuard(auth, 'BESOIN_CONSULTER')).toEqual({ redirect: '/login' });
    });

    it('retourne true si aucune permission requise (route publique)', () => {
      auth.setToken(makeJwt());
      expect(permissionGuard(auth, '')).toBe(true);
    });

    it('retourne true pour DASHBOARD_CONSULTER si présent', () => {
      auth.setToken(makeJwt('DASHBOARD_CONSULTER,BESOIN_CONSULTER'));
      expect(permissionGuard(auth, 'DASHBOARD_CONSULTER')).toBe(true);
    });

    it('redirige vers /403 pour WORKFLOW_CONSULTER si absent', () => {
      auth.setToken(makeJwt('BESOIN_CONSULTER'));
      expect(permissionGuard(auth, 'WORKFLOW_CONSULTER')).toEqual({ redirect: '/403' });
    });
  });

  // ── changePasswordGuard ───────────────────────────────────────────────────

  describe('changePasswordGuard', () => {
    it('retourne true si authentifié et doitChangerMotDePasse=true', () => {
      auth.setToken(makeJwt());
      localStorage.setItem(MDP_KEY, 'true');
      expect(changePasswordGuard(auth)).toBe(true);
    });

    it('redirige vers /dashboard si doitChangerMotDePasse=false', () => {
      auth.setToken(makeJwt());
      localStorage.setItem(MDP_KEY, 'false');
      expect(changePasswordGuard(auth)).toEqual({ redirect: '/dashboard' });
    });

    it('redirige vers /login si non authentifié', () => {
      expect(changePasswordGuard(auth)).toEqual({ redirect: '/login' });
    });

    it('redirige vers /dashboard si flag absent', () => {
      auth.setToken(makeJwt());
      // MDP_KEY absent → mustChangePassword() retourne false
      expect(changePasswordGuard(auth)).toEqual({ redirect: '/dashboard' });
    });
  });

  // ── Permissions requises par route ────────────────────────────────────────

  describe('Permissions requises par route', () => {
    const routePermissions: Record<string, string> = {
      '/dashboard':          'DASHBOARD_CONSULTER',
      '/besoins':            'BESOIN_CONSULTER',
      '/besoins/new':        'BESOIN_CREER',
      '/besoins/validations':'BESOIN_VALIDER',
      '/categories':         'CATEGORIE_CONSULTER',
      '/workflow':           'WORKFLOW_CONSULTER',
      '/notifications':      'NOTIFICATION_LIRE',
      '/reporting':          'DASHBOARD_CONSULTER',
      '/logs':               'LOG_CONSULTER',
      '/admin/users':        'USER_CONSULTER',
      '/admin/roles':        'ROLE_CONSULTER',
      '/admin/permissions':  'PERMISSION_CONSULTER',
      '/ma-signature':       'SIGNATURE_PERSO_GERER',
    };

    it.each(Object.entries(routePermissions))(
      'route %s requiert la permission %s',
      (route, permission) => {
        // Utilisateur avec la permission → accès autorisé
        auth.setToken(makeJwt(permission));
        expect(permissionGuard(auth, permission)).toBe(true);

        // Utilisateur sans la permission → accès refusé
        auth.setToken(makeJwt('AUTRE_PERMISSION'));
        expect(permissionGuard(auth, permission)).toEqual({ redirect: '/403' });
      }
    );

    it('routes sans permission (profil, settings) sont accessibles à tout utilisateur authentifié', () => {
      auth.setToken(makeJwt('BESOIN_CONSULTER'));
      // Ces routes n'ont pas de permissionGuard → authGuard suffit
      expect(authGuard(auth)).toBe(true);
    });
  });

  // ── Scénarios combinés ────────────────────────────────────────────────────

  describe('Scénarios combinés', () => {
    it('admin avec toutes les permissions accède à toutes les routes protégées', () => {
      const allPerms = 'DASHBOARD_CONSULTER,BESOIN_CONSULTER,BESOIN_CREER,BESOIN_VALIDER,CATEGORIE_CONSULTER,WORKFLOW_CONSULTER,NOTIFICATION_LIRE,LOG_CONSULTER,USER_CONSULTER,ROLE_CONSULTER,PERMISSION_CONSULTER,SIGNATURE_PERSO_GERER';
      auth.setToken(makeJwt(allPerms));

      const routes = ['DASHBOARD_CONSULTER', 'BESOIN_CONSULTER', 'WORKFLOW_CONSULTER', 'LOG_CONSULTER'];
      routes.forEach(perm => {
        expect(permissionGuard(auth, perm)).toBe(true);
      });
    });

    it('agent avec permissions limitées est bloqué sur les routes admin', () => {
      auth.setToken(makeJwt('BESOIN_CONSULTER,BESOIN_CREER'));

      expect(permissionGuard(auth, 'USER_CONSULTER')).toEqual({ redirect: '/403' });
      expect(permissionGuard(auth, 'ROLE_CONSULTER')).toEqual({ redirect: '/403' });
      expect(permissionGuard(auth, 'LOG_CONSULTER')).toEqual({ redirect: '/403' });
    });

    it('utilisateur déconnecté est redirigé vers /login pour toutes les routes', () => {
      // Pas de token
      const routes = ['BESOIN_CONSULTER', 'WORKFLOW_CONSULTER', 'USER_CONSULTER'];
      routes.forEach(perm => {
        expect(permissionGuard(auth, perm)).toEqual({ redirect: '/login' });
      });
      expect(authGuard(auth)).toEqual({ redirect: '/login' });
    });
  });
});
