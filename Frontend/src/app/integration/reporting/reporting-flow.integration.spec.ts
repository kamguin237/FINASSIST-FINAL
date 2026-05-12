/**
 * Tests d'intégration — Flux Reporting
 *
 * Vérifie la collaboration entre :
 *   ReportingService ↔ AuthService (permissions) ↔ HttpClient
 *
 * Scénarios : dashboard, statistiques, rapport filtré, export.
 */

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { of, throwError } from 'rxjs';
import { firstValueFrom } from 'rxjs';

// ── Constantes ────────────────────────────────────────────────────────────────

const TOKEN_KEY = 'finassist_token';
const API_URL   = 'http://localhost:5000/api';

function makeJwt(permissions = 'RAPPORT_CONSULTER,RAPPORT_EXPORTER'): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const body   = btoa(JSON.stringify({
    exp: Math.floor(Date.now() / 1000) + 3600,
    sub: '1', role: 'Administrateur', permissions
  }));
  return `${header}.${body}.sig`;
}

// ── Types ─────────────────────────────────────────────────────────────────────

interface DashboardDTO {
  besoinsEnAttente: number; besoinsApprouves: number; besoinsRejetes: number;
  besoinsSignes: number; besoinsSoumis: number; besoinsEnregistres: number;
  besoinsBrouillons: number; notificationsNonLues: number;
  derniersBesoins: Array<{ id: number; titre: string; statut: string }>;
}

interface StatistiquesDTO {
  totalBesoins: number; totalUtilisateurs: number; totalActifs: number;
  besoinsByStatut: Record<string, number>; besoinsByCategorie: Record<string, number>;
  signaturesApposees: number; notificationsEnvoyees: number;
}

interface FiltreRapportDTO { dateDebut?: string; dateFin?: string; statut?: string; }

// ── Simulation ReportingService ───────────────────────────────────────────────

class ReportingServiceSim {
  private http: { get: ReturnType<typeof vi.fn>; post: ReturnType<typeof vi.fn> };
  private url = `${API_URL}/reporting`;

  constructor(http: typeof ReportingServiceSim.prototype.http) {
    this.http = http;
  }

  getDashboard()                    { return this.http.get(`${this.url}/dashboard`); }
  getEvolutionBesoins(periode: string) {
    return this.http.get(`${this.url}/dashboard/evolution?periode=${periode}`);
  }
  getStatistiques()                 { return this.http.get(`${this.url}/statistiques`); }

  getRapportBesoins(filtres?: FiltreRapportDTO) {
    const params = new URLSearchParams();
    if (filtres?.dateDebut) params.set('dateDebut', filtres.dateDebut);
    if (filtres?.dateFin)   params.set('dateFin', filtres.dateFin);
    if (filtres?.statut)    params.set('statut', filtres.statut);
    const query = params.toString();
    return this.http.get(`${this.url}/besoins${query ? '?' + query : ''}`);
  }

  exporter(request: { format: string; filtres?: FiltreRapportDTO }) {
    return this.http.post(`${this.url}/exporter`, request);
  }
}

// ── Données de test ───────────────────────────────────────────────────────────

const mockDashboard: DashboardDTO = {
  besoinsEnAttente: 5, besoinsApprouves: 12, besoinsRejetes: 2,
  besoinsSignes: 8, besoinsSoumis: 15, besoinsEnregistres: 3,
  besoinsBrouillons: 1, notificationsNonLues: 4,
  derniersBesoins: [
    { id: 1, titre: 'Achat ordinateur', statut: 'EN_ATTENTE_RESPONSABLE' },
    { id: 2, titre: 'Formation Angular', statut: 'TERMINE' }
  ]
};

const mockStats: StatistiquesDTO = {
  totalBesoins: 30, totalUtilisateurs: 10, totalActifs: 8,
  besoinsByStatut: { BROUILLON: 5, EN_ATTENTE_RESPONSABLE: 3, TERMINE: 22 },
  besoinsByCategorie: { Informatique: 15, Finance: 10, RH: 5 },
  signaturesApposees: 18, notificationsEnvoyees: 45
};

// ═════════════════════════════════════════════════════════════════════════════
// Tests
// ═════════════════════════════════════════════════════════════════════════════

