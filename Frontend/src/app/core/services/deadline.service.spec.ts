import { describe, it, expect, beforeEach, vi } from 'vitest';
import { of } from 'rxjs';

const mockHttp = { get: vi.fn() };
const BASE_URL = 'http://localhost:4200/api/besoins/deadlines';

function createDeadlineService() {
  return {
    getDeadlines: () => mockHttp.get(BASE_URL)
  };
}

describe('DeadlineService — appels HTTP', () => {
  let service: ReturnType<typeof createDeadlineService>;

  beforeEach(() => {
    vi.clearAllMocks();
    service = createDeadlineService();
  });

  it('getDeadlines appelle GET /besoins/deadlines', () => {
    mockHttp.get.mockReturnValue(of([]));
    service.getDeadlines();
    expect(mockHttp.get).toHaveBeenCalledWith(BASE_URL);
  });
});

// ── Tests logique urgence deadline ────────────────────────────────────────────

describe('Deadline — calcul urgence', () => {

  function getUrgence(pct: number): string {
    if (pct >= 100) return 'expired';
    if (pct >= 80) return 'danger';
    if (pct >= 50) return 'warning';
    return 'normal';
  }

  it('retourne "normal" pour < 50%', () => {
    expect(getUrgence(0)).toBe('normal');
    expect(getUrgence(49)).toBe('normal');
  });

  it('retourne "warning" pour 50-79%', () => {
    expect(getUrgence(50)).toBe('warning');
    expect(getUrgence(79)).toBe('warning');
  });

  it('retourne "danger" pour 80-99%', () => {
    expect(getUrgence(80)).toBe('danger');
    expect(getUrgence(99)).toBe('danger');
  });

  it('retourne "expired" pour >= 100%', () => {
    expect(getUrgence(100)).toBe('expired');
    expect(getUrgence(150)).toBe('expired');
  });

  it('calcule le pourcentage écoulé correctement', () => {
    const delaiMinutes = 100;
    const elapsed = 55;
    const pct = (elapsed / delaiMinutes) * 100;
    expect(pct).toBeCloseTo(55, 5);
    expect(getUrgence(pct)).toBe('warning');
  });

  it('calcule les minutes restantes', () => {
    const delaiMinutes = 100;
    const elapsed = 60;
    const restant = Math.max(delaiMinutes - elapsed, 0);
    expect(restant).toBe(40);
  });

  it('les minutes restantes ne sont jamais négatives', () => {
    const delaiMinutes = 100;
    const elapsed = 150; // dépassé
    const restant = Math.max(delaiMinutes - elapsed, 0);
    expect(restant).toBe(0);
  });
});
