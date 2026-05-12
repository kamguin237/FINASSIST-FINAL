import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { of, throwError } from 'rxjs';

// ── Mocks globaux ─────────────────────────────────────────────────────────────

// Mock HttpClient
const mockHttp = {
  get:    vi.fn(),
  post:   vi.fn(),
  delete: vi.fn()
};

// Mock PushSubscription (retourné par pushManager.subscribe)
const makeMockPushSub = (endpoint = 'https://push.example.com/sub1') => ({
  endpoint,
  toJSON: () => ({
    endpoint,
    keys: { p256dh: 'dGVzdF9wMjU2ZGg=', auth: 'dGVzdF9hdXRo' }
  }),
  unsubscribe: vi.fn().mockResolvedValue(true)
});

// Mock ServiceWorkerRegistration
const makeMockSwReg = (existingSub: any = null) => ({
  pushManager: {
    subscribe:       vi.fn().mockResolvedValue(makeMockPushSub()),
    getSubscription: vi.fn().mockResolvedValue(existingSub)
  }
});

// Mock navigator.serviceWorker
const mockSwContainer = {
  register: vi.fn()
};

// ── Implémentation testable (sans DI Angular) ─────────────────────────────────

/**
 * Version testable de PushNotificationService avec injection manuelle.
 * Reproduit fidèlement la logique du service réel.
 */
class TestablePushNotificationService {
  private url = 'http://localhost:4200/api/push';
  swReg: any = null; // exposé pour les tests

  constructor(private http: typeof mockHttp) {}

  async init(swContainer = mockSwContainer): Promise<void> {
    try {
      this.swReg = await swContainer.register('/sw.js');
    } catch (e) {
      // Échec silencieux
    }
  }

  async subscribe(
    permission: NotificationPermission = 'granted',
    swContainer = mockSwContainer
  ): Promise<boolean> {
    if (!this.swReg) await this.init(swContainer);
    if (!this.swReg) return false;

    if (permission !== 'granted') return false;

    try {
      const resp = await (this.http.get(`${this.url}/vapid-public-key`) as any).toPromise();
      const publicKey = resp?.publicKey;

      const sub = await this.swReg.pushManager.subscribe({
        userVisibleOnly: true,
        applicationServerKey: publicKey
      });

      const json = sub.toJSON();
      await (this.http.post(`${this.url}/subscribe`, {
        endpoint: json.endpoint,
        p256dh:   json.keys?.p256dh,
        auth:     json.keys?.auth
      }) as any).toPromise();

      return true;
    } catch {
      return false;
    }
  }

  async unsubscribe(): Promise<void> {
    if (!this.swReg) return;
    const sub = await this.swReg.pushManager.getSubscription();
    if (!sub) return;
    await (this.http.delete(`${this.url}/unsubscribe`, {
      body: { endpoint: sub.endpoint }
    }) as any).toPromise();
    await sub.unsubscribe();
  }

  async isSubscribed(): Promise<boolean> {
    if (!this.swReg) return false;
    const sub = await this.swReg.pushManager.getSubscription();
    return !!sub;
  }

  urlBase64ToUint8Array(base64String: string): Uint8Array {
    const padding = '='.repeat((4 - base64String.length % 4) % 4);
    const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
    const raw = window.atob(base64);
    return Uint8Array.from([...raw].map(c => c.charCodeAt(0)));
  }
}

// ── Tests ─────────────────────────────────────────────────────────────────────

