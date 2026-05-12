import { describe, it, expect, beforeEach, vi } from 'vitest';
import { of } from 'rxjs';

const mockHttp = { get: vi.fn(), post: vi.fn() };
const BASE_URL = 'http://localhost:4200/api/reporting';

function createService() {
  return {
    getDashboard: () => mockHttp.get(`${BASE_URL}/dashboard`),
    getEvolutionBesoins: (periode: string) =>
      mockHttp.get(`${BASE_URL}/dashboard/evolution?periode=${periode}`),
    getStatistiques: () => mockHttp.get(`${BASE_URL}/statistiques`),
    getRapportBesoins: (filtres?: any) => {
      const params: string[] = [];
      if (filtres?.dateDebut) params.push(`dateDebut=${filtres.dateDebut}`);
      if (filtres?.dateFin)   params.push(`dateFin=${filtres.dateFin}`);
      if (filtres?.statut)    params.push(`statut=${filtres.statut}`);
      const query = params.length ? `?${params.join('&')}` : '';
      return mockHttp.get(`${BASE_URL}/besoins${query}`);
    },
    exporter: (request: any) => mockHttp.post(`${BASE_URL}/exporter`, request)
  };
}

describe('ReportingService — appels HTTP', () => {
  let service: ReturnType<typeof createService>;

  beforeEach(() => { vi.clearAllMocks(); service = createService(); });

  it('getDashboard appelle GET /reporting/dashboard', () => {
    mockHttp.get.mockReturnValue(of({}));
    service.getDashboard();
    expect(mockHttp.get).toHaveBeenCalledWith(`${BASE_URL}/dashboard`);
  });

  it('getEvolutionBesoins appelle GET avec la période', () => {
    mockHttp.get.mockReturnValue(of([]));
    service.getEvolutionBesoins('jours');
    expect(mockHttp.get).toHaveBeenCalledWith(`${BASE_URL}/dashboard/evolution?periode=jours`);
  });

  it('getEvolutionBesoins accepte semaines et mois', () => {
    mockHttp.get.mockReturnValue(of([]));
    service.getEvolutionBesoins('semaines');
    expect(mockHttp.get).toHaveBeenCalledWith(`${BASE_URL}/dashboard/evolution?periode=semaines`);
    service.getEvolutionBesoins('mois');
    expect(mockHttp.get).toHaveBeenCalledWith(`${BASE_URL}/dashboard/evolution?periode=mois`);
  });

  it('getStatistiques appelle GET /reporting/statistiques', () => {
    mockHttp.get.mockReturnValue(of({}));
    service.getStatistiques();
    expect(mockHttp.get).toHaveBeenCalledWith(`${BASE_URL}/statistiques`);
  });

  it('getRapportBesoins sans filtres appelle GET /reporting/besoins', () => {
    mockHttp.get.mockReturnValue(of([]));
    service.getRapportBesoins();
    expect(mockHttp.get).toHaveBeenCalledWith(`${BASE_URL}/besoins`);
  });

  it('exporter appelle POST /reporting/exporter avec le request', () => {
    const request = { format: 'PDF', filtres: null };
    mockHttp.post.mockReturnValue(of(new Blob()));
    service.exporter(request);
    expect(mockHttp.post).toHaveBeenCalledWith(`${BASE_URL}/exporter`, request);
  });
});
