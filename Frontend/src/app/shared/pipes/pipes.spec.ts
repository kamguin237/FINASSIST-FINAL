import { describe, it, expect } from 'vitest';

// ── Tests logique des pipes (sans DI Angular) ─────────────────────────────────

// ── NiveauOptionsPipe ─────────────────────────────────────────────────────────

function niveauOptionsPipe(niveaux: readonly string[]) {
  return (niveaux ?? []).map(n => ({ value: n, label: n }));
}

describe('NiveauOptionsPipe', () => {
  it('transforme un tableau de strings en SelectOption[]', () => {
    const result = niveauOptionsPipe(['FAIBLE', 'MOYEN', 'ELEVE', 'CRITIQUE']);
    expect(result).toHaveLength(4);
    expect(result[0]).toEqual({ value: 'FAIBLE', label: 'FAIBLE' });
    expect(result[3]).toEqual({ value: 'CRITIQUE', label: 'CRITIQUE' });
  });

  it('retourne un tableau vide pour un tableau vide', () => {
    expect(niveauOptionsPipe([])).toHaveLength(0);
  });

  it('gère null/undefined sans erreur', () => {
    expect(niveauOptionsPipe(null as any)).toHaveLength(0);
  });

  it('value et label sont identiques', () => {
    const result = niveauOptionsPipe(['MOYEN']);
    expect(result[0].value).toBe(result[0].label);
  });
});

// ── CategorieOptionsPipe ──────────────────────────────────────────────────────

function categorieOptionsPipe(categories: { id: number; nom: string }[]) {
  return (categories ?? []).map(c => ({ value: c.id, label: c.nom }));
}

describe('CategorieOptionsPipe', () => {
  it('transforme les catégories en SelectOption[]', () => {
    const cats = [
      { id: 1, nom: 'Informatique' },
      { id: 2, nom: 'RH' }
    ];
    const result = categorieOptionsPipe(cats);
    expect(result).toHaveLength(2);
    expect(result[0]).toEqual({ value: 1, label: 'Informatique' });
    expect(result[1]).toEqual({ value: 2, label: 'RH' });
  });

  it('retourne un tableau vide pour un tableau vide', () => {
    expect(categorieOptionsPipe([])).toHaveLength(0);
  });

  it('gère null sans erreur', () => {
    expect(categorieOptionsPipe(null as any)).toHaveLength(0);
  });

  it('utilise l\'id comme value et le nom comme label', () => {
    const result = categorieOptionsPipe([{ id: 42, nom: 'Test' }]);
    expect(result[0].value).toBe(42);
    expect(result[0].label).toBe('Test');
  });
});

// ── RoleOptionsPipe ───────────────────────────────────────────────────────────

function roleOptionsPipe(roles: { id: number; code: string }[]) {
  return (roles ?? [])
    .filter(r => r.code !== 'Administrateur')
    .map(r => ({ value: r.id, label: r.code }));
}

describe('RoleOptionsPipe', () => {
  it('exclut le rôle Administrateur', () => {
    const roles = [
      { id: 1, code: 'Administrateur' },
      { id: 2, code: 'Responsable' },
      { id: 3, code: 'Agent' }
    ];
    const result = roleOptionsPipe(roles);
    expect(result).toHaveLength(2);
    expect(result.find(r => r.label === 'Administrateur')).toBeUndefined();
  });

  it('inclut tous les autres rôles', () => {
    const roles = [{ id: 2, code: 'Responsable' }, { id: 3, code: 'Direction' }];
    const result = roleOptionsPipe(roles);
    expect(result).toHaveLength(2);
  });

  it('retourne un tableau vide si tous les rôles sont Administrateur', () => {
    const result = roleOptionsPipe([{ id: 1, code: 'Administrateur' }]);
    expect(result).toHaveLength(0);
  });

  it('gère null sans erreur', () => {
    expect(roleOptionsPipe(null as any)).toHaveLength(0);
  });
});

// ── CircuitOptionsPipe ────────────────────────────────────────────────────────

function circuitOptionsPipe(circuits: { id: number; nom: string }[]) {
  return (circuits ?? []).map(c => ({ value: c.id, label: c.nom }));
}

describe('CircuitOptionsPipe', () => {
  it('transforme les circuits en SelectOption[]', () => {
    const circuits = [{ id: 1, nom: 'Circuit A' }, { id: 2, nom: 'Circuit B' }];
    const result = circuitOptionsPipe(circuits);
    expect(result).toHaveLength(2);
    expect(result[0]).toEqual({ value: 1, label: 'Circuit A' });
  });

  it('retourne un tableau vide pour un tableau vide', () => {
    expect(circuitOptionsPipe([])).toHaveLength(0);
  });
});

// ── StyledDatePipe ────────────────────────────────────────────────────────────

describe('StyledDatePipe — logique de formatage', () => {

  function styledDatePipe(value: any, format: 'date' | 'time' | 'datetime' = 'datetime'): string {
    if (!value) return '';

    const date = new Date(value);
    const pad = (n: number) => String(n).padStart(2, '0');
    const day = pad(date.getDate());
    const month = pad(date.getMonth() + 1);
    const year = date.getFullYear();
    const hours = pad(date.getHours());
    const minutes = pad(date.getMinutes());

    switch (format) {
      case 'date':
        return `<span class="date-badge">${day}/${month}/${year}</span>`;
      case 'time':
        return `<span class="time-badge">${hours}:${minutes}</span>`;
      case 'datetime':
      default:
        return `<span class="datetime-badge">${day}/${month}/${year} ${hours}:${minutes}</span>`;
    }
  }

  it('retourne une chaîne vide pour une valeur nulle', () => {
    expect(styledDatePipe(null)).toBe('');
    expect(styledDatePipe(undefined)).toBe('');
    expect(styledDatePipe('')).toBe('');
  });

  it('format "date" retourne un span avec classe date-badge', () => {
    const result = styledDatePipe('2026-04-15T10:30:00', 'date');
    expect(result).toContain('class="date-badge"');
    expect(result).toContain('15/04/2026');
  });

  it('format "time" retourne un span avec classe time-badge', () => {
    const result = styledDatePipe('2026-04-15T10:30:00', 'time');
    expect(result).toContain('class="time-badge"');
    expect(result).toContain('10:30');
  });

  it('format "datetime" (défaut) retourne un span avec classe datetime-badge', () => {
    const result = styledDatePipe('2026-04-15T10:30:00', 'datetime');
    expect(result).toContain('class="datetime-badge"');
    expect(result).toContain('15/04/2026');
  });

  it('format par défaut est "datetime"', () => {
    const result = styledDatePipe('2026-04-15T10:30:00');
    expect(result).toContain('datetime-badge');
  });
});
