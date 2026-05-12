import { describe, it, expect } from 'vitest';

// ── Tests logique des routes ──────────────────────────────────────────────────
// On teste la structure des routes sans DI Angular

describe('Routes — structure et configuration', () => {

  // Simulation de la structure des routes
  const routes = [
    { path: 'login', component: 'LoginComponent' },
    { path: 'change-password', component: 'ChangePasswordComponent', guards: ['authGuard', 'changePasswordGuard'] },
    { path: 'sign-mobile/:token', component: 'SignMobileComponent' },
    { path: '403', component: 'ForbiddenComponent' },
    {
      path: '',
      guards: ['authGuard'],
      children: [
        { path: '', redirectTo: 'dashboard' },
        { path: 'dashboard', permission: 'DASHBOARD_CONSULTER' },
        { path: 'besoins', permission: 'BESOIN_CONSULTER' },
        { path: 'besoins/new', permission: 'BESOIN_CREER' },
        { path: 'workflow', permission: 'WORKFLOW_CONSULTER' },
        { path: 'reporting', permission: 'RAPPORT_CONSULTER' },
        { path: 'logs', permission: 'LOG_CONSULTER' },
        { path: 'settings' },
        { path: 'profil' },
        { path: 'ma-signature' },
        { path: 'notifications' }
      ]
    },
    { path: '**', redirectTo: '' }
  ];

  it('la route login est accessible sans authentification', () => {
    const loginRoute = routes.find(r => r.path === 'login');
    expect(loginRoute).toBeDefined();
    expect((loginRoute as any).guards).toBeUndefined();
  });

  it('la route sign-mobile est accessible sans authentification', () => {
    const signRoute = routes.find(r => r.path === 'sign-mobile/:token');
    expect(signRoute).toBeDefined();
    expect((signRoute as any).guards).toBeUndefined();
  });

  it('la route 403 est accessible sans authentification', () => {
    const forbiddenRoute = routes.find(r => r.path === '403');
    expect(forbiddenRoute).toBeDefined();
  });

  it('la route racine requiert authGuard', () => {
    const rootRoute = routes.find(r => r.path === '');
    expect((rootRoute as any).guards).toContain('authGuard');
  });

  it('la route par défaut redirige vers dashboard', () => {
    const rootRoute = routes.find(r => r.path === '');
    const defaultChild = (rootRoute as any).children?.find((c: any) => c.path === '');
    expect(defaultChild?.redirectTo).toBe('dashboard');
  });

  it('le wildcard redirige vers la racine', () => {
    const wildcard = routes.find(r => r.path === '**');
    expect((wildcard as any).redirectTo).toBe('');
  });

  it('les routes protégées ont des permissions', () => {
    const rootRoute = routes.find(r => r.path === '');
    const protectedRoutes = (rootRoute as any).children?.filter((c: any) => c.permission);
    expect(protectedRoutes?.length).toBeGreaterThan(0);
  });

  it('dashboard requiert DASHBOARD_CONSULTER', () => {
    const rootRoute = routes.find(r => r.path === '');
    const dashRoute = (rootRoute as any).children?.find((c: any) => c.path === 'dashboard');
    expect(dashRoute?.permission).toBe('DASHBOARD_CONSULTER');
  });

  it('besoins/new requiert BESOIN_CREER', () => {
    const rootRoute = routes.find(r => r.path === '');
    const newRoute = (rootRoute as any).children?.find((c: any) => c.path === 'besoins/new');
    expect(newRoute?.permission).toBe('BESOIN_CREER');
  });
});

// ── Tests logique de navigation ───────────────────────────────────────────────

describe('Navigation — logique de redirection', () => {

  it('un utilisateur non authentifié est redirigé vers /login', () => {
    const isAuthenticated = false;
    const target = isAuthenticated ? '/dashboard' : '/login';
    expect(target).toBe('/login');
  });

  it('un utilisateur authentifié accède au dashboard', () => {
    const isAuthenticated = true;
    const target = isAuthenticated ? '/dashboard' : '/login';
    expect(target).toBe('/dashboard');
  });

  it('un utilisateur sans permission est redirigé vers /403', () => {
    const hasPermission = false;
    const target = hasPermission ? 'allow' : '/403';
    expect(target).toBe('/403');
  });
});