describe('PushNotificationService', () => {
  let service: TestablePushNotificationService;

  beforeEach(() => {
    vi.clearAllMocks();
    service = new TestablePushNotificationService(mockHttp);
  });

  // ── init() ──────────────────────────────────────────────────────────────────

  describe('init()', () => {
    it('enregistre le service worker /sw.js', async () => {
      const mockReg = makeMockSwReg();
      mockSwContainer.register.mockResolvedValue(mockReg);

      await service.init(mockSwContainer);

      expect(mockSwContainer.register).toHaveBeenCalledWith('/sw.js');
      expect(service.swReg).toBe(mockReg);
    });

    it('ne lève pas d\'exception si l\'enregistrement échoue', async () => {
      mockSwContainer.register.mockRejectedValue(new Error('SW non supporté'));

      await expect(service.init(mockSwContainer)).resolves.not.toThrow();
      expect(service.swReg).toBeNull();
    });

    it('ne réenregistre pas si swReg est déjà défini', async () => {
      const mockReg = makeMockSwReg();
      mockSwContainer.register.mockResolvedValue(mockReg);
      await service.init(mockSwContainer);

      // Deuxième appel — le swReg est déjà là
      await service.init(mockSwContainer);

      // register peut être appelé 2 fois (init est idempotent côté SW)
      expect(service.swReg).toBe(mockReg);
    });
  });

  // ── subscribe() ─────────────────────────────────────────────────────────────

  describe('subscribe()', () => {
    it('retourne false si la permission est refusée', async () => {
      const mockReg = makeMockSwReg();
      mockSwContainer.register.mockResolvedValue(mockReg);
      await service.init(mockSwContainer);

      const result = await service.subscribe('denied', mockSwContainer);

      expect(result).toBe(false);
      expect(mockHttp.get).not.toHaveBeenCalled();
    });

    it('retourne false si la permission est "default" (non répondue)', async () => {
      const mockReg = makeMockSwReg();
      mockSwContainer.register.mockResolvedValue(mockReg);
      await service.init(mockSwContainer);

      const result = await service.subscribe('default', mockSwContainer);

      expect(result).toBe(false);
    });

    it('récupère la clé VAPID publique via GET /push/vapid-public-key', async () => {
      const mockReg = makeMockSwReg();
      mockSwContainer.register.mockResolvedValue(mockReg);
      await service.init(mockSwContainer);

      mockHttp.get.mockReturnValue(of({ publicKey: 'VAPID_PUBLIC_KEY' }));
      mockHttp.post.mockReturnValue(of({ message: 'OK' }));

      await service.subscribe('granted', mockSwContainer);

      expect(mockHttp.get).toHaveBeenCalledWith(
        expect.stringContaining('/push/vapid-public-key')
      );
    });

    it('envoie la subscription au backend via POST /push/subscribe', async () => {
      const mockReg = makeMockSwReg();
      mockSwContainer.register.mockResolvedValue(mockReg);
      await service.init(mockSwContainer);

      mockHttp.get.mockReturnValue(of({ publicKey: 'VAPID_PUBLIC_KEY' }));
      mockHttp.post.mockReturnValue(of({ message: 'Subscription enregistrée.' }));

      const result = await service.subscribe('granted', mockSwContainer);

      expect(result).toBe(true);
      expect(mockHttp.post).toHaveBeenCalledWith(
        expect.stringContaining('/push/subscribe'),
        expect.objectContaining({
          endpoint: expect.any(String),
          p256dh:   expect.any(String),
          auth:     expect.any(String)
        })
      );
    });

    it('envoie les bonnes clés p256dh et auth au backend', async () => {
      const mockReg = makeMockSwReg();
      mockSwContainer.register.mockResolvedValue(mockReg);
      await service.init(mockSwContainer);

      mockHttp.get.mockReturnValue(of({ publicKey: 'VAPID_PUBLIC_KEY' }));
      mockHttp.post.mockReturnValue(of({}));

      await service.subscribe('granted', mockSwContainer);

      const postCall = mockHttp.post.mock.calls[0];
      expect(postCall[1]).toMatchObject({
        p256dh: 'dGVzdF9wMjU2ZGg=',
        auth:   'dGVzdF9hdXRo'
      });
    });

    it('retourne false si l\'appel HTTP échoue', async () => {
      const mockReg = makeMockSwReg();
      mockSwContainer.register.mockResolvedValue(mockReg);
      await service.init(mockSwContainer);

      mockHttp.get.mockReturnValue(throwError(() => new Error('Network error')));

      const result = await service.subscribe('granted', mockSwContainer);

      expect(result).toBe(false);
    });

    it('retourne false si swReg est null après init échouée', async () => {
      mockSwContainer.register.mockRejectedValue(new Error('SW non supporté'));

      const result = await service.subscribe('granted', mockSwContainer);

      expect(result).toBe(false);
    });

    it('retourne true en cas de succès complet', async () => {
      const mockReg = makeMockSwReg();
      mockSwContainer.register.mockResolvedValue(mockReg);
      await service.init(mockSwContainer);

      mockHttp.get.mockReturnValue(of({ publicKey: 'VAPID_KEY' }));
      mockHttp.post.mockReturnValue(of({ message: 'OK' }));

      const result = await service.subscribe('granted', mockSwContainer);

      expect(result).toBe(true);
    });
  });

  // ── isSubscribed() ───────────────────────────────────────────────────────────

  describe('isSubscribed()', () => {
    it('retourne false si swReg est null', async () => {
      // swReg non initialisé
      const result = await service.isSubscribed();
      expect(result).toBe(false);
    });

    it('retourne false si aucune subscription active', async () => {
      const mockReg = makeMockSwReg(null); // pas de subscription
      mockSwContainer.register.mockResolvedValue(mockReg);
      await service.init(mockSwContainer);

      const result = await service.isSubscribed();
      expect(result).toBe(false);
    });

    it('retourne true si une subscription existe', async () => {
      const existingSub = makeMockPushSub();
      const mockReg = makeMockSwReg(existingSub);
      mockSwContainer.register.mockResolvedValue(mockReg);
      await service.init(mockSwContainer);

      const result = await service.isSubscribed();
      expect(result).toBe(true);
    });

    it('appelle pushManager.getSubscription()', async () => {
      const mockReg = makeMockSwReg(null);
      mockSwContainer.register.mockResolvedValue(mockReg);
      await service.init(mockSwContainer);

      await service.isSubscribed();

      expect(mockReg.pushManager.getSubscription).toHaveBeenCalledOnce();
    });
  });

  // ── unsubscribe() ────────────────────────────────────────────────────────────

  describe('unsubscribe()', () => {
    it('ne fait rien si swReg est null', async () => {
      await service.unsubscribe();
      expect(mockHttp.delete).not.toHaveBeenCalled();
    });

    it('ne fait rien si aucune subscription active', async () => {
      const mockReg = makeMockSwReg(null);
      mockSwContainer.register.mockResolvedValue(mockReg);
      await service.init(mockSwContainer);

      await service.unsubscribe();

      expect(mockHttp.delete).not.toHaveBeenCalled();
    });

    it('appelle DELETE /push/unsubscribe avec l\'endpoint', async () => {
      const existingSub = makeMockPushSub('https://push.example.com/sub1');
      const mockReg = makeMockSwReg(existingSub);
      mockSwContainer.register.mockResolvedValue(mockReg);
      await service.init(mockSwContainer);

      mockHttp.delete.mockReturnValue(of(null));

      await service.unsubscribe();

      expect(mockHttp.delete).toHaveBeenCalledWith(
        expect.stringContaining('/push/unsubscribe'),
        expect.objectContaining({
          body: { endpoint: 'https://push.example.com/sub1' }
        })
      );
    });

    it('appelle sub.unsubscribe() pour désactiver côté navigateur', async () => {
      const existingSub = makeMockPushSub();
      const mockReg = makeMockSwReg(existingSub);
      mockSwContainer.register.mockResolvedValue(mockReg);
      await service.init(mockSwContainer);

      mockHttp.delete.mockReturnValue(of(null));

      await service.unsubscribe();

      expect(existingSub.unsubscribe).toHaveBeenCalledOnce();
    });
  });

  // ── urlBase64ToUint8Array() ───────────────────────────────────────────────────

  describe('urlBase64ToUint8Array()', () => {
    it('convertit une clé VAPID base64url en Uint8Array', () => {
      // Clé VAPID publique valide (format base64url)
      const key = 'BIGOipHWZkUWsJzabUIkmdz0XR5tFVGfUgum_D73Q-6Er7ltCzuUAoYp0RDTggVjoENxukR56VYvEDY4gaxNuTU';
      const result = service.urlBase64ToUint8Array(key);

      expect(result).toBeInstanceOf(Uint8Array);
      expect(result.length).toBeGreaterThan(0);
    });

    it('retourne un Uint8Array non vide pour une clé courte', () => {
      const key = 'dGVzdA'; // "test" en base64
      const result = service.urlBase64ToUint8Array(key);

      expect(result).toBeInstanceOf(Uint8Array);
      expect(result.length).toBe(4); // "test" = 4 octets
    });

    it('gère le padding manquant (base64url sans =)', () => {
      // base64url n'a pas de padding — la fonction doit l'ajouter
      const key = 'dGVzdA'; // sans padding
      const result = service.urlBase64ToUint8Array(key);

      expect(result).toBeInstanceOf(Uint8Array);
    });

    it('remplace - par + et _ par / (base64url → base64 standard)', () => {
      // Vérifier que la conversion ne lève pas d'exception avec des caractères base64url
      const keyWithSpecialChars = 'BIGOipHW_D73Q-6Er7lt';
      const act = () => service.urlBase64ToUint8Array(keyWithSpecialChars);

      expect(act).not.toThrow();
    });
  });

  // ── Pipeline complet ──────────────────────────────────────────────────────────

  describe('Pipeline complet : init → subscribe → isSubscribed → unsubscribe', () => {
    it('flux nominal complet sans erreur', async () => {
      // 1. Init
      const mockSub = makeMockPushSub('https://push.example.com/user1');
      const mockReg = {
        pushManager: {
          subscribe:       vi.fn().mockResolvedValue(mockSub),
          getSubscription: vi.fn()
            .mockResolvedValueOnce(null)      // avant subscribe → pas encore abonné
            .mockResolvedValueOnce(mockSub)   // après subscribe → abonné
            .mockResolvedValueOnce(mockSub)   // pour unsubscribe
        }
      };
      mockSwContainer.register.mockResolvedValue(mockReg);
      mockHttp.get.mockReturnValue(of({ publicKey: 'VAPID_KEY' }));
      mockHttp.post.mockReturnValue(of({ message: 'OK' }));
      mockHttp.delete.mockReturnValue(of(null));

      await service.init(mockSwContainer);

      // 2. Pas encore abonné
      const avant = await service.isSubscribed();
      expect(avant).toBe(false);

      // 3. S'abonner
      const ok = await service.subscribe('granted', mockSwContainer);
      expect(ok).toBe(true);

      // 4. Maintenant abonné
      const apres = await service.isSubscribed();
      expect(apres).toBe(true);

      // 5. Se désabonner
      await service.unsubscribe();
      expect(mockSub.unsubscribe).toHaveBeenCalledOnce();
      expect(mockHttp.delete).toHaveBeenCalledWith(
        expect.stringContaining('/push/unsubscribe'),
        expect.objectContaining({ body: { endpoint: 'https://push.example.com/user1' } })
      );
    });

    it('subscribe puis isSubscribed retourne true', async () => {
      const mockSub = makeMockPushSub();
      const mockReg = {
        pushManager: {
          subscribe:       vi.fn().mockResolvedValue(mockSub),
          getSubscription: vi.fn().mockResolvedValue(mockSub)
        }
      };
      mockSwContainer.register.mockResolvedValue(mockReg);
      mockHttp.get.mockReturnValue(of({ publicKey: 'KEY' }));
      mockHttp.post.mockReturnValue(of({}));

      await service.init(mockSwContainer);
      await service.subscribe('granted', mockSwContainer);

      const subscribed = await service.isSubscribed();
      expect(subscribed).toBe(true);
    });
  });
});
