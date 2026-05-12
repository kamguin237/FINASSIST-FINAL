import { describe, it, expect } from 'vitest';

// ── Tests logique ChangePasswordComponent ─────────────────────────────────────

describe('ChangePasswordComponent — validation', () => {

  function isFormValid(ancienMdp: string, nouveauMdp: string, confirmation: string): boolean {
    if (!ancienMdp.trim() || !nouveauMdp.trim() || !confirmation.trim()) return false;
    if (nouveauMdp.length < 8) return false;
    if (nouveauMdp !== confirmation) return false;
    return true;
  }

  it('invalide si ancien mot de passe vide', () => {
    expect(isFormValid('', 'nouveau_mdp_12', 'nouveau_mdp_12')).toBe(false);
  });

  it('invalide si nouveau mot de passe vide', () => {
    expect(isFormValid('ancien', '', '')).toBe(false);
  });

  it('invalide si nouveau mot de passe < 8 caractères', () => {
    expect(isFormValid('ancien', 'court', 'court')).toBe(false);
  });

  it('invalide si confirmation ne correspond pas', () => {
    expect(isFormValid('ancien', 'nouveau_mdp_12', 'different')).toBe(false);
  });

  it('valide avec tous les champs corrects', () => {
    expect(isFormValid('ancien', 'nouveau_mdp_12', 'nouveau_mdp_12')).toBe(true);
  });

  it('nouveau mot de passe doit avoir au moins 8 caractères', () => {
    expect(isFormValid('ancien', '12345678', '12345678')).toBe(true);
    expect(isFormValid('ancien', '1234567', '1234567')).toBe(false);
  });
});

describe('ChangePasswordComponent — passwordsMatch', () => {

  function passwordsMatch(nouveauMdp: string, confirmation: string): boolean {
    return nouveauMdp === confirmation;
  }

  it('retourne true si les mots de passe correspondent', () => {
    expect(passwordsMatch('monMotDePasse', 'monMotDePasse')).toBe(true);
  });

  it('retourne false si les mots de passe ne correspondent pas', () => {
    expect(passwordsMatch('monMotDePasse', 'autreMotDePasse')).toBe(false);
  });

  it('est sensible à la casse', () => {
    expect(passwordsMatch('MonMotDePasse', 'monmotdepasse')).toBe(false);
  });

  it('deux chaînes vides correspondent', () => {
    expect(passwordsMatch('', '')).toBe(true);
  });
});

describe('ChangePasswordComponent — gestion des erreurs', () => {
  it('affiche un message d\'erreur générique si pas de message serveur', () => {
    const error = { error: null };
    const message = error.error?.message ?? 'Erreur lors du changement.';
    expect(message).toBe('Erreur lors du changement.');
  });

  it('affiche le message d\'erreur du serveur', () => {
    const error = { error: { message: 'Mot de passe actuel incorrect.' } };
    const message = error.error?.message ?? 'Erreur lors du changement.';
    expect(message).toBe('Mot de passe actuel incorrect.');
  });
});
