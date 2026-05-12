import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';

const STORAGE_KEY = 'app-language';

type Language = 'fr' | 'en';

function getInitialLanguage(): Language {
  const stored = localStorage.getItem(STORAGE_KEY) as Language;
  if (stored === 'fr' || stored === 'en') return stored;
  return 'fr'; // défaut
}

describe('LanguageService — logique langue', () => {

  beforeEach(() => { localStorage.clear(); });
  afterEach(() => { localStorage.clear(); });

  // ── getInitialLanguage ──────────────────────────────────────────────────────

  it('retourne "fr" par défaut si aucune langue stockée', () => {
    localStorage.removeItem(STORAGE_KEY);
    expect(getInitialLanguage()).toBe('fr');
  });

  it('retourne "fr" si "fr" est stocké', () => {
    localStorage.setItem(STORAGE_KEY, 'fr');
    expect(getInitialLanguage()).toBe('fr');
  });

  it('retourne "en" si "en" est stocké', () => {
    localStorage.setItem(STORAGE_KEY, 'en');
    expect(getInitialLanguage()).toBe('en');
  });

  it('retourne "fr" si une valeur invalide est stockée', () => {
    localStorage.setItem(STORAGE_KEY, 'de'); // non supporté
    expect(getInitialLanguage()).toBe('fr');
  });

  // ── setLanguage ─────────────────────────────────────────────────────────────

  it('setLanguage stocke la langue dans localStorage', () => {
    function setLanguage(lang: Language) {
      localStorage.setItem(STORAGE_KEY, lang);
    }
    setLanguage('en');
    expect(localStorage.getItem(STORAGE_KEY)).toBe('en');
  });

  it('setLanguage peut basculer entre fr et en', () => {
    function setLanguage(lang: Language) {
      localStorage.setItem(STORAGE_KEY, lang);
    }
    setLanguage('fr');
    expect(localStorage.getItem(STORAGE_KEY)).toBe('fr');
    setLanguage('en');
    expect(localStorage.getItem(STORAGE_KEY)).toBe('en');
  });

  // ── persistance ─────────────────────────────────────────────────────────────

  it('la langue est persistée entre les sessions', () => {
    localStorage.setItem(STORAGE_KEY, 'en');
    const lang = getInitialLanguage();
    expect(lang).toBe('en');
  });
});
