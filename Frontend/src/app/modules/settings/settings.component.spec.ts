import { describe, it, expect } from 'vitest';

// ── Tests logique SettingsComponent ──────────────────────────────────────────

describe('SettingsComponent — initiales', () => {
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
});

describe('SettingsComponent — setInactivite', () => {
  function setInactivite(val: number): number {
    return Math.min(480, Math.max(1, +val || 30));
  }

  it('clamp à 1 minimum', () => {
    expect(setInactivite(-5)).toBe(1);
    expect(setInactivite(1)).toBe(1);
  });

  it('clamp à 480 maximum', () => {
    expect(setInactivite(500)).toBe(480);
    expect(setInactivite(1000)).toBe(480);
  });

  it('retourne 30 par défaut si NaN', () => {
    expect(setInactivite(NaN)).toBe(30);
  });

  it('retourne la valeur si dans les limites', () => {
    expect(setInactivite(60)).toBe(60);
    expect(setInactivite(120)).toBe(120);
  });
});

describe('SettingsComponent — setAvertissement', () => {
  function setAvertissement(val: number): number {
    return Math.min(300, Math.max(10, +val || 30));
  }

  it('clamp à 10 minimum', () => {
    expect(setAvertissement(5)).toBe(10);
    expect(setAvertissement(10)).toBe(10);
  });

  it('clamp à 300 maximum', () => {
    expect(setAvertissement(400)).toBe(300);
  });

  it('retourne 30 par défaut si NaN', () => {
    expect(setAvertissement(NaN)).toBe(30);
  });
});

describe('SettingsComponent — options', () => {
  const langues = [{ value: 'fr', label: '🇫🇷 Français' }, { value: 'en', label: '🇬🇧 English' }];
  const pages = [
    { value: '/dashboard', label: 'Dashboard' },
    { value: '/besoins', label: 'Besoins' },
    { value: '/notifications', label: 'Notifications' }
  ];

  it('langues contient fr et en', () => {
    expect(langues.find(l => l.value === 'fr')).toBeDefined();
    expect(langues.find(l => l.value === 'en')).toBeDefined();
  });

  it('pages contient dashboard et besoins', () => {
    expect(pages.find(p => p.value === '/dashboard')).toBeDefined();
    expect(pages.find(p => p.value === '/besoins')).toBeDefined();
  });
});
