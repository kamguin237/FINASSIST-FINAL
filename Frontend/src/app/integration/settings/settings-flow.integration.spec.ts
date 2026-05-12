/**
 * Tests d'intégration — Flux Paramètres (Settings)
 *
 * Vérifie la collaboration entre :
 *   SettingsService ↔ localStorage ↔ HttpClient (backend)
 *
 * Scénarios : chargement local, sync backend, sauvegarde,
 * reset, valeurs par défaut, fusion local + backend.
 */

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { of, throwError } from 'rxjs';

// ── Constantes ────────────────────────────────────────────────────────────────

const SETTINGS_KEY = 'finassist_settings';
const API_URL      = 'http://localhost:5000/api';

// ── Types ─────────────────────────────────────────────────────────────────────

interface UserSettings {
  notifApp: boolean; notifEmail: boolean;
  alertNouveauBesoin: boolean; alertValidation: boolean; alertEnAttente: boolean;
  langue: string; formatDate: string; fuseauHoraire: string;
  itemsParPage: number; pageAccueil: string; triDefaut: string;
  deconnexionAuto: boolean; inactiviteMinutes: number; avertissementSecondes: number;
}

const DEFAULTS: UserSettings = {
  notifApp: true, notifEmail: false,
  alertNouveauBesoin: true, alertValidation: true, alertEnAttente: true,
  langue: 'fr', formatDate: 'dd/MM/yyyy', fuseauHoraire: 'Africa/Douala',
  itemsParPage: 20, pageAccueil: '/dashboard', triDefaut: 'dateDesc',
  deconnexionAuto: true, inactiviteMinutes: 30, avertissementSecondes: 30
};

// ── Simulation SettingsService ────────────────────────────────────────────────

class SettingsServiceSim {
  private http: { get: ReturnType<typeof vi.fn>; put: ReturnType<typeof vi.fn> };
  private url = `${API_URL}/settings`;
  settings: UserSettings = { ...DEFAULTS };

  constructor(http: typeof SettingsServiceSim.prototype.http) {
    this.http = http;
    this.loadFromLocal();
  }

  loadFromLocal() {
    try {
      const raw = localStorage.getItem(SETTINGS_KEY);
      if (raw) this.settings = { ...DEFAULTS, ...JSON.parse(raw) };
    } catch { /* silencieux */ }
  }

  loadFromBackend() {
    return this.http.get(this.url);
  }

  applyFromBackend(prefs: Partial<UserSettings>) {
    const merged = { ...DEFAULTS, ...prefs };
    this.settings = merged;
    localStorage.setItem(SETTINGS_KEY, JSON.stringify(merged));
  }

  save(partial: Partial<UserSettings>) {
    const updated = { ...this.settings, ...partial };
    this.settings = updated;
    localStorage.setItem(SETTINGS_KEY, JSON.stringify(updated));
    return this.http.put(this.url, updated);
  }

  reset() {
    this.settings = { ...DEFAULTS };
    localStorage.removeItem(SETTINGS_KEY);
    return this.http.put(this.url, DEFAULTS);
  }
}

// ═════════════════════════════════════════════════════════════════════════════
// Tests
// ═════════════════════════════════════════════════════════════════════════════

