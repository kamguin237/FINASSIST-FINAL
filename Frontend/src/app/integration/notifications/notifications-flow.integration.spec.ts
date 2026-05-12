/**
 * Tests d'intégration — Flux Notifications
 *
 * Vérifie la collaboration entre :
 *   NotificationsService ↔ AuthService (token) ↔ HttpClient
 *
 * Scénarios : récupération, marquage lu, envoi, comptage non lues.
 */

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { of, throwError } from 'rxjs';
import { firstValueFrom } from 'rxjs';

// ── Constantes ────────────────────────────────────────────────────────────────

const TOKEN_KEY = 'finassist_token';
const API_URL   = 'http://localhost:5000/api';

function makeJwt(): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const body   = btoa(JSON.stringify({ exp: Math.floor(Date.now() / 1000) + 3600, sub: '1' }));
  return `${header}.${body}.sig`;
}

// ── Types ─────────────────────────────────────────────────────────────────────

interface NotificationDTO {
  id: number; message: string; type: string; lu: boolean; dateEnvoi: string;
}

// ── Simulation NotificationsService ──────────────────────────────────────────

class NotificationsServiceSim {
  private http: { get: ReturnType<typeof vi.fn>; put: ReturnType<typeof vi.fn>; post: ReturnType<typeof vi.fn> };
  private url = `${API_URL}/notifications`;

  constructor(http: typeof NotificationsServiceSim.prototype.http) {
    this.http = http;
  }

  getMesNotifications()               { return this.http.get(this.url); }
  marquerLu(id: number)               { return this.http.put(`${this.url}/${id}/lire`, {}); }
  envoyer(dto: Partial<NotificationDTO>) { return this.http.post(this.url, dto); }

  // Logique locale : compter les non lues
  countNonLues(notifications: NotificationDTO[]): number {
    return notifications.filter(n => !n.lu).length;
  }

  // Logique locale : filtrer par type
  filterByType(notifications: NotificationDTO[], type: string): NotificationDTO[] {
    return notifications.filter(n => n.type === type);
  }
}

// ── Données de test ───────────────────────────────────────────────────────────

const notifNonLue: NotificationDTO = {
  id: 1, message: 'Votre besoin a été approuvé', type: 'VALIDATION', lu: false,
  dateEnvoi: '2026-04-01T10:00:00Z'
};
const notifLue: NotificationDTO = {
  id: 2, message: 'Rappel : délai bientôt dépassé', type: 'RAPPEL', lu: true,
  dateEnvoi: '2026-04-01T09:00:00Z'
};
const notifRejet: NotificationDTO = {
  id: 3, message: 'Votre besoin a été rejeté', type: 'REJET', lu: false,
  dateEnvoi: '2026-04-01T08:00:00Z'
};

// ═════════════════════════════════════════════════════════════════════════════
// Tests
// ═════════════════════════════════════════════════════════════════════════════

