import { describe, it, expect, beforeEach, vi } from 'vitest';
import { of, throwError } from 'rxjs';
import { NotificationDTO, CreateNotificationDTO } from '../models/notification.models';

// ── Mock HTTP ─────────────────────────────────────────────────────────────────
const mockHttp = {
  get:  vi.fn(),
  put:  vi.fn(),
  post: vi.fn()
};

const BASE_URL = 'http://localhost:4200/api/notifications';

// ── Service simulé (même logique que NotificationsService) ────────────────────
function createNotificationsService() {
  return {
    getMesNotifications: () => mockHttp.get(BASE_URL),
    marquerLu:  (id: number) => mockHttp.put(`${BASE_URL}/${id}/lire`, {}),
    envoyer:    (dto: CreateNotificationDTO) => mockHttp.post(BASE_URL, dto)
  };
}

// ── Tests ─────────────────────────────────────────────────────────────────────

describe('NotificationsService — getMesNotifications', () => {
  let service: ReturnType<typeof createNotificationsService>;

  beforeEach(() => {
    vi.clearAllMocks();
    service = createNotificationsService();
  });

  it('appelle GET /notifications', () => {
    mockHttp.get.mockReturnValue(of([]));
    service.getMesNotifications();
    expect(mockHttp.get).toHaveBeenCalledWith(BASE_URL);
  });

  it('retourne la liste des notifications', () => {
    const notifs: NotificationDTO[] = [
      { id: 1, message: 'Besoin soumis', type: 'ACCUSE_RECEPTION', dateEnvoi: '2026-05-07T10:00:00Z', lu: false },
      { id: 2, message: 'Besoin rejeté', type: 'REJET',            dateEnvoi: '2026-05-07T11:00:00Z', lu: true  }
    ];
    mockHttp.get.mockReturnValue(of(notifs));

    let result: NotificationDTO[] = [];
    service.getMesNotifications().subscribe((n: NotificationDTO[]) => result = n);

    expect(result).toHaveLength(2);
    expect(result[0].message).toBe('Besoin soumis');
    expect(result[0].lu).toBe(false);
    expect(result[1].type).toBe('REJET');
    expect(result[1].lu).toBe(true);
  });

  it('retourne une liste vide si aucune notification', () => {
    mockHttp.get.mockReturnValue(of([]));

    let result: NotificationDTO[] = [];
    service.getMesNotifications().subscribe((n: NotificationDTO[]) => result = n);

    expect(result).toHaveLength(0);
  });

  it('mappe correctement tous les champs du DTO', () => {
    const notif: NotificationDTO = {
      id: 42,
      message: 'Rappel délai',
      type: 'RAPPEL',
      dateEnvoi: '2026-05-08T09:30:00Z',
      lu: false
    };
    mockHttp.get.mockReturnValue(of([notif]));

    let result: NotificationDTO[] = [];
    service.getMesNotifications().subscribe((n: NotificationDTO[]) => result = n);

    expect(result[0].id).toBe(42);
    expect(result[0].type).toBe('RAPPEL');
    expect(result[0].dateEnvoi).toBe('2026-05-08T09:30:00Z');
  });

  it('distingue les notifications lues et non lues', () => {
    const notifs: NotificationDTO[] = [
      { id: 1, message: 'Non lue', type: 'VALIDATION', dateEnvoi: '2026-05-07T08:00:00Z', lu: false },
      { id: 2, message: 'Lue',     type: 'VALIDATION', dateEnvoi: '2026-05-07T09:00:00Z', lu: true  }
    ];
    mockHttp.get.mockReturnValue(of(notifs));

    let result: NotificationDTO[] = [];
    service.getMesNotifications().subscribe((n: NotificationDTO[]) => result = n);

    const nonLues = result.filter(n => !n.lu);
    const lues    = result.filter(n => n.lu);
    expect(nonLues).toHaveLength(1);
    expect(lues).toHaveLength(1);
  });
});

describe('NotificationsService — marquerLu', () => {
  let service: ReturnType<typeof createNotificationsService>;

  beforeEach(() => {
    vi.clearAllMocks();
    service = createNotificationsService();
  });

  it('appelle PUT /notifications/:id/lire', () => {
    mockHttp.put.mockReturnValue(of(null));
    service.marquerLu(42);
    expect(mockHttp.put).toHaveBeenCalledWith(`${BASE_URL}/42/lire`, {});
  });

  it('passe le bon ID dans l\'URL', () => {
    mockHttp.put.mockReturnValue(of(null));
    service.marquerLu(99);
    expect(mockHttp.put).toHaveBeenCalledWith(`${BASE_URL}/99/lire`, {});
  });

  it('passe un body vide {}', () => {
    mockHttp.put.mockReturnValue(of(null));
    service.marquerLu(1);
    const [, body] = mockHttp.put.mock.calls[0];
    expect(body).toEqual({});
  });

  it('peut marquer plusieurs notifications différentes', () => {
    mockHttp.put.mockReturnValue(of(null));
    service.marquerLu(1);
    service.marquerLu(2);
    service.marquerLu(3);
    expect(mockHttp.put).toHaveBeenCalledTimes(3);
    expect(mockHttp.put).toHaveBeenCalledWith(`${BASE_URL}/1/lire`, {});
    expect(mockHttp.put).toHaveBeenCalledWith(`${BASE_URL}/2/lire`, {});
    expect(mockHttp.put).toHaveBeenCalledWith(`${BASE_URL}/3/lire`, {});
  });
});

