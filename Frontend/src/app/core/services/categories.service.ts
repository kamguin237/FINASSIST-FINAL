import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CategorieDTO, CategorieDetailDTO, CreateCategorieDTO, UpdateCategorieDTO } from '../models/categorie.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class CategoriesService {
  private url = `${environment.apiUrl}/categories`;

  constructor(private http: HttpClient) {}

  getAll()                              { return this.http.get<CategorieDTO[]>(this.url); }
  getById(id: number)                   { return this.http.get<CategorieDetailDTO>(`${this.url}/${id}`); }
  create(dto: CreateCategorieDTO)       { return this.http.post<CategorieDTO>(this.url, dto); }
  update(id: number, dto: UpdateCategorieDTO) { return this.http.put<CategorieDTO>(`${this.url}/${id}`, dto); }
  delete(id: number)                    { return this.http.delete<void>(`${this.url}/${id}`); }
}
