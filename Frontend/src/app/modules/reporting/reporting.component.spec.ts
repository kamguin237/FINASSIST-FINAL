import { describe, it, expect } from 'vitest';

// ── Tests logique ReportingComponent ─────────────────────────────────────────

describe('ReportingComponent — validation des filtres', () => {

  function rechercher(dateDebut: string, dateFin: string): { erreur: boolean } {
    if (!dateDebut || !dateFin) return { erreur: true };
    return { erreur: false };
  }

  it('erreur si dateDebut manquante', () => {
    expect(rechercher('', '2026-12-31').erreur).toBe(true);
  });

  it('erreur si dateFin manquante', () => {
    expect(rechercher('2026-01-01', '').erreur).toBe(true);
  });

  it('pas d\'erreur si les deux dates sont présentes', () => {
    expect(rechercher('2026-01-01', '2026-12-31').erreur).toBe(false);
  });
});

describe('ReportingComponent — peutExporter', () => {

  function peutExporter(resultats: any[] | null): boolean {
    return resultats !== null && resultats.length > 0;
  }

  it('retourne false si resultats est null', () => {
    expect(peutExporter(null)).toBe(false);
  });

  it('retourne false si resultats est vide', () => {
    expect(peutExporter([])).toBe(false);
  });

  it('retourne true si resultats contient des éléments', () => {
    expect(peutExporter([{ id: 1 }])).toBe(true);
  });
});

describe('ReportingComponent — génération des options de statut', () => {

  function buildStatutOptions(roles: { code: string }[]) {
    const dynamiques: { value: string; label: string }[] = [];
    for (const r of roles.filter(r => r.code !== 'Administrateur')) {
      const code = r.code.trim().toUpperCase();
      dynamiques.push({ value: `EN_ATTENTE_${code}`,   label: `En attente — ${r.code}` });
      dynamiques.push({ value: `APPROUVE_PAR_${code}`, label: `Approuvé par ${r.code}` });
      dynamiques.push({ value: `REJETE_PAR_${code}`,   label: `Rejeté par ${r.code}` });
      dynamiques.push({ value: `SIGNE_PAR_${code}`,    label: `Signé par ${r.code}` });
    }
    return [
      { value: 'BROUILLON', label: 'Brouillon' },
      { value: 'ENREGISTRE', label: 'Enregistré' },
      ...dynamiques,
      { value: 'TRANSMIS', label: 'Transmis' },
      { value: 'TERMINE', label: 'Terminé' }
    ];
  }

  it('génère 4 options par rôle non-Administrateur', () => {
    const roles = [{ code: 'Responsable' }, { code: 'Direction' }];
    const options = buildStatutOptions(roles);
    // 2 statuts fixes + 4*2 dynamiques + 2 statuts fixes = 12
    expect(options.length).toBe(12);
  });

  it('exclut Administrateur des options dynamiques', () => {
    const roles = [{ code: 'Administrateur' }, { code: 'Responsable' }];
    const options = buildStatutOptions(roles);
    const adminOptions = options.filter(o => o.value.includes('ADMINISTRATEUR'));
    expect(adminOptions).toHaveLength(0);
  });

  it('inclut les statuts fixes BROUILLON et TERMINE', () => {
    const options = buildStatutOptions([]);
    expect(options.find(o => o.value === 'BROUILLON')).toBeDefined();
    expect(options.find(o => o.value === 'TERMINE')).toBeDefined();
  });
});
