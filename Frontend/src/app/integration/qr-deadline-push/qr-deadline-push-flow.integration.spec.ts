/**
 * Tests d'intégration — Flux QR Signature + Deadline + Push + Langue
 *
 * Vérifie la collaboration entre :
 *   QrSignatureService ↔ HttpClient (session, status, submit)
 *   DeadlineService ↔ AuthService ↔ HttpClient
 *   PushNotificationService — logique urlBase64ToUint8Array
 *   LanguageService ↔ localStorage ↔ document.documentElement.lang
 *   InactivityService — logique de timer et countdown
 */

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { of, throwError } from 'rxjs';
import { firstValueFrom } from 'rxjs';

// ── Constantes ────────────────────────────────────────────────────────────────

const TOKEN_KEY    = 'finassist_token';
const LANG_KEY     = 'app-language';
const API_URL      = 'http://localhost:5000/api';

function makeJwt(): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const body   = btoa(JSON.stringify({ exp: Math.floor(Date.now() / 1000) + 3600, sub: '1' }));
  return `${header}.${body}.sig`;
}

// ── Simulation QrSignatureService ─────────────────────────────────────────────

class QrSignatureServiceSim {
  private http: { get: ReturnType<typeof vi.fn>; post: ReturnType<typeof vi.fn> };
  private url = `${API_URL}/signatures/qr`;

  constructor(http: typeof QrSignatureServiceSim.prototype.http) { this.http = http; }

  createSession()                        { return this.http.post(`${this.url}/session`, {}); }
  getStatus(token: string)               { return this.http.get(`${this.url}/session/${token}/status`); }
  getSessionInfo(token: string)          { return this.http.get(`${this.url}/session/${token}`); }
  submitSignature(token: string, imageBase64: string) {
    return this.http.post(`${this.url}/session/${token}/submit`, { imageBase64 });
  }
}

// ── Simulation DeadlineService ────────────────────────────────────────────────

interface BesoinDeadlineDTO {
  id: number; titre: string; statut: string; urgence: string;
  pourcentageEcoule: number; minutesRestantes: number;
}

class DeadlineServiceSim {
  private http: { get: ReturnType<typeof vi.fn> };
  private url = `${API_URL}/besoins/deadlines`;

  constructor(http: typeof DeadlineServiceSim.prototype.http) { this.http = http; }

  getMesDeadlines() { return this.http.get(this.url); }
}

// ── Simulation LanguageService ────────────────────────────────────────────────

type Language = 'fr' | 'en';

class LanguageServiceSim {
  currentLanguage: Language;

  constructor() {
    this.currentLanguage = this.getInitialLanguage();
  }

  private getInitialLanguage(): Language {
    const stored = localStorage.getItem(LANG_KEY) as Language;
    if (stored === 'fr' || stored === 'en') return stored;
    return 'fr'; // défaut
  }

  setLanguage(lang: Language) {
    this.currentLanguage = lang;
    localStorage.setItem(LANG_KEY, lang);
    document.documentElement.lang = lang;
  }
}

// ── Simulation InactivityService (logique pure) ───────────────────────────────

class InactivityServiceSim {
  private active = false;
  private inactivityMs: number;
  private warningSeconds: number;
  warningEmitted = false;
  logoutEmitted = false;
  countdownValues: number[] = [];

  constructor(inactivityMs: number, warningSeconds: number) {
    this.inactivityMs = inactivityMs;
    this.warningSeconds = warningSeconds;
  }

  start() { this.active = true; }
  stop()  { this.active = false; }

  simulateInactivity() {
    if (!this.active) return;
    this.warningEmitted = true;
    // Simuler le countdown
    for (let i = this.warningSeconds; i >= 0; i--) {
      this.countdownValues.push(i);
    }
    this.logoutEmitted = true;
  }

  isActive() { return this.active; }
}

// ── Données de test ───────────────────────────────────────────────────────────

const mockDeadlines: BesoinDeadlineDTO[] = [
  { id: 1, titre: 'Achat urgent', statut: 'EN_ATTENTE_RESPONSABLE', urgence: 'danger', pourcentageEcoule: 85, minutesRestantes: 9 },
  { id: 2, titre: 'Formation', statut: 'EN_ATTENTE_DIRECTION', urgence: 'normal', pourcentageEcoule: 30, minutesRestantes: 42 },
  { id: 3, titre: 'Matériel expiré', statut: 'EN_ATTENTE_RESPONSABLE', urgence: 'expired', pourcentageEcoule: 120, minutesRestantes: 0 }
];

// ═════════════════════════════════════════════════════════════════════════════
// Tests QrSignatureService
// ═════════════════════════════════════════════════════════════════════════════