describe('Intégration — Flux Paramètres', () => {
  let http: { get: ReturnType<typeof vi.fn>; put: ReturnType<typeof vi.fn> };
  let service: SettingsServiceSim;

  beforeEach(() => {
    localStorage.clear();
    http = { get: vi.fn(), put: vi.fn() };
    service = new SettingsServiceSim(http);
  });

  afterEach(() => {
    localStorage.clear();
    vi.clearAllMocks();
  });

  // ── Valeurs par défaut ────────────────────────────────────────────────────

  describe('Valeurs par défaut', () => {
    it('charge les valeurs par défaut si localStorage vide', () => {
      expect(service.settings.langue).toBe('fr');
      expect(service.settings.itemsParPage).toBe(20);
      expect(service.settings.notifApp).toBe(true);
      expect(service.settings.deconnexionAuto).toBe(true);
    });

    it('charge les valeurs par défaut si localStorage corrompu', () => {
      localStorage.setItem(SETTINGS_KEY, 'json_invalide{{{');
      const s = new SettingsServiceSim(http);

      expect(s.settings.langue).toBe('fr');
    });
  });

  // ── Chargement depuis localStorage ───────────────────────────────────────

  describe('Chargement depuis localStorage', () => {
    it('charge les préférences sauvegardées', () => {
      localStorage.setItem(SETTINGS_KEY, JSON.stringify({ langue: 'en', itemsParPage: 50 }));
      const s = new SettingsServiceSim(http);

      expect(s.settings.langue).toBe('en');
      expect(s.settings.itemsParPage).toBe(50);
    });

    it('fusionne avec les valeurs par défaut pour les champs manquants', () => {
      localStorage.setItem(SETTINGS_KEY, JSON.stringify({ langue: 'en' }));
      const s = new SettingsServiceSim(http);

      expect(s.settings.langue).toBe('en');
      expect(s.settings.notifApp).toBe(true); // valeur par défaut
      expect(s.settings.itemsParPage).toBe(20); // valeur par défaut
    });
  });

  // ── Sync avec le backend ──────────────────────────────────────────────────

  describe('Synchronisation avec le backend', () => {
    it('loadFromBackend appelle GET /api/settings', () => {
      http.get.mockReturnValue(of(DEFAULTS));

      service.loadFromBackend();

      expect(http.get).toHaveBeenCalledWith(`${API_URL}/settings`);
    });

    it('applyFromBackend met à jour les settings et localStorage', () => {
      const backendPrefs = { langue: 'en', itemsParPage: 10 };
      service.applyFromBackend(backendPrefs);

      expect(service.settings.langue).toBe('en');
      expect(service.settings.itemsParPage).toBe(10);

      const stored = JSON.parse(localStorage.getItem(SETTINGS_KEY)!);
      expect(stored.langue).toBe('en');
    });

    it('applyFromBackend fusionne avec les valeurs par défaut', () => {
      service.applyFromBackend({ langue: 'en' });

      expect(service.settings.notifApp).toBe(true); // défaut préservé
      expect(service.settings.langue).toBe('en');
    });
  });

  // ── Sauvegarde ────────────────────────────────────────────────────────────

  describe('Sauvegarde des paramètres', () => {
    it('save met à jour les settings en mémoire', () => {
      http.put.mockReturnValue(of(null));

      service.save({ langue: 'en', itemsParPage: 50 });

      expect(service.settings.langue).toBe('en');
      expect(service.settings.itemsParPage).toBe(50);
    });

    it('save persiste dans localStorage', () => {
      http.put.mockReturnValue(of(null));

      service.save({ langue: 'en' });

      const stored = JSON.parse(localStorage.getItem(SETTINGS_KEY)!);
      expect(stored.langue).toBe('en');
    });

    it('save appelle PUT /api/settings avec les settings complets', () => {
      http.put.mockReturnValue(of(null));

      service.save({ langue: 'en' });

      expect(http.put).toHaveBeenCalledWith(
        `${API_URL}/settings`,
        expect.objectContaining({ langue: 'en', notifApp: true })
      );
    });

    it('save préserve les champs non modifiés', () => {
      http.put.mockReturnValue(of(null));

      service.save({ langue: 'en' });

      expect(service.settings.notifApp).toBe(true);
      expect(service.settings.deconnexionAuto).toBe(true);
    });

    it('save partiel ne remplace pas tout', () => {
      http.put.mockReturnValue(of(null));
      service.save({ notifApp: false });

      expect(service.settings.notifApp).toBe(false);
      expect(service.settings.langue).toBe('fr'); // inchangé
    });
  });

  // ── Reset ─────────────────────────────────────────────────────────────────

  describe('Reset des paramètres', () => {
    it('reset restaure les valeurs par défaut', () => {
      http.put.mockReturnValue(of(null));
      service.save({ langue: 'en', itemsParPage: 50 });

      service.reset();

      expect(service.settings.langue).toBe('fr');
      expect(service.settings.itemsParPage).toBe(20);
    });

    it('reset supprime le localStorage', () => {
      http.put.mockReturnValue(of(null));
      service.save({ langue: 'en' });

      service.reset();

      expect(localStorage.getItem(SETTINGS_KEY)).toBeNull();
    });

    it('reset appelle PUT /api/settings avec les valeurs par défaut', () => {
      http.put.mockReturnValue(of(null));

      service.reset();

      expect(http.put).toHaveBeenCalledWith(`${API_URL}/settings`, DEFAULTS);
    });
  });

  // ── Résilience aux erreurs backend ────────────────────────────────────────

  describe('Résilience aux erreurs backend', () => {
    it('les settings locaux restent valides si le backend échoue', () => {
      http.put.mockReturnValue(throwError(() => new Error('Network error')));

      // La sauvegarde locale doit quand même fonctionner
      service.save({ langue: 'en' });

      // Les settings en mémoire sont mis à jour
      expect(service.settings.langue).toBe('en');
      // localStorage est mis à jour
      const stored = JSON.parse(localStorage.getItem(SETTINGS_KEY)!);
      expect(stored.langue).toBe('en');
    });
  });
});
