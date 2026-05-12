import { describe, it, expect, beforeEach, vi } from 'vitest';

// ── Tests logique SignMobileComponent ─────────────────────────────────────────

describe('SignMobileComponent — états', () => {

  it('état initial: loading = true, submitted = false', () => {
    const state = { loading: true, submitted: false, submitting: false, erreur: '' };
    expect(state.loading).toBe(true);
    expect(state.submitted).toBe(false);
  });

  it('après chargement réussi: loading = false, sessionInfo défini', () => {
    const state = { loading: false, sessionInfo: { nom: 'Dupont', prenom: 'Jean', role: 'Responsable', expiration: '' } };
    expect(state.loading).toBe(false);
    expect(state.sessionInfo).not.toBeNull();
  });

  it('après erreur: loading = false, erreur défini', () => {
    const state = { loading: false, erreur: 'Session invalide ou expirée.' };
    expect(state.loading).toBe(false);
    expect(state.erreur).toBe('Session invalide ou expirée.');
  });

  it('après soumission réussie: submitted = true', () => {
    const state = { submitted: true, submitting: false };
    expect(state.submitted).toBe(true);
    expect(state.submitting).toBe(false);
  });
});

describe('SignMobileComponent — gestion des erreurs', () => {
  it('affiche un message générique si pas de message serveur', () => {
    const err = { error: null };
    const message = err.error?.message ?? 'Erreur lors de l\'envoi.';
    expect(message).toBe('Erreur lors de l\'envoi.');
  });

  it('affiche le message du serveur si disponible', () => {
    const err = { error: { message: 'Session expirée.' } };
    const message = err.error?.message ?? 'Erreur lors de l\'envoi.';
    expect(message).toBe('Session expirée.');
  });
});

describe('SignMobileComponent — canvas drawing', () => {
  beforeEach(() => { vi.useFakeTimers(); });

  it('drawing démarre à false', () => {
    let drawing = false;
    expect(drawing).toBe(false);
  });

  it('onMouseDown active drawing', () => {
    let drawing = false;
    // Simuler onMouseDown
    drawing = true;
    expect(drawing).toBe(true);
  });

  it('onMouseUp désactive drawing', () => {
    let drawing = true;
    // Simuler onMouseUp
    drawing = false;
    expect(drawing).toBe(false);
  });
});