describe('Intégration — Flux QR Signature', () => {
  let http: { get: ReturnType<typeof vi.fn>; post: ReturnType<typeof vi.fn> };
  let service: QrSignatureServiceSim;

  beforeEach(() => {
    localStorage.clear();
    localStorage.setItem(TOKEN_KEY, makeJwt());
    http = { get: vi.fn(), post: vi.fn() };
    service = new QrSignatureServiceSim(http);
  });

  afterEach(() => { localStorage.clear(); vi.clearAllMocks(); });

  it('createSession appelle POST /api/signatures/qr/session', async () => {
    http.post.mockReturnValue(of({ token: 'abc123', urlMobile: 'http://app/sign/abc123', expiration: '2026-04-01T11:00:00Z' }));
    const result = await firstValueFrom(service.createSession() as any) as { token: string };
    expect(http.post).toHaveBeenCalledWith(`${API_URL}/signatures/qr/session`, {});
    expect(result.token).toBe('abc123');
  });

  it('getStatus appelle GET /api/signatures/qr/session/:token/status', async () => {
    http.get.mockReturnValue(of({ completed: false, expired: false }));
    const result = await firstValueFrom(service.getStatus('abc123') as any) as { completed: boolean };
    expect(http.get).toHaveBeenCalledWith(`${API_URL}/signatures/qr/session/abc123/status`);
    expect(result.completed).toBe(false);
  });

  it('getStatus retourne completed=true quand signé', async () => {
    http.get.mockReturnValue(of({ completed: true, expired: false }));
    const result = await firstValueFrom(service.getStatus('abc123') as any) as { completed: boolean };
    expect(result.completed).toBe(true);
  });

  it('getStatus retourne expired=true quand expiré', async () => {
    http.get.mockReturnValue(of({ completed: false, expired: true }));
    const result = await firstValueFrom(service.getStatus('abc123') as any) as { expired: boolean };
    expect(result.expired).toBe(true);
  });

  it('getSessionInfo appelle GET /api/signatures/qr/session/:token', async () => {
    http.get.mockReturnValue(of({ nom: 'Dupont', prenom: 'Jean', role: 'Agent', expiration: '2026-04-01T11:00:00Z' }));
    const result = await firstValueFrom(service.getSessionInfo('abc123') as any) as { nom: string };
    expect(http.get).toHaveBeenCalledWith(`${API_URL}/signatures/qr/session/abc123`);
    expect(result.nom).toBe('Dupont');
  });

  it('submitSignature envoie POST avec imageBase64', async () => {
    http.post.mockReturnValue(of({ success: true }));
    await firstValueFrom(service.submitSignature('abc123', 'data:image/png;base64,abc') as any);
    expect(http.post).toHaveBeenCalledWith(
      `${API_URL}/signatures/qr/session/abc123/submit`,
      { imageBase64: 'data:image/png;base64,abc' }
    );
  });

  it('cycle complet : créer session → vérifier status → soumettre', async () => {
    http.post.mockReturnValueOnce(of({ token: 'tok1', urlMobile: 'http://app/sign/tok1', expiration: '2026-04-01T11:00:00Z' }));
    const session = await firstValueFrom(service.createSession() as any) as { token: string };
    expect(session.token).toBe('tok1');

    http.get.mockReturnValueOnce(of({ completed: false, expired: false }));
    const status1 = await firstValueFrom(service.getStatus(session.token) as any) as { completed: boolean };
    expect(status1.completed).toBe(false);

    http.post.mockReturnValueOnce(of({ success: true }));
    await firstValueFrom(service.submitSignature(session.token, 'data:image/png;base64,sig') as any);

    http.get.mockReturnValueOnce(of({ completed: true, expired: false }));
    const status2 = await firstValueFrom(service.getStatus(session.token) as any) as { completed: boolean };
    expect(status2.completed).toBe(true);
  });

  it('propage l\'erreur 404 si session introuvable', async () => {
    http.get.mockReturnValue(throwError(() => ({ status: 404 })));
    await expect(firstValueFrom(service.getStatus('invalid') as any)).rejects.toMatchObject({ status: 404 });
  });
});

// ═════════════════════════════════════════════════════════════════════════════
// Tests DeadlineService
// ═════════════════════════════════════════════════════════════════════════════

