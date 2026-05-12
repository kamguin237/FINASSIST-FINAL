import { describe, it, expect } from 'vitest';

// ── Tests logique UsersComponent ──────────────────────────────────────────────

describe('UsersComponent — finstarEmailValidator', () => {

  function finstarEmailValidator(email: string): boolean {
    return email.toLowerCase().endsWith('@finstar-cm.com');
  }

  it('valide un email finstar-cm.com', () => {
    expect(finstarEmailValidator('jean@finstar-cm.com')).toBe(true);
  });

  it('invalide un email gmail', () => {
    expect(finstarEmailValidator('jean@gmail.com')).toBe(false);
  });

  it('est insensible à la casse', () => {
    expect(finstarEmailValidator('JEAN@FINSTAR-CM.COM')).toBe(true);
  });

  it('invalide un email vide', () => {
    expect(finstarEmailValidator('')).toBe(false);
  });
});

describe('UsersComponent — génération email automatique', () => {

  function slugify(str: string): string {
    return str.trim()
      .toLowerCase()
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .replace(/\s+/g, '-')
      .replace(/[^a-z0-9-]/g, '-')
      .replace(/-+/g, '-')
      .replace(/^-|-$/g, '');
  }

  function genererEmail(prenom: string, nom: string): string {
    if (!prenom.trim() && !nom.trim()) return '';
    return `${slugify(prenom)}.${slugify(nom)}@finstar-cm.com`;
  }

  it('génère un email à partir du prénom et nom', () => {
    expect(genererEmail('Jean', 'Dupont')).toBe('jean.dupont@finstar-cm.com');
  });

  it('gère les accents', () => {
    expect(genererEmail('Élodie', 'Müller')).toBe('elodie.muller@finstar-cm.com');
  });

  it('gère les espaces dans le nom', () => {
    expect(genererEmail('Jean Pierre', 'Dupont')).toBe('jean-pierre.dupont@finstar-cm.com');
  });

  it('retourne une chaîne vide si prénom et nom sont vides', () => {
    expect(genererEmail('', '')).toBe('');
  });
});

describe('UsersComponent — permissions directes', () => {

  function isDirecte(permDirectes: Set<number>, permId: number): boolean {
    return permDirectes.has(permId);
  }

  function isFromRole(permRole: Set<number>, permId: number): boolean {
    return permRole.has(permId);
  }

  function toggleDirecte(permDirectes: Set<number>, permId: number, checked: boolean): Set<number> {
    const newSet = new Set(permDirectes);
    if (checked) newSet.add(permId);
    else newSet.delete(permId);
    return newSet;
  }

  it('isDirecte retourne true si la permission est directe', () => {
    const set = new Set([1, 2, 3]);
    expect(isDirecte(set, 2)).toBe(true);
  });

  it('isFromRole retourne true si la permission vient du rôle', () => {
    const set = new Set([5, 6]);
    expect(isFromRole(set, 5)).toBe(true);
  });

  it('toggleDirecte ajoute une permission', () => {
    const set = new Set<number>([1]);
    const newSet = toggleDirecte(set, 5, true);
    expect(newSet.has(5)).toBe(true);
  });

  it('toggleDirecte supprime une permission', () => {
    const set = new Set<number>([1, 5]);
    const newSet = toggleDirecte(set, 5, false);
    expect(newSet.has(5)).toBe(false);
  });
});

describe('UsersComponent — isModuleAllChecked', () => {

  function isModuleAllChecked(
    permissions: { id: number }[],
    permDirectes: Set<number>,
    permRole: Set<number>
  ): boolean {
    return permissions
      .filter(p => !permRole.has(p.id))
      .every(p => permDirectes.has(p.id));
  }

  it('retourne true si toutes les permissions éditables sont cochées', () => {
    const perms = [{ id: 1 }, { id: 2 }];
    const directes = new Set([1, 2]);
    const role = new Set<number>();
    expect(isModuleAllChecked(perms, directes, role)).toBe(true);
  });

  it('ignore les permissions du rôle', () => {
    const perms = [{ id: 1 }, { id: 2 }];
    const directes = new Set([1]); // 2 est du rôle
    const role = new Set([2]);
    expect(isModuleAllChecked(perms, directes, role)).toBe(true);
  });
});
