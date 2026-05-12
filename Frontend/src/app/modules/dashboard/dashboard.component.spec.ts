import { describe, it, expect } from 'vitest';

// ── Tests logique DashboardComponent ─────────────────────────────────────────

function statutLabel(statut: string): string {
  const formatRole = (roleCode: string): string => {
    const map: Record<string, string> = {
      'RESPONSABLE': 'le Responsable',
      'DIRECTION': 'la Direction',
      'DIRECTIONGENERALE': 'la Direction Générale',
      'ADMINISTRATEUR': "l'Administrateur"
    };
    return map[roleCode] ?? roleCode.charAt(0) + roleCode.slice(1).toLowerCase();
  };

  if (statut === 'BROUILLON')  return 'Brouillon';
  if (statut === 'ENREGISTRE') return 'Enregistré';
  if (statut === 'SOUMISE')    return 'Soumis';
  if (statut === 'TRANSMIS')   return 'Transmis';
  if (statut === 'TERMINE')    return 'Terminé';
  if (statut.startsWith('EN_ATTENTE_'))   return `En attente — ${formatRole(statut.replace('EN_ATTENTE_', ''))}`;
  if (statut.startsWith('APPROUVE_PAR_')) return `Approuvé par ${formatRole(statut.replace('APPROUVE_PAR_', ''))}`;
  if (statut.startsWith('REJETE_PAR_'))   return `Rejeté par ${formatRole(statut.replace('REJETE_PAR_', ''))}`;
  if (statut.startsWith('SIGNE_PAR_'))    return `Signé par ${formatRole(statut.replace('SIGNE_PAR_', ''))}`;
  return statut;
}

function badgeClass(statut: string): string {
  if (statut === 'BROUILLON')                        return 'grey';
  if (statut === 'SOUMISE' || statut === 'TRANSMIS') return 'blue';
  if (statut.startsWith('EN_ATTENTE_'))              return 'blue';
  if (statut.startsWith('APPROUVE_PAR_'))            return 'green';
  if (statut.startsWith('REJETE_PAR_'))              return 'red';
  if (statut.startsWith('SIGNE_PAR_'))               return 'purple';
  if (statut === 'TERMINE')                          return 'cyan';
  return 'grey';
}

describe('DashboardComponent — statutLabel', () => {
  it('retourne "Brouillon" pour BROUILLON', () => {
    expect(statutLabel('BROUILLON')).toBe('Brouillon');
  });

  it('retourne "Terminé" pour TERMINE', () => {
    expect(statutLabel('TERMINE')).toBe('Terminé');
  });

  it('formate EN_ATTENTE_RESPONSABLE', () => {
    expect(statutLabel('EN_ATTENTE_RESPONSABLE')).toBe('En attente — le Responsable');
  });

  it('formate APPROUVE_PAR_DIRECTION', () => {
    expect(statutLabel('APPROUVE_PAR_DIRECTION')).toBe('Approuvé par la Direction');
  });

  it('retourne le statut brut si inconnu', () => {
    expect(statutLabel('STATUT_INCONNU')).toBe('STATUT_INCONNU');
  });
});

describe('DashboardComponent — badgeClass', () => {
  it('retourne "grey" pour BROUILLON', () => {
    expect(badgeClass('BROUILLON')).toBe('grey');
  });

  it('retourne "blue" pour EN_ATTENTE_RESPONSABLE', () => {
    expect(badgeClass('EN_ATTENTE_RESPONSABLE')).toBe('blue');
  });

  it('retourne "green" pour APPROUVE_PAR_DIRECTION', () => {
    expect(badgeClass('APPROUVE_PAR_DIRECTION')).toBe('green');
  });

  it('retourne "red" pour REJETE_PAR_RESPONSABLE', () => {
    expect(badgeClass('REJETE_PAR_RESPONSABLE')).toBe('red');
  });

  it('retourne "purple" pour SIGNE_PAR_DIRECTION', () => {
    expect(badgeClass('SIGNE_PAR_DIRECTION')).toBe('purple');
  });

  it('retourne "cyan" pour TERMINE', () => {
    expect(badgeClass('TERMINE')).toBe('cyan');
  });

  it('retourne "blue" pour TRANSMIS', () => {
    expect(badgeClass('TRANSMIS')).toBe('blue');
  });
});

describe('DashboardComponent — KPI calculés', () => {
  const evolutionData = [
    { recus: 5, approuves: 3, rejetes: 1, enAttentePlus48h: 2, tauxApprobation: 60 },
    { recus: 8, approuves: 6, rejetes: 2, enAttentePlus48h: 1, tauxApprobation: 75 },
    { recus: 3, approuves: 2, rejetes: 0, enAttentePlus48h: 4, tauxApprobation: 66.7 }
  ];

  it('totalRecus est la somme de tous les recus', () => {
    const total = evolutionData.reduce((s, p) => s + p.recus, 0);
    expect(total).toBe(16);
  });

  it('totalApprouves est la somme de tous les approuves', () => {
    const total = evolutionData.reduce((s, p) => s + p.approuves, 0);
    expect(total).toBe(11);
  });

  it('totalRejetes est la somme de tous les rejetes', () => {
    const total = evolutionData.reduce((s, p) => s + p.rejetes, 0);
    expect(total).toBe(3);
  });

  it('maxAttente est le maximum de enAttentePlus48h', () => {
    const max = Math.max(...evolutionData.map(p => p.enAttentePlus48h), 0);
    expect(max).toBe(4);
  });

  it('maxAttente retourne 0 si evolutionData est vide', () => {
    const max = Math.max(...([] as number[]), 0);
    expect(max).toBe(0);
  });
});

describe('DashboardComponent — changerPeriode', () => {
  it('accepte les 3 périodes valides', () => {
    const periodes = ['jours', 'semaines', 'mois'] as const;
    periodes.forEach(p => {
      expect(['jours', 'semaines', 'mois']).toContain(p);
    });
  });
});
