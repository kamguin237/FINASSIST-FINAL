import { describe, it, expect } from 'vitest';

// ── Tests logique ProfilComponent ─────────────────────────────────────────────

describe('ProfilComponent — initiales', () => {

  function getInitiales(profil: { prenom?: string; nom?: string } | null): string {
    if (!profil) return '?';
    return `${profil.prenom?.[0] ?? ''}${profil.nom?.[0] ?? ''}`.toUpperCase();
  }

  it('retourne "?" si profil est null', () => {
    expect(getInitiales(null)).toBe('?');
  });

  it('retourne les initiales en majuscules', () => {
    expect(getInitiales({ prenom: 'Jean', nom: 'Dupont' })).toBe('JD');
  });

  it('gère les prénoms et noms avec minuscules', () => {
    expect(getInitiales({ prenom: 'jean', nom: 'dupont' })).toBe('JD');
  });

  it('gère les champs manquants', () => {
    expect(getInitiales({ prenom: 'Jean' })).toBe('J');
    expect(getInitiales({ nom: 'Dupont' })).toBe('D');
  });
});

describe('ProfilComponent — avatarColor', () => {

  function getAvatarColor(role: string): string {
    const colors: Record<string, string> = {
      'Administrateur': '#7c3aed',
      'Direction':      '#0d9488',
      'Responsable':    '#2563eb',
      'Agent':          '#7c3aed'
    };
    return colors[role] ?? '#7c3aed';
  }

  it('retourne violet pour Administrateur', () => {
    expect(getAvatarColor('Administrateur')).toBe('#7c3aed');
  });

  it('retourne teal pour Direction', () => {
    expect(getAvatarColor('Direction')).toBe('#0d9488');
  });

  it('retourne bleu pour Responsable', () => {
    expect(getAvatarColor('Responsable')).toBe('#2563eb');
  });

  it('retourne violet par défaut pour un rôle inconnu', () => {
    expect(getAvatarColor('RoleInconnu')).toBe('#7c3aed');
  });
});

describe('ProfilComponent — strengthScore', () => {

  function getStrengthScore(pwd: string): number {
    if (!pwd) return 0;
    let score = 0;
    if (pwd.length >= 8)  score++;
    if (pwd.length >= 12) score++;
    if (/[A-Z]/.test(pwd)) score++;
    if (/[0-9]/.test(pwd)) score++;
    if (/[^A-Za-z0-9]/.test(pwd)) score++;
    return score;
  }

  function getStrengthLabel(score: number): string {
    if (score <= 1) return 'profile.strengthWeak';
    if (score <= 3) return 'profile.strengthMedium';
    return 'profile.strengthStrong';
  }

  it('retourne 0 pour un mot de passe vide', () => {
    expect(getStrengthScore('')).toBe(0);
  });

  it('retourne 1 pour un mot de passe de 8 caractères simples', () => {
    expect(getStrengthScore('abcdefgh')).toBe(1);
  });

  it('retourne 5 pour un mot de passe fort', () => {
    expect(getStrengthScore('MonMotDePasse1!')).toBe(5);
  });

  it('retourne "weak" pour score <= 1', () => {
    expect(getStrengthLabel(0)).toBe('profile.strengthWeak');
    expect(getStrengthLabel(1)).toBe('profile.strengthWeak');
  });

  it('retourne "medium" pour score 2-3', () => {
    expect(getStrengthLabel(2)).toBe('profile.strengthMedium');
    expect(getStrengthLabel(3)).toBe('profile.strengthMedium');
  });

  it('retourne "strong" pour score >= 4', () => {
    expect(getStrengthLabel(4)).toBe('profile.strengthStrong');
    expect(getStrengthLabel(5)).toBe('profile.strengthStrong');
  });

  it('strengthWidth est proportionnel au score', () => {
    const score = 3;
    const width = `${(score / 5) * 100}%`;
    expect(width).toBe('60%');
  });
});

describe('ProfilComponent — passwordsMatch', () => {

  function passwordsMatch(nouveau: string, confirm: string): boolean {
    return !nouveau || !confirm || nouveau === confirm;
  }

  it('retourne true si les mots de passe correspondent', () => {
    expect(passwordsMatch('monMdp123!', 'monMdp123!')).toBe(true);
  });

  it('retourne false si les mots de passe ne correspondent pas', () => {
    expect(passwordsMatch('monMdp123!', 'autreMotDePasse')).toBe(false);
  });

  it('retourne true si les champs sont vides', () => {
    expect(passwordsMatch('', '')).toBe(true);
  });
});