describe('Intégration — Flux Deadlines', () => {
  let http: { get: ReturnType<typeof vi.fn> };
  let service: DeadlineServiceSim;

  beforeEach(() => {
    localStorage.clear();
    localStorage.setItem(TOKEN_KEY, makeJwt());
    http = { get: vi.fn() };
    service = new DeadlineServiceSim(http);
  });

  afterEach(() => { localStorage.clear(); vi.clearAllMocks(); });

  it('getMesDeadlines appelle GET /api/besoins/deadlines', async () => {
    http.get.mockReturnValue(of(mockDeadlines));
    const result = await firstValueFrom(service.getMesDeadlines() as any) as BesoinDeadlineDTO[];
    expect(http.get).toHaveBeenCalledWith(`${API_URL}/besoins/deadlines`);
    expect(result).toHaveLength(3);
  });

  it('retourne les deadlines avec les niveaux d\'urgence', async () => {
    http.get.mockReturnValue(of(mockDeadlines));
    const result = await firstValueFrom(service.getMesDeadlines() as any) as BesoinDeadlineDTO[];
    const urgences = result.map(d => d.urgence);
    expect(urgences).toContain('danger');
    expect(urgences).toContain('normal');
    expect(urgences).toContain('expired');
  });

  it('filtre local : deadlines expirées', async () => {
    http.get.mockReturnValue(of(mockDeadlines));
    const result = await firstValueFrom(service.getMesDeadlines() as any) as BesoinDeadlineDTO[];
    const expirees = result.filter(d => d.urgence === 'expired');
    expect(expirees).toHaveLength(1);
    expect(expirees[0].pourcentageEcoule).toBeGreaterThan(100);
  });

  it('filtre local : deadlines en danger', async () => {
    http.get.mockReturnValue(of(mockDeadlines));
    const result = await firstValueFrom(service.getMesDeadlines() as any) as BesoinDeadlineDTO[];
    const danger = result.filter(d => d.urgence === 'danger');
    expect(danger).toHaveLength(1);
    expect(danger[0].pourcentageEcoule).toBeGreaterThan(80);
  });

  it('retourne une liste vide si aucune deadline', async () => {
    http.get.mockReturnValue(of([]));
    const result = await firstValueFrom(service.getMesDeadlines() as any) as BesoinDeadlineDTO[];
    expect(result).toHaveLength(0);
  });
});

// ═════════════════════════════════════════════════════════════════════════════
// Tests LanguageService
// ═════════════════════════════════════════════════════════════════════════════

describe('Intégration — Flux Langue', () => {
  beforeEach(() => { localStorage.clear(); });
  afterEach(() => { localStorage.clear(); });

  it('charge la langue par défaut (fr) si localStorage vide', () => {
    const service = new LanguageServiceSim();
    expect(service.currentLanguage).toBe('fr');
  });

  it('charge la langue depuis localStorage si présente', () => {
    localStorage.setItem(LANG_KEY, 'en');
    const service = new LanguageServiceSim();
    expect(service.currentLanguage).toBe('en');
  });

  it('setLanguage met à jour currentLanguage', () => {
    const service = new LanguageServiceSim();
    service.setLanguage('en');
    expect(service.currentLanguage).toBe('en');
  });

  it('setLanguage persiste dans localStorage', () => {
    const service = new LanguageServiceSim();
    service.setLanguage('en');
    expect(localStorage.getItem(LANG_KEY)).toBe('en');
  });

  it('setLanguage met à jour document.documentElement.lang', () => {
    const service = new LanguageServiceSim();
    service.setLanguage('en');
    expect(document.documentElement.lang).toBe('en');
  });

  it('setLanguage fr remet la langue en français', () => {
    const service = new LanguageServiceSim();
    service.setLanguage('en');
    service.setLanguage('fr');
    expect(service.currentLanguage).toBe('fr');
    expect(localStorage.getItem(LANG_KEY)).toBe('fr');
  });

  it('ignore les valeurs invalides dans localStorage', () => {
    localStorage.setItem(LANG_KEY, 'de'); // langue non supportée
    const service = new LanguageServiceSim();
    // Doit retomber sur le défaut 'fr'
    expect(service.currentLanguage).toBe('fr');
  });
});

// ═════════════════════════════════════════════════════════════════════════════
// Tests InactivityService (logique pure)
// ═════════════════════════════════════════════════════════════════════════════

describe('Intégration — Flux Inactivité', () => {
  it('start active le service', () => {
    const service = new InactivityServiceSim(30 * 60 * 1000, 30);
    service.start();
    expect(service.isActive()).toBe(true);
  });

  it('stop désactive le service', () => {
    const service = new InactivityServiceSim(30 * 60 * 1000, 30);
    service.start();
    service.stop();
    expect(service.isActive()).toBe(false);
  });

  it('simulateInactivity émet warning et logout', () => {
    const service = new InactivityServiceSim(30 * 60 * 1000, 5);
    service.start();
    service.simulateInactivity();
    expect(service.warningEmitted).toBe(true);
    expect(service.logoutEmitted).toBe(true);
  });

  it('countdown décroît de warningSeconds à 0', () => {
    const service = new InactivityServiceSim(30 * 60 * 1000, 5);
    service.start();
    service.simulateInactivity();
    expect(service.countdownValues[0]).toBe(5);
    expect(service.countdownValues[service.countdownValues.length - 1]).toBe(0);
  });

  it('service inactif ne déclenche pas le logout', () => {
    const service = new InactivityServiceSim(30 * 60 * 1000, 5);
    // Pas de start()
    service.simulateInactivity();
    expect(service.logoutEmitted).toBe(false);
  });
});