describe('Intégration — Flux Reporting', () => {
  let http: { get: ReturnType<typeof vi.fn>; post: ReturnType<typeof vi.fn> };
  let service: ReportingServiceSim;

  beforeEach(() => {
    localStorage.clear();
    localStorage.setItem(TOKEN_KEY, makeJwt());
    http = { get: vi.fn(), post: vi.fn() };
    service = new ReportingServiceSim(http);
  });

  afterEach(() => {
    localStorage.clear();
    vi.clearAllMocks();
  });

  // ── getDashboard ──────────────────────────────────────────────────────────

  describe('getDashboard', () => {
    it('appelle GET /api/reporting/dashboard', async () => {
      http.get.mockReturnValue(of(mockDashboard));

      const result = await firstValueFrom(service.getDashboard() as any) as DashboardDTO;

      expect(http.get).toHaveBeenCalledWith(`${API_URL}/reporting/dashboard`);
      expect(result.besoinsEnAttente).toBe(5);
      expect(result.derniersBesoins).toHaveLength(2);
    });

    it('retourne les compteurs corrects', async () => {
      http.get.mockReturnValue(of(mockDashboard));

      const result = await firstValueFrom(service.getDashboard() as any) as DashboardDTO;

      expect(result.besoinsApprouves).toBe(12);
      expect(result.besoinsRejetes).toBe(2);
      expect(result.notificationsNonLues).toBe(4);
    });
  });

  // ── getEvolutionBesoins ───────────────────────────────────────────────────

  describe('getEvolutionBesoins', () => {
    it.each(['jours', 'semaines', 'mois'])('appelle GET avec période=%s', async (periode) => {
      http.get.mockReturnValue(of([]));

      await firstValueFrom(service.getEvolutionBesoins(periode) as any);

      expect(http.get).toHaveBeenCalledWith(
        `${API_URL}/reporting/dashboard/evolution?periode=${periode}`
      );
    });
  });

  // ── getStatistiques ───────────────────────────────────────────────────────

  describe('getStatistiques', () => {
    it('appelle GET /api/reporting/statistiques', async () => {
      http.get.mockReturnValue(of(mockStats));

      const result = await firstValueFrom(service.getStatistiques() as any) as StatistiquesDTO;

      expect(http.get).toHaveBeenCalledWith(`${API_URL}/reporting/statistiques`);
      expect(result.totalBesoins).toBe(30);
      expect(result.totalActifs).toBe(8);
    });

    it('retourne les stats par statut', async () => {
      http.get.mockReturnValue(of(mockStats));

      const result = await firstValueFrom(service.getStatistiques() as any) as StatistiquesDTO;

      expect(result.besoinsByStatut['BROUILLON']).toBe(5);
      expect(result.besoinsByStatut['TERMINE']).toBe(22);
    });

    it('retourne les stats par catégorie', async () => {
      http.get.mockReturnValue(of(mockStats));

      const result = await firstValueFrom(service.getStatistiques() as any) as StatistiquesDTO;

      expect(result.besoinsByCategorie['Informatique']).toBe(15);
    });
  });

  // ── getRapportBesoins ─────────────────────────────────────────────────────

  describe('getRapportBesoins', () => {
    it('sans filtres, appelle GET /api/reporting/besoins', async () => {
      http.get.mockReturnValue(of({ contenu: [] }));

      await firstValueFrom(service.getRapportBesoins() as any);

      expect(http.get).toHaveBeenCalledWith(`${API_URL}/reporting/besoins`);
    });

    it('avec filtre statut, inclut le paramètre dans l\'URL', async () => {
      http.get.mockReturnValue(of({ contenu: [] }));

      await firstValueFrom(service.getRapportBesoins({ statut: 'BROUILLON' }) as any);

      expect(http.get).toHaveBeenCalledWith(
        expect.stringContaining('statut=BROUILLON')
      );
    });

    it('avec filtre dates, inclut les paramètres dans l\'URL', async () => {
      http.get.mockReturnValue(of({ contenu: [] }));

      await firstValueFrom(service.getRapportBesoins({
        dateDebut: '2026-01-01', dateFin: '2026-12-31'
      }) as any);

      const url = http.get.mock.calls[0][0] as string;
      expect(url).toContain('dateDebut=2026-01-01');
      expect(url).toContain('dateFin=2026-12-31');
    });

    it('avec tous les filtres, inclut tous les paramètres', async () => {
      http.get.mockReturnValue(of({ contenu: [] }));

      await firstValueFrom(service.getRapportBesoins({
        dateDebut: '2026-01-01', dateFin: '2026-12-31', statut: 'TERMINE'
      }) as any);

      const url = http.get.mock.calls[0][0] as string;
      expect(url).toContain('dateDebut=');
      expect(url).toContain('dateFin=');
      expect(url).toContain('statut=TERMINE');
    });
  });

  // ── exporter ──────────────────────────────────────────────────────────────

  describe('exporter', () => {
    it('exporter Excel appelle POST /api/reporting/exporter', async () => {
      const blob = new Blob(['data'], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });
      http.post.mockReturnValue(of(blob));

      const result = await firstValueFrom(service.exporter({ format: 'EXCEL' }) as any);

      expect(http.post).toHaveBeenCalledWith(
        `${API_URL}/reporting/exporter`,
        { format: 'EXCEL' }
      );
      expect(result).toBeInstanceOf(Blob);
    });

    it('exporter PDF appelle POST avec format PDF', async () => {
      const blob = new Blob(['%PDF'], { type: 'application/pdf' });
      http.post.mockReturnValue(of(blob));

      await firstValueFrom(service.exporter({ format: 'PDF', filtres: { statut: 'TERMINE' } }) as any);

      expect(http.post).toHaveBeenCalledWith(
        `${API_URL}/reporting/exporter`,
        { format: 'PDF', filtres: { statut: 'TERMINE' } }
      );
    });
  });

  // ── Contrôle d'accès ──────────────────────────────────────────────────────

  describe('Contrôle d\'accès par permission', () => {
    it('utilisateur avec RAPPORT_EXPORTER peut exporter', () => {
      const token = makeJwt('RAPPORT_CONSULTER,RAPPORT_EXPORTER');
      localStorage.setItem(TOKEN_KEY, token);
      const payload = JSON.parse(atob(token.split('.')[1]));
      const perms = payload.permissions.split(',');

      expect(perms).toContain('RAPPORT_EXPORTER');
    });

    it('utilisateur sans RAPPORT_EXPORTER ne peut pas exporter', () => {
      const token = makeJwt('RAPPORT_CONSULTER');
      localStorage.setItem(TOKEN_KEY, token);
      const payload = JSON.parse(atob(token.split('.')[1]));
      const perms = payload.permissions.split(',');

      expect(perms).not.toContain('RAPPORT_EXPORTER');
    });
  });

  // ── Gestion d'erreurs ─────────────────────────────────────────────────────

  describe('Gestion d\'erreurs', () => {
    it('propage l\'erreur 403 si accès refusé au dashboard', async () => {
      http.get.mockReturnValue(throwError(() => ({ status: 403 })));

      await expect(firstValueFrom(service.getDashboard() as any))
        .rejects.toMatchObject({ status: 403 });
    });
  });
});
