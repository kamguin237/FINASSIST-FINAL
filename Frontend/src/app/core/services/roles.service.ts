import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { RoleDTO, CreateRoleDTO, PermissionDTO, AssignerPermissionsDTO } from '../models/role.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class RolesService {
  private url = `${environment.apiUrl}/roles`;

  constructor(private http: HttpClient) {}

  getAll()                                  { return this.http.get<RoleDTO[]>(this.url); }
  getById(id: number)                       { return this.http.get<RoleDTO>(`${this.url}/${id}`); }
  create(dto: CreateRoleDTO)                { return this.http.post<RoleDTO>(this.url, dto); }
  update(id: number, dto: CreateRoleDTO)    { return this.http.put<RoleDTO>(`${this.url}/${id}`, dto); }
  delete(id: number)                        { return this.http.delete<void>(`${this.url}/${id}`); }
  getPermissions(id: number)                { return this.http.get<PermissionDTO[]>(`${this.url}/${id}/permissions`); }
  addPermissions(id: number, dto: AssignerPermissionsDTO)  { return this.http.post(`${this.url}/${id}/permissions`, dto); }
  setPermissions(id: number, dto: AssignerPermissionsDTO)  { return this.http.put(`${this.url}/${id}/permissions`, dto); }
  removePermission(id: number, permId: number)             { return this.http.delete(`${this.url}/${id}/permissions/${permId}`); }
}
