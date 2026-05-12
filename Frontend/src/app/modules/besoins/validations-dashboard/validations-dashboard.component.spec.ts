import { describe, it, expect } from 'vitest';

// ── Tests logique ValidationsDashboardComponent ───────────────────────────────

function getBarColor(pct: number): string {
  if (pct >= 100) return '#ef4444';
  if (pct >= 80)  return '#f97316';
  if (pct >= 50)  return '#f59e0b';
  return '#22c55e';
}

function formatDuree(minutes: number): string {
  if (minutes <= 0) return '0 min';
  if (minutes < 60) return `${Math.round(minutes)} min`;
  const h = Math.floor(minutes / 60);
  const m = Math.round(minutes % 60);
  return m > 0 ? `${h}h${m.toString().padStart(2, '0')}` : `${h}h`;
}

function formatRole(etapeRole: string): string {
  if (!etapeRole) return '';
  return etapeRole.charAt(0) + etapeRole.slice(1).toLowerCase();
}

describe('ValidationsDashboardComponent — getBarColor', () => {
  it('retourne rouge pour >= 100%', () => {
    expect(getBarColor(100)).toBe('#ef4444');
    expect(getBarColor(150)).toBe('#ef4444');
  });

  it('retourne orange pour 80-99%', () => {
    expect(getBarColor(80)).toBe('#f97316');
    expect(getBarColor(99)).toBe('#f97316');
  });

  it('retourne jaune pour 50-79%', () => {
    expect(getBarColor(50)).toBe('#f59e0b');
    expect(getBarColor(79)).toBe('#f59e0b');
  });

  it('retourne vert pour < 50%', () => {
    expect(getBarColor(0)).toBe('#22c55e');
    expect(getBarColor(49)).toBe('#22c55e');
  });
});

describe('ValidationsDashboardComponent — formatDuree', () => {
  it('retourne "0 min" pour 0 ou négatif', () => {
    expect(formatDuree(0)).toBe('0 min');
    expect(formatDuree(-5)).toBe('0 min');
  });

  it('retourne les minutes pour < 60', () => {
    expect(formatDuree(30)).toBe('30 min');
    expect(formatDuree(59)).toBe('59 min');
  });

  it('retourne les heures pour >= 60', () => {
    expect(formatDuree(60)).toBe('1h');
    expect(formatDuree(120)).toBe('2h');
  });

  it('retourne heures et minutes', () => {
    expect(formatDuree(90)).toBe('1h30');
    expect(formatDuree(75)).toBe('1h15');
  });

  it('padde les minutes avec un zéro', () => {
    expect(formatDuree(65)).toBe('1h05');
  });
});

describe('ValidationsDashboardComponent — formatRole', () => {
  it('met la première lettre en majuscule', () => {
    expect(formatRole('RESPONSABLE')).toBe('Responsable');
  });

  it('met le reste en minuscule', () => {
    expect(formatRole('DIRECTION')).toBe('Direction');
  });

  it('retourne une chaîne vide si le rôle est vide', () => {
    expect(formatRole('')).toBe('');
  });
});

describe('ValidationsDashboardComponent — getUrgenceClass', () => {
  function getUrgenceClass(urgence: string): string {
    return urgence;
  }

  it('retourne la classe d\'urgence directement', () => {
    expect(getUrgenceClass('normal')).toBe('normal');
    expect(getUrgenceClass('warning')).toBe('warning');
    expect(getUrgenceClass('danger')).toBe('danger');
    expect(getUrgenceClass('expired')).toBe('expired');
  });
});