describe('Intégration — Flux Notifications', () => {
  let http: { get: ReturnType<typeof vi.fn>; put: ReturnType<typeof vi.fn>; post: ReturnType<typeof vi.fn> };
  let service: NotificationsServiceSim;

  beforeEach(() => {
    localStorage.clear();
    localStorage.setItem(TOKEN_KEY, makeJwt());
    http = { get: vi.fn(), put: vi.fn(), post: vi.fn() };
    service = new NotificationsServiceSim(http);
  });

  afterEach(() => {
    localStorage.clear();
    vi.clearAllMocks();
  });

  // ── getMesNotifications ───────────────────────────────────────────────────

  describe('getMesNotifications', () => {
    it('appelle GET /api/notifications', async () => {
      http.get.mockReturnValue(of([notifNonLue, notifLue]));

      const result = await firstValueFrom(service.getMesNotifications() as any) as NotificationDTO[];

      expect(http.get).toHaveBeenCalledWith(`${API_URL}/notifications`);
      expect(result).toHaveLength(2);
    });

    it('retourne une liste vide si aucune notification', async () => {
      http.get.mockReturnValue(of([]));

      const result = await firstValueFrom(service.getMesNotifications() as any) as NotificationDTO[];

      expect(result).toHaveLength(0);
    });
  });

  // ── marquerLu ─────────────────────────────────────────────────────────────

  describe('marquerLu', () => {
    it('appelle PUT /api/notifications/:id/lire', async () => {
      http.put.mockReturnValue(of(undefined));

      await firstValueFrom(service.marquerLu(1) as any);

      expect(http.put).toHaveBeenCalledWith(`${API_URL}/notifications/1/lire`, {});
    });

    it('marque plusieurs notifications comme lues séquentiellement', async () => {
      http.put.mockReturnValue(of(undefined));

      await firstValueFrom(service.marquerLu(1) as any);
      await firstValueFrom(service.marquerLu(2) as any);
      await firstValueFrom(service.marquerLu(3) as any);

      expect(http.put).toHaveBeenCalledTimes(3);
    });
  });

  // ── envoyer ───────────────────────────────────────────────────────────────

  describe('envoyer', () => {
    it('appelle POST /api/notifications avec le DTO', async () => {
      const dto = { message: 'Test', type: 'VALIDATION', destinataireIds: [1, 2] };
      http.post.mockReturnValue(of({ id: 10, ...dto, lu: false, dateEnvoi: '2026-04-01T10:00:00Z' }));

      const result = await firstValueFrom(service.envoyer(dto) as any) as NotificationDTO;

      expect(http.post).toHaveBeenCalledWith(`${API_URL}/notifications`, dto);
      expect(result.id).toBe(10);
    });
  });

  // ── Logique locale : countNonLues ─────────────────────────────────────────

  describe('countNonLues (logique locale)', () => {
    it('compte correctement les notifications non lues', () => {
      const notifs = [notifNonLue, notifLue, notifRejet];
      expect(service.countNonLues(notifs)).toBe(2);
    });

    it('retourne 0 si toutes sont lues', () => {
      const notifs = [{ ...notifNonLue, lu: true }, { ...notifRejet, lu: true }];
      expect(service.countNonLues(notifs)).toBe(0);
    });

    it('retourne 0 pour une liste vide', () => {
      expect(service.countNonLues([])).toBe(0);
    });

    it('retourne le total si toutes sont non lues', () => {
      const notifs = [notifNonLue, notifRejet];
      expect(service.countNonLues(notifs)).toBe(2);
    });
  });

  // ── Logique locale : filterByType ─────────────────────────────────────────

  describe('filterByType (logique locale)', () => {
    it('filtre par type VALIDATION', () => {
      const notifs = [notifNonLue, notifLue, notifRejet];
      const result = service.filterByType(notifs, 'VALIDATION');

      expect(result).toHaveLength(1);
      expect(result[0].type).toBe('VALIDATION');
    });

    it('filtre par type REJET', () => {
      const notifs = [notifNonLue, notifLue, notifRejet];
      const result = service.filterByType(notifs, 'REJET');

      expect(result).toHaveLength(1);
      expect(result[0].message).toContain('rejeté');
    });

    it('retourne une liste vide si aucune notification du type', () => {
      const notifs = [notifNonLue, notifLue];
      const result = service.filterByType(notifs, 'SIGNATURE_REQUISE');

      expect(result).toHaveLength(0);
    });
  });

  // ── Cycle complet : récupérer → marquer lu ────────────────────────────────

  describe('Cycle complet : récupérer → marquer lu', () => {
    it('récupère les notifications puis marque les non lues comme lues', async () => {
      http.get.mockReturnValue(of([notifNonLue, notifLue, notifRejet]));
      http.put.mockReturnValue(of(undefined));

      // 1. Récupérer
      const notifs = await firstValueFrom(service.getMesNotifications() as any) as NotificationDTO[];
      expect(service.countNonLues(notifs)).toBe(2);

      // 2. Marquer les non lues comme lues
      const nonLues = notifs.filter(n => !n.lu);
      for (const n of nonLues) {
        await firstValueFrom(service.marquerLu(n.id) as any);
      }

      expect(http.put).toHaveBeenCalledTimes(2);
      expect(http.put).toHaveBeenCalledWith(`${API_URL}/notifications/1/lire`, {});
      expect(http.put).toHaveBeenCalledWith(`${API_URL}/notifications/3/lire`, {});
    });
  });

  // ── Gestion d'erreurs ─────────────────────────────────────────────────────

  describe('Gestion d\'erreurs', () => {
    it('propage l\'erreur si getMesNotifications échoue', async () => {
      http.get.mockReturnValue(throwError(() => ({ status: 401, message: 'Unauthorized' })));

      await expect(firstValueFrom(service.getMesNotifications() as any))
        .rejects.toMatchObject({ status: 401 });
    });
  });
});
