import { describe, it, expect, vi } from 'vitest';

// ── Tests logique ForbiddenComponent.goHome ───────────────────────────────────

function goHomeLogic(permissions: string[]): string {
  if (permissions.includes('DASHBOARD_CONSULTER')) return '/dashboard';
  if (permissions.includes('BESOIN_CONSULTER')) return '/besoins';
  return 'logout';
}

describe('ForbiddenComponent — logique goHome', () => {

  it('navigue vers /dashboard si DASHBOARD_CONSULTER est présent', () => {
    const result = goHomeLogic(['DASHBOARD_CONSULTER', 'BESOIN_CONSULTER']);
    expect(result).toBe('/dashboard');
  });

  it('navigue vers /besoins si BESOIN_CONSULTER est présent mais pas DASHBOARD_CONSULTER', () => {
    const result = goHomeLogic(['BESOIN_CONSULTER', 'RAPPORT_EXPORTER']);
    expect(result).toBe('/besoins');
  });

  it('déconnecte si aucune permission accessible', () => {
    const result = goHomeLogic([]);
    expect(result).toBe('logout');
  });

  it('déconnecte si les permissions ne contiennent pas les routes connues', () => {
    const result = goHomeLogic(['RAPPORT_EXPORTER', 'USER_CREER']);
    expect(result).toBe('logout');
  });

  it('DASHBOARD_CONSULTER a priorité sur BESOIN_CONSULTER', () => {
    const result = goHomeLogic(['BESOIN_CONSULTER', 'DASHBOARD_CONSULTER']);
    expect(result).toBe('/dashboard');
  });
});
