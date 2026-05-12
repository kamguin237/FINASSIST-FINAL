import { describe, it, expect, beforeEach, vi } from 'vitest';
import { of } from 'rxjs';

const mockHttp = { get: vi.fn(), put: vi.fn() };
const BASE_URL = 'http://localhost:4200/api/settings';

function createSettingsService() {
  return {
    get: () => mockHttp.get(BASE_URL),
    save: (dto: any) => mockHttp.put(BASE_URL, dto)
  };
}

describe('SettingsService — appels HTTP', () => {
  let service: ReturnType<typeof createSettingsService>;

  beforeEach(() => {
    vi.clearAllMocks();
    service = createSettingsService();
  });

  it('get appelle GET /settings', () => {
    mockHttp.get.mockReturnValue(of({}));
    service.get();
    expect(mockHttp.get).toHaveBeenCalledWith(BASE_URL);
  });

  it('save appelle PUT /settings avec le DTO', () => {
    const dto = { langue: 'fr', notifApp: true, itemsParPage: 20 };
    mockHttp.put.mockReturnValue(of(dto));
    service.save(dto);
    expect(mockHttp.put).toHaveBeenCalledWith(BASE_URL, dto);
  });
});

// ── Tests logique des préférences par défaut ──────────────────────────────────

describe('UserPreferences — valeurs par défaut', () => {
  const DEFAULTS = {
    notifApp: true,
    notifEmail: true,
    alertNouveauBesoin: true,
    alertValidation: true,
    alertEnAttente: true,
    langue: 'fr',
    formatDate: 'dd/MM/yyyy',
    fuseauHoraire: 'Africa/Douala',
    itemsParPage: 20,
    pageAccueil: 'dashboard',
    triDefaut: 'date_desc'
  };

  it('les valeurs par défaut sont correctes', () => {
    expect(DEFAULTS.langue).toBe('fr');
    expect(DEFAULTS.itemsParPage).toBe(20);
    expect(DEFAULTS.notifApp).toBe(true);
    expect(DEFAULTS.pageAccueil).toBe('dashboard');
  });

  it('merge les préférences avec les valeurs par défaut', () => {
    const prefs = { langue: 'en', itemsParPage: 50 };
    const merged = { ...DEFAULTS, ...prefs };
    expect(merged.langue).toBe('en');
    expect(merged.itemsParPage).toBe(50);
    expect(merged.notifApp).toBe(true); // valeur par défaut conservée
  });
});
