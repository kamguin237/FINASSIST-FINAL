import { describe, it, expect, beforeEach, vi } from 'vitest';
import { of } from 'rxjs';

const mockHttp = { get: vi.fn() };
const BASE_URL = 'http://localhost:4200/api/logs';

function createService() {
  return {
    getLogs: (page = 1, pageSize = 20, filtres?: any) => {
      const params: string[] = [`page=${page}`, `pageSize=${pageSize}`];
      if (filtres?.action)    params.push(`action=${filtres.action}`);
      if (filtres?.dateDebut) params.push(`dateDebut=${filtres.dateDebut}`);
      if (filtres?.dateFin)   params.push(`dateFin=${filtres.dateFin}`);
      return mockHttp.get(`${BASE_URL}?${params.join('&')}`);
    }
  };
}

describe('LogsService — appels HTTP', () => {
  let service: ReturnType<typeof createService>;

  beforeEach(() => { vi.clearAllMocks(); service = createService(); });

  it('getLogs appelle GET /logs avec page et pageSize par défaut', () => {
    mockHttp.get.mockReturnValue(of({ total: 0, items: [] }));
    service.getLogs();
    expect(mockHttp.get).toHaveBeenCalledWith(`${BASE_URL}?page=1&pageSize=20`);
  });

  it('getLogs passe les paramètres de pagination', () => {
    mockHttp.get.mockReturnValue(of({ total: 0, items: [] }));
    service.getLogs(2, 10);
    expect(mockHttp.get).toHaveBeenCalledWith(`${BASE_URL}?page=2&pageSize=10`);
  });

  it('getLogs passe les filtres action et dates', () => {
    mockHttp.get.mockReturnValue(of({ total: 0, items: [] }));
    service.getLogs(1, 20, { action: 'CREATION', dateDebut: '2026-01-01', dateFin: '2026-12-31' });
    const call = mockHttp.get.mock.calls[0][0] as string;
    expect(call).toContain('action=CREATION');
    expect(call).toContain('dateDebut=2026-01-01');
    expect(call).toContain('dateFin=2026-12-31');
  });
});
