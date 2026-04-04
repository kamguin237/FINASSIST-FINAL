import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { DashboardDTO, EvolutionPoint, StatistiquesDTO, FiltreRapportDTO, ExportRequestDTO } from '../models/reporting.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ReportingService {
  private url = `${environment.apiUrl}/reporting`;

  constructor(private http: HttpClient) {}

  getDashboard()                            { return this.http.get<DashboardDTO>(`${this.url}/dashboard`); }
  getEvolutionBesoins(periode: 'jours' | 'semaines' | 'mois') {
    return this.http.get<EvolutionPoint[]>(`${this.url}/dashboard/evolution?periode=${periode}`);
  }
  getStatistiques()                         { return this.http.get<StatistiquesDTO>(`${this.url}/statistiques`); }

  getRapportBesoins(filtres?: FiltreRapportDTO) {
    let params = new HttpParams();
    if (filtres?.dateDebut) params = params.set('dateDebut', filtres.dateDebut);
    if (filtres?.dateFin)   params = params.set('dateFin', filtres.dateFin);
    if (filtres?.statut)    params = params.set('statut', filtres.statut);
    return this.http.get(`${this.url}/besoins`, { params });
  }

  exporter(request: ExportRequestDTO) {
    return this.http.post(`${this.url}/exporter`, request, { responseType: 'blob' });
  }
}
