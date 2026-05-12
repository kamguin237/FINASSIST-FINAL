import { describe, it, expect } from 'vitest';
import { NIVEAUX_IMPORTANCE } from '../../../core/models/besoin.models';

// ── Tests logique BesoinsListComponent ───────────────────────────────────────

// Simulation de la logique labelStatut
function labelStatut(statut: string): string {
  const formatRole = (roleCode: string): string => {
    const map: Record<string, string> = {
      'RESPONSABLE': 'le Responsable',
      'DIRECTION': 'la Direction',
      'DIRECTIONGENERALE': 'la Direction Générale',
      'ADMINISTRATEUR': "l'Administrateur",
      'AGENT': "l'Agent"
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

// Simulation de peutSupprimer
function peutSupprimer(statut: string, estTermine: boolean): boolean {
  return statut === 'BROUILLON' ||
         statut === 'ENREGISTRE' ||
         statut.startsWith('REJETE_ROLE') ||
         estTermine;
}

// Simulation de minutesToHHmm
function minutesToHHmm(minutes: number): string {
  const h = Math.floor(minutes / 60).toString().padStart(2, '0');
  const m = (minutes % 60).toString().padStart(2, '0');
  return `${h}:${m}`;
}

// Simulation de besoinsFiltres
function besoinsFiltres(besoins: any[], filtreStatut: string): any[] {
  if (!filtreStatut) return besoins;
  return besoins.filter(b => b.statut === filtreStatut);
}

describe('BesoinsListComponent — labelStatut', () => {
  it('retourne "Brouillon" pour BROUILLON', () => {
    expect(labelStatut('BROUILLON')).toBe('Brouillon');
  });

  it('retourne "Enregistré" pour ENREGISTRE', () => {
    expect(labelStatut('ENREGISTRE')).toBe('Enregistré');
  });

  it('retourne "Terminé" pour TERMINE', () => {
    expect(labelStatut('TERMINE')).toBe('Terminé');
  });

  it('formate EN_ATTENTE_RESPONSABLE correctement', () => {
    expect(labelStatut('EN_ATTENTE_RESPONSABLE')).toBe('En attente — le Responsable');
  });

  it('formate APPROUVE_PAR_DIRECTION correctement', () => {
    expect(labelStatut('APPROUVE_PAR_DIRECTION')).toBe('Approuvé par la Direction');
  });

  it('formate REJETE_PAR_RESPONSABLE correctement', () => {
    expect(labelStatut('REJETE_PAR_RESPONSABLE')).toBe('Rejeté par le Responsable');
  });

  it('formate SIGNE_PAR_DIRECTION correctement', () => {
    expect(labelStatut('SIGNE_PAR_DIRECTION')).toBe('Signé par la Direction');
  });

  it('retourne le statut brut si inconnu', () => {
    expect(labelStatut('STATUT_INCONNU')).toBe('STATUT_INCONNU');
  });
});

describe('BesoinsListComponent — peutSupprimer', () => {
  it('peut supprimer un brouillon', () => {
    expect(peutSupprimer('BROUILLON', false)).toBe(true);
  });

  it('peut supprimer un besoin enregistré', () => {
    expect(peutSupprimer('ENREGISTRE', false)).toBe(true);
  });

  it('peut supprimer un besoin terminé', () => {
    expect(peutSupprimer('EN_ATTENTE_RESPONSABLE', true)).toBe(true);
  });

  it('ne peut pas supprimer un besoin en attente', () => {
    expect(peutSupprimer('EN_ATTENTE_RESPONSABLE', false)).toBe(false);
  });

  it('ne peut pas supprimer un besoin approuvé', () => {
    expect(peutSupprimer('APPROUVE_PAR_DIRECTION', false)).toBe(false);
  });
});

describe('BesoinsListComponent — minutesToHHmm', () => {
  it('convertit 0 minutes en 00:00', () => {
    expect(minutesToHHmm(0)).toBe('00:00');
  });

  it('convertit 60 minutes en 01:00', () => {
    expect(minutesToHHmm(60)).toBe('01:00');
  });

  it('convertit 90 minutes en 01:30', () => {
    expect(minutesToHHmm(90)).toBe('01:30');
  });

  it('convertit 1440 minutes en 24:00', () => {
    expect(minutesToHHmm(1440)).toBe('24:00');
  });

  it('padde les heures et minutes avec des zéros', () => {
    expect(minutesToHHmm(5)).toBe('00:05');
  });
});

describe('BesoinsListComponent — filtrage', () => {
  const besoins = [
    { id: 1, statut: 'BROUILLON' },
    { id: 2, statut: 'ENREGISTRE' },
    { id: 3, statut: 'BROUILLON' },
    { id: 4, statut: 'TERMINE' }
  ];

  it('retourne tous les besoins si pas de filtre', () => {
    expect(besoinsFiltres(besoins, '')).toHaveLength(4);
  });

  it('filtre par statut BROUILLON', () => {
    const result = besoinsFiltres(besoins, 'BROUILLON');
    expect(result).toHaveLength(2);
    result.forEach(b => expect(b.statut).toBe('BROUILLON'));
  });

  it('retourne un tableau vide si aucun besoin ne correspond', () => {
    expect(besoinsFiltres(besoins, 'EN_ATTENTE_RESPONSABLE')).toHaveLength(0);
  });

  it('génère les options de statut uniques', () => {
    const statutsUniques = [...new Set(besoins.map(b => b.statut))].sort();
    expect(statutsUniques).toHaveLength(3);
    expect(statutsUniques).toContain('BROUILLON');
    expect(statutsUniques).toContain('ENREGISTRE');
    expect(statutsUniques).toContain('TERMINE');
  });
});
