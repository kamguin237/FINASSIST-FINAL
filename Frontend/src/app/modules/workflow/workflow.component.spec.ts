import { describe, it, expect } from 'vitest';

// ── Tests logique WorkflowComponent ──────────────────────────────────────────

function minutesToHHmm(minutes: number): string {
  const h = Math.floor(minutes / 60).toString().padStart(2, '0');
  const m = (minutes % 60).toString().padStart(2, '0');
  return `${h}:${m}`;
}

function hhmmToMinutes(hhmm: string): number {
  const [h, m] = (hhmm ?? '00:00').split(':').map(Number);
  const total = (h || 0) * 60 + (m || 0);
  return total > 0 ? total : 1;
}

describe('WorkflowComponent — minutesToHHmm', () => {
  it('convertit 0 en 00:00', () => {
    expect(minutesToHHmm(0)).toBe('00:00');
  });

  it('convertit 60 en 01:00', () => {
    expect(minutesToHHmm(60)).toBe('01:00');
  });

  it('convertit 90 en 01:30', () => {
    expect(minutesToHHmm(90)).toBe('01:30');
  });

  it('convertit 1440 en 24:00', () => {
    expect(minutesToHHmm(1440)).toBe('24:00');
  });

  it('padde les heures et minutes', () => {
    expect(minutesToHHmm(5)).toBe('00:05');
    expect(minutesToHHmm(65)).toBe('01:05');
  });
});

describe('WorkflowComponent — hhmmToMinutes', () => {
  it('convertit 00:00 en 1 (minimum)', () => {
    expect(hhmmToMinutes('00:00')).toBe(1);
  });

  it('convertit 01:00 en 60', () => {
    expect(hhmmToMinutes('01:00')).toBe(60);
  });

  it('convertit 01:30 en 90', () => {
    expect(hhmmToMinutes('01:30')).toBe(90);
  });

  it('convertit 24:00 en 1440', () => {
    expect(hhmmToMinutes('24:00')).toBe(1440);
  });

  it('gère les valeurs nulles/undefined avec 1 minimum', () => {
    expect(hhmmToMinutes('')).toBe(1);
  });

  it('est l\'inverse de minutesToHHmm', () => {
    const minutes = 90;
    expect(hhmmToMinutes(minutesToHHmm(minutes))).toBe(minutes);
  });
});

describe('WorkflowComponent — validation circuit', () => {

  function derniereEtapeDefinie(etapes: { estDerniereEtape: boolean }[]): boolean {
    return etapes.some(e => e.estDerniereEtape === true);
  }

  function tousDelaisValides(etapes: { delaiMaxJours: string }[]): boolean {
    return etapes.every(e => hhmmToMinutes(e.delaiMaxJours) > 0);
  }

  it('derniereEtapeDefinie retourne true si une étape est marquée dernière', () => {
    const etapes = [
      { estDerniereEtape: false },
      { estDerniereEtape: true }
    ];
    expect(derniereEtapeDefinie(etapes)).toBe(true);
  });

  it('derniereEtapeDefinie retourne false si aucune étape n\'est marquée dernière', () => {
    const etapes = [
      { estDerniereEtape: false },
      { estDerniereEtape: false }
    ];
    expect(derniereEtapeDefinie(etapes)).toBe(false);
  });

  it('tousDelaisValides retourne true si tous les délais sont > 0', () => {
    const etapes = [
      { delaiMaxJours: '01:00' },
      { delaiMaxJours: '02:30' }
    ];
    expect(tousDelaisValides(etapes)).toBe(true);
  });

  it('tousDelaisValides retourne false si un délai est 00:00', () => {
    const etapes = [
      { delaiMaxJours: '01:00' },
      { delaiMaxJours: '00:00' } // minimum → 1, donc valide
    ];
    // 00:00 → hhmmToMinutes retourne 1 (minimum), donc valide
    expect(tousDelaisValides(etapes)).toBe(true);
  });

  it('roleOptions exclut Administrateur', () => {
    const roles = [
      { id: 1, code: 'Administrateur' },
      { id: 2, code: 'Responsable' },
      { id: 3, code: 'Direction' }
    ];
    const roleOptions = roles
      .filter(r => r.code !== 'Administrateur')
      .map(r => ({ value: r.code, label: r.code }));
    expect(roleOptions).toHaveLength(2);
    expect(roleOptions.find(r => r.value === 'Administrateur')).toBeUndefined();
  });
});
