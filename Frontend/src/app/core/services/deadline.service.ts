import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BesoinDeadlineDTO } from '../models/deadline.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class DeadlineService {
  private url = `${environment.apiUrl}/besoins/deadlines`;
  constructor(private http: HttpClient) {}
  getMesDeadlines() { return this.http.get<BesoinDeadlineDTO[]>(this.url); }
}
