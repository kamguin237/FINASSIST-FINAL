import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { PermissionDTO } from '../models/role.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class PermissionsService {
  private url = `${environment.apiUrl}/permissions`;

  constructor(private http: HttpClient) {}

  getAll()                              { return this.http.get<PermissionDTO[]>(this.url); }
  getById(id: number)                   { return this.http.get<PermissionDTO>(`${this.url}/${id}`); }
  update(id: number, dto: Partial<PermissionDTO>) { return this.http.put<PermissionDTO>(`${this.url}/${id}`, dto); }
}
