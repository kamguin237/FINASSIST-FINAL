import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { UtilisateurDTO, CreateUtilisateurDTO, UpdateUtilisateurDTO, PermissionsEffectivesDTO, LogUtilisateurDTO } from '../models/user.models';
import { AssignerPermissionsDTO } from '../models/role.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class UsersService {
  private url = `${environment.apiUrl}/users`;

  constructor(private http: HttpClient) {}

  getMe() { return this.http.get<UtilisateurDTO>(`${this.url}/me`); }
  changePassword(dto: { ancienMotDePasse: string; nouveauMotDePasse: string }) {
    return this.http.put<{ message: string }>(`${this.url}/me/password`, dto);
  }
  getAll()                                  { return this.http.get<UtilisateurDTO[]>(this.url); }
  getById(id: number)                       { return this.http.get<UtilisateurDTO>(`${this.url}/${id}`); }
  create(dto: CreateUtilisateurDTO)         { return this.http.post<UtilisateurDTO>(this.url, dto); }
  update(id: number, dto: UpdateUtilisateurDTO) { return this.http.put<UtilisateurDTO>(`${this.url}/${id}`, dto); }
  deactivate(id: number)                    { return this.http.delete<void>(`${this.url}/${id}`); }
  activate(id: number)                      { return this.http.patch<void>(`${this.url}/${id}/activer`, {}); }
  deletePermanent(id: number)               { return this.http.delete<void>(`${this.url}/${id}/supprimer`); }
  changeRole(id: number, roleId: number)    { return this.http.put<UtilisateurDTO>(`${this.url}/${id}/role`, { roleId }); }
  getLogs(id: number)                       { return this.http.get<LogUtilisateurDTO[]>(`${this.url}/${id}/logs`); }
  getPermissions(id: number)                { return this.http.get<PermissionsEffectivesDTO>(`${this.url}/${id}/permissions`); }
  addPermissions(id: number, dto: AssignerPermissionsDTO)  { return this.http.post(`${this.url}/${id}/permissions`, dto); }
  setPermissions(id: number, dto: AssignerPermissionsDTO)  { return this.http.put(`${this.url}/${id}/permissions`, dto); }
  removePermission(id: number, permId: number)             { return this.http.delete(`${this.url}/${id}/permissions/${permId}`); }
}
