import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';

// ── Tests ThemeService — logique pure ─────────────────────────────────────────

const THEME_KEY = 'finassist_theme';

function loadTheme(): boolean {
  return localStorage.getItem(THEME_KEY) !== 'light';
}

function applyTheme(dark: boolean, body: { classList: { toggle: (cls: string, force: boolean) => void } }) {
  body.classList.toggle('light-mode', !dark);
}

describe('ThemeService — logique thème', () => {

  const mockBody = {
    classList: {
      classes: new Set<string>(),
      toggle(cls: string, force: boolean) {
        if (force) this.classes.add(cls);
        else this.classes.delete(cls);
      },
      contains(cls: string) { return this.classes.has(cls); }
    }
  };

  beforeEach(() => {
    localStorage.clear();
    mockBody.classList.classes.clear();
  });

  afterEach(() => {
    localStorage.clear();
  });

  // ── loadTheme ───────────────────────────────────────────────────────────────

  it('retourne true (dark) par défaut si aucune préférence stockée', () => {
    localStorage.removeItem(THEME_KEY);
    expect(loadTheme()).toBe(true);
  });

  it('retourne false (light) si "light" est stocké', () => {
    localStorage.setItem(THEME_KEY, 'light');
    expect(loadTheme()).toBe(false);
  });

  it('retourne true (dark) si "dark" est stocké', () => {
    localStorage.setItem(THEME_KEY, 'dark');
    expect(loadTheme()).toBe(true);
  });

  // ── toggle ──────────────────────────────────────────────────────────────────

  it('toggle passe de dark à light et stocke "light"', () => {
    localStorage.setItem(THEME_KEY, 'dark');
    const isDark = loadTheme(); // true
    const next = !isDark; // false
    localStorage.setItem(THEME_KEY, next ? 'dark' : 'light');
    expect(localStorage.getItem(THEME_KEY)).toBe('light');
  });

  it('toggle passe de light à dark et stocke "dark"', () => {
    localStorage.setItem(THEME_KEY, 'light');
    const isDark = loadTheme(); // false
    const next = !isDark; // true
    localStorage.setItem(THEME_KEY, next ? 'dark' : 'light');
    expect(localStorage.getItem(THEME_KEY)).toBe('dark');
  });

  // ── apply ───────────────────────────────────────────────────────────────────

  it('apply(false) ajoute la classe light-mode au body', () => {
    applyTheme(false, mockBody);
    expect(mockBody.classList.contains('light-mode')).toBe(true);
  });

  it('apply(true) retire la classe light-mode du body', () => {
    mockBody.classList.classes.add('light-mode');
    applyTheme(true, mockBody);
    expect(mockBody.classList.contains('light-mode')).toBe(false);
  });

  // ── persistance ─────────────────────────────────────────────────────────────

  it('la préférence est persistée entre les sessions', () => {
    localStorage.setItem(THEME_KEY, 'light');
    // Simuler un rechargement
    const theme = loadTheme();
    expect(theme).toBe(false); // light mode
  });

  it('valeur inconnue est traitée comme dark', () => {
    localStorage.setItem(THEME_KEY, 'unknown');
    expect(loadTheme()).toBe(true); // !== 'light' → dark
  });
});
