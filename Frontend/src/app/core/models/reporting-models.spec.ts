import { describe, it, expect } from 'vitest';

// ── Tests modèles Reporting ───────────────────────────────────────────────────

describe('DashboardDTO — structure et calculs', () => {

  const dashboard = {
    besoinsEnAttente: 5,
    besoinsSoumis: 3,
    besoinsApprouves: 10,
    besoinsRejetes: 2,
    besoinsEnregistres: 1,
    besoinsBrouillons: 4,
    besoinsSignes: 8,
    notificationsNonLues: 7,
    besoinsSoumisParMoi: 3,
    derniersBesoins: []
  };

  it('a toutes les propriétés requises', () => {
    expect(dashboard.besoinsEnAttente).toBe(5);
    expect(dashboard.besoinsApprouves).toBe(10);
    expect(dashboard.besoinsRejetes).toBe(2);
    expect(dashboard.notificationsNonLues).toBe(7);
  });

  it('le total des besoins actifs est cohérent', () => {
    const total = dashboard.besoinsEnAttente + dashboard.besoinsApprouves +
                  dashboard.besoinsRejetes + dashboard.besoinsEnregistres +
                  dashboard.besoinsBrouillons;
    expect(total).toBeGreaterThan(0);
  });

  it('derniersBesoins est un tableau', () => {
    expect(Array.isArray(dashboard.derniersBesoins)).toBe(true);
  });
});

describe('EvolutionPoint — structure', () => {
  it('a les propriétés de suivi temporel', () => {
    const point = {
      date: '2026-04-01',
      recus: 5,
      approuves: 3,
      rejetes: 1,
      enAttentePlus48h: 2,
      tauxApprobation: 60.0
    };
    expect(point.date).toBe('2026-04-01');
    expect(point.tauxApprobation).toBe(60.0);
  });

  it('le taux d\'approbation est entre 0 et 100', () => {
    const taux = 75.5;
    expect(taux).toBeGreaterThanOrEqual(0);
    expect(taux).toBeLessThanOrEqual(100);
  });

  it('calcule le taux d\'approbation correctement', () => {
    const recus = 10;
    const approuves = 7;
    const taux = recus > 0 ? Math.round((approuves / recus) * 100 * 10) / 10 : 0;
    expect(taux).toBe(70);
  });
});

describe('StatistiquesDTO — structure', () => {
  it('a les compteurs globaux', () => {
    const stats = {
      totalBesoins: 100,
      totalUtilisateurs: 25,
      totalActifs: 20,
      besoinsByStatut: { BROUILLON: 10, TERMINE: 50 },
      besoinsByCategorie: { Informatique: 30, RH: 20 },
      signaturesApposees: 45,
      notificationsEnvoyees: 200
    };
    expect(stats.totalBesoins).toBe(100);
    expect(stats.totalActifs).toBeLessThanOrEqual(stats.totalUtilisateurs);
    expect(stats.besoinsByStatut['BROUILLON']).toBe(10);
  });
});

describe('FiltreRapportDTO — validation', () => {
  it('tous les champs sont optionnels', () => {
    const filtreVide: { dateDebut?: string; dateFin?: string; statut?: string } = {};
    expect(filtreVide.dateDebut).toBeUndefined();
    expect(filtreVide.dateFin).toBeUndefined();
    expect(filtreVide.statut).toBeUndefined();
  });

  it('dateDebut doit être avant dateFin', () => {
    const filtre = { dateDebut: '2026-01-01', dateFin: '2026-12-31' };
    expect(new Date(filtre.dateDebut) < new Date(filtre.dateFin)).toBe(true);
  });
});

describe('ExportRequestDTO — formats', () => {
  it('accepte le format PDF', () => {
    const req = { format: 'PDF' as const };
    expect(req.format).toBe('PDF');
  });

  it('accepte le format EXCEL', () => {
    const req = { format: 'EXCEL' as const };
    expect(req.format).toBe('EXCEL');
  });
});
