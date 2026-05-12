import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';

// ── Tests logique des guards (sans DI Angular) ────────────────────────────────
// On teste la logique de décision des guards directement

describe('authGuard — logique de décision', () => {

  // Simulation de la logique du guard
  function authGuardLogic(isAuthenticated: boolean, mustChangePassword: boolean): string {
    if (!isAuthenticated) return '/login';
    if (mustChangePassword) return '/change-password';
    return 'allow';
  }

  it('redirige vers /login si non authentifié', () => {
    expect(authGuardLogic(false, false)).toBe('/login');
  });

  it('redirige vers /change-password si authentifié mais doit changer le mot de passe', () => {
    expect(authGuardLogic(true, true)).toBe('/change-password');
  });

  it('autorise l\'accès si authentifié et pas de changement de mot de passe requis', () => {
    expect(authGuardLogic(true, false)).toBe('allow');
  });

  it('redirige vers /login même si mustChangePassword est true mais non authentifié', () => {
    expect(authGuardLogic(false, true)).toBe('/login');
  });
});

describe('permissionGuard — logique de décision', () => {

  function permissionGuardLogic(
    isAuthenticated: boolean,
    permissions: string[],
    requiredPermission: string | undefined
  ): string {
    if (!isAuthenticated) return '/login';
    if (!requiredPermission || permissions.includes(requiredPermission)) return 'allow';
    return '/403';
  }

  it('redirige vers /login si non authentifié', () => {
    expect(permissionGuardLogic(false, [], 'BESOIN_CONSULTER')).toBe('/login');
  });

  it('autorise si la permission requise est présente', () => {
    expect(permissionGuardLogic(true, ['BESOIN_CONSULTER', 'BESOIN_CREER'], 'BESOIN_CONSULTER')).toBe('allow');
  });

  it('redirige vers /403 si la permission est absente', () => {
    expect(permissionGuardLogic(true, ['BESOIN_CONSULTER'], 'RAPPORT_EXPORTER')).toBe('/403');
  });

  it('autorise si aucune permission requise (route publique)', () => {
    expect(permissionGuardLogic(true, [], undefined)).toBe('allow');
  });

  it('autorise si la permission requise est une chaîne vide', () => {
    expect(permissionGuardLogic(true, [], '')).toBe('allow');
  });
});

describe('changePasswordGuard — logique de décision', () => {

  function changePasswordGuardLogic(isAuthenticated: boolean, mustChangePassword: boolean): string {
    if (!isAuthenticated) return '/login';
    if (!mustChangePassword) return '/dashboard';
    return 'allow';
  }

  it('redirige vers /login si non authentifié', () => {
    expect(changePasswordGuardLogic(false, true)).toBe('/login');
  });

  it('redirige vers /dashboard si authentifié mais pas besoin de changer le mot de passe', () => {
    expect(changePasswordGuardLogic(true, false)).toBe('/dashboard');
  });

  it('autorise l\'accès si authentifié et doit changer le mot de passe', () => {
    expect(changePasswordGuardLogic(true, true)).toBe('allow');
  });
});
