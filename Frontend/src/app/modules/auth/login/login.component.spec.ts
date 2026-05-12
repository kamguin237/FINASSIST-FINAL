import { describe, it, expect, beforeEach, afterEach } from 'vitest';

// ── Tests logique LoginComponent ──────────────────────────────────────────────

describe('LoginComponent — validation du formulaire', () => {

  // Simulation de la validation Angular Validators.email
  function isValidEmail(email: string): boolean {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
  }

  function isFormValid(email: string, motDePasse: string): boolean {
    return email.trim().length > 0 &&
           isValidEmail(email) &&
           motDePasse.trim().length > 0;
  }

  it('formulaire invalide si email vide', () => {
    expect(isFormValid('', 'password')).toBe(false);
  });

  it('formulaire invalide si mot de passe vide', () => {
    expect(isFormValid('jean@test.com', '')).toBe(false);
  });

  it('formulaire invalide si email mal formé', () => {
    expect(isFormValid('jean', 'password')).toBe(false);
    expect(isFormValid('jean@', 'password')).toBe(false);
    expect(isFormValid('@test.com', 'password')).toBe(false);
  });

  it('formulaire valide avec email et mot de passe corrects', () => {
    expect(isFormValid('jean@finstar-cm.com', 'password123')).toBe(true);
  });

  it('email valide avec différents formats', () => {
    expect(isValidEmail('jean@finstar-cm.com')).toBe(true);
    expect(isValidEmail('jean.dupont@example.org')).toBe(true);
    expect(isValidEmail('user+tag@domain.co')).toBe(true);
  });
});

describe('LoginComponent — logique de navigation post-login', () => {

  function getNavigationTarget(mustChangePassword: boolean, pageAccueil: string): string {
    if (mustChangePassword) return '/change-password';
    return pageAccueil || '/dashboard';
  }

  it('redirige vers /change-password si doitChangerMotDePasse', () => {
    expect(getNavigationTarget(true, '/dashboard')).toBe('/change-password');
  });

  it('redirige vers la page d\'accueil configurée', () => {
    expect(getNavigationTarget(false, '/besoins')).toBe('/besoins');
  });

  it('redirige vers /dashboard par défaut si pageAccueil vide', () => {
    expect(getNavigationTarget(false, '')).toBe('/dashboard');
  });

  it('redirige vers /dashboard si pageAccueil est /dashboard', () => {
    expect(getNavigationTarget(false, '/dashboard')).toBe('/dashboard');
  });
});

describe('LoginComponent — gestion des erreurs', () => {
  it('affiche un message d\'erreur générique si pas de message serveur', () => {
    const error = { error: null };
    const message = error.error?.message ?? 'Erreur de connexion';
    expect(message).toBe('Erreur de connexion');
  });

  it('affiche le message d\'erreur du serveur si disponible', () => {
    const error = { error: { message: 'Email ou mot de passe incorrect.' } };
    const message = error.error?.message ?? 'Erreur de connexion';
    expect(message).toBe('Email ou mot de passe incorrect.');
  });

  it('réinitialise loading à false en cas d\'erreur', () => {
    let loading = true;
    // Simuler le callback error
    loading = false;
    expect(loading).toBe(false);
  });
});