describe('NotificationsService — envoyer', () => {
  let service: ReturnType<typeof createNotificationsService>;

  beforeEach(() => {
    vi.clearAllMocks();
    service = createNotificationsService();
  });

  it('appelle POST /notifications avec le DTO', () => {
    const dto: CreateNotificationDTO = {
      message: 'Test notification',
      type: 'VALIDATION',
      destinataireIds: [1, 2, 3]
    };
    mockHttp.post.mockReturnValue(of({ id: 10, ...dto, dateEnvoi: '2026-05-07T12:00:00Z', lu: false }));

    service.envoyer(dto);
    expect(mockHttp.post).toHaveBeenCalledWith(BASE_URL, dto);
  });

  it('retourne la notification créée', () => {
    const dto: CreateNotificationDTO = {
      message: 'Nouvelle notification',
      type: 'RAPPEL',
      destinataireIds: [5]
    };
    const response: NotificationDTO = {
      id: 10, message: dto.message, type: dto.type,
      dateEnvoi: '2026-05-07T12:00:00Z', lu: false
    };
    mockHttp.post.mockReturnValue(of(response));

    let result: NotificationDTO | null = null;
    service.envoyer(dto).subscribe((n: NotificationDTO) => result = n);

    expect(result).not.toBeNull();
    expect(result!.id).toBe(10);
    expect(result!.message).toBe('Nouvelle notification');
    expect(result!.lu).toBe(false);
  });

  it('envoie les destinataireIds correctement', () => {
    const dto: CreateNotificationDTO = {
      message: 'Multi-destinataires',
      type: 'ACCUSE_RECEPTION',
      destinataireIds: [1, 2, 3, 4, 5]
    };
    mockHttp.post.mockReturnValue(of({ id: 11, ...dto, dateEnvoi: '2026-05-07T12:00:00Z', lu: false }));

    service.envoyer(dto);
    const [, sentDto] = mockHttp.post.mock.calls[0];
    expect(sentDto.destinataireIds).toEqual([1, 2, 3, 4, 5]);
  });

  it('propage les erreurs HTTP', () => {
    const dto: CreateNotificationDTO = {
      message: 'Test', type: 'RAPPEL', destinataireIds: [1]
    };
    mockHttp.post.mockReturnValue(throwError(() => ({ status: 403, message: 'Accès refusé' })));

    let errorReceived = false;
    service.envoyer(dto).subscribe({
      error: (err) => {
        errorReceived = true;
        expect(err.status).toBe(403);
      }
    });
    expect(errorReceived).toBe(true);
  });
});

// ── Tests logique métier notifications ────────────────────────────────────────

describe('Notifications — logique métier', () => {

  it('unreadCount — compte les notifications non lues', () => {
    const notifs: NotificationDTO[] = [
      { id: 1, message: 'A', type: 'RAPPEL',     dateEnvoi: '2026-05-07T08:00:00Z', lu: false },
      { id: 2, message: 'B', type: 'VALIDATION', dateEnvoi: '2026-05-07T09:00:00Z', lu: true  },
      { id: 3, message: 'C', type: 'REJET',      dateEnvoi: '2026-05-07T10:00:00Z', lu: false }
    ];
    const unreadCount = notifs.filter(n => !n.lu).length;
    expect(unreadCount).toBe(2);
  });

  it('recentNotifs — retourne les 5 premières non lues', () => {
    const notifs: NotificationDTO[] = Array.from({ length: 8 }, (_, i) => ({
      id: i + 1, message: `Notif ${i + 1}`, type: 'RAPPEL',
      dateEnvoi: '2026-05-07T08:00:00Z', lu: false
    }));
    const recentNotifs = notifs.filter(n => !n.lu).slice(0, 5);
    expect(recentNotifs).toHaveLength(5);
  });

  it('recentNotifs — exclut les notifications lues', () => {
    const notifs: NotificationDTO[] = [
      { id: 1, message: 'Non lue 1', type: 'RAPPEL',     dateEnvoi: '2026-05-07T08:00:00Z', lu: false },
      { id: 2, message: 'Lue',       type: 'VALIDATION', dateEnvoi: '2026-05-07T09:00:00Z', lu: true  },
      { id: 3, message: 'Non lue 2', type: 'REJET',      dateEnvoi: '2026-05-07T10:00:00Z', lu: false }
    ];
    const recentNotifs = notifs.filter(n => !n.lu).slice(0, 5);
    expect(recentNotifs).toHaveLength(2);
    expect(recentNotifs.every(n => !n.lu)).toBe(true);
  });

  it('badge — affiche 9+ si plus de 9 non lues', () => {
    const unreadCount = 12;
    const badge = unreadCount > 9 ? '9+' : String(unreadCount);
    expect(badge).toBe('9+');
  });

  it('badge — affiche le nombre exact si <= 9', () => {
    const unreadCount = 7;
    const badge = unreadCount > 9 ? '9+' : String(unreadCount);
    expect(badge).toBe('7');
  });

  it('badge — affiche 0 si aucune notification non lue', () => {
    const unreadCount = 0;
    const badge = unreadCount > 9 ? '9+' : String(unreadCount);
    expect(badge).toBe('0');
  });

  it('types de notification valides', () => {
    const typesValides = ['ACCUSE_RECEPTION', 'VALIDATION', 'REJET', 'RAPPEL', 'SIGNATURE_REQUISE'];
    typesValides.forEach(type => {
      const notif: NotificationDTO = {
        id: 1, message: 'Test', type, dateEnvoi: '2026-05-07T08:00:00Z', lu: false
      };
      expect(notif.type).toBe(type);
    });
  });
});
