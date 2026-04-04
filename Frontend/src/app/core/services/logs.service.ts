import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class LogsService {
  private url = `${environment.apiUrl}/logs`;

  constructor(private http: HttpClient) {}

  getLogs(page = 1, pageSize = 20, filtres?: { action?: string; dateDebut?: string; dateFin?: string }) {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (filtres?.action)    params = params.set('action', filtres.action);
    if (filtres?.dateDebut) params = params.set('dateDebut', filtres.dateDebut);
    if (filtres?.dateFin)   params = params.set('dateFin', filtres.dateFin);
    return this.http.get<{ total: number; page: number; pageSize: number; items: any[] }>(this.url, { params });
  }
}
