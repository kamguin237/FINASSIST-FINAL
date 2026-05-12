import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';

// ── Tests authInterceptor (logique pure) ──────────────────────────────────────

describe('authInterceptor — logique des headers', () => {

  beforeEach(() => {
    localStorage.clear();
  });

  afterEach(() => {
    localStorage.clear();
  });

  // Simulation de la logique de l'intercepteur
  function simulateInterceptor(token: string | null): Record<string, string> {
    const headers: Record<string, string> = {
      'ngrok-skip-browser-warning': 'true'
    };
    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
    }
    return headers;
  }

  it('ajoute toujours le header ngrok-skip-browser-warning', () => {
    localStorage.removeItem('finassist_token');
    const token = localStorage.getItem('finassist_token');
    const headers = simulateInterceptor(token);
    expect(headers['ngrok-skip-browser-warning']).toBe('true');
  });

  it('ajoute le header Authorization si un token est présent', () => {
    const fakeToken = 'eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0.sig';
    localStorage.setItem('finassist_token', fakeToken);
    const token = localStorage.getItem('finassist_token');
    const headers = simulateInterceptor(token);
    expect(headers['Authorization']).toBe(`Bearer ${fakeToken}`);
  });

  it('n\'ajoute pas Authorization si aucun token', () => {
    localStorage.removeItem('finassist_token');
    const token = localStorage.getItem('finassist_token');
    const headers = simulateInterceptor(token);
    expect(headers['Authorization']).toBeUndefined();
  });

  it('lit le token depuis la clé finassist_token', () => {
    localStorage.setItem('finassist_token', 'mon_token');
    const token = localStorage.getItem('finassist_token');
    expect(token).toBe('mon_token');
  });

  it('ne lit pas depuis une autre clé', () => {
    localStorage.setItem('token', 'mauvaise_cle');
    localStorage.removeItem('finassist_token');
    const token = localStorage.getItem('finassist_token');
    const headers = simulateInterceptor(token);
    expect(headers['Authorization']).toBeUndefined();
  });
});
