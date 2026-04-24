import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BesoinDTO, CreateBesoinDTO, UpdateBesoinDTO, HistoriqueDTO, DocumentDTO } from '../models/besoin.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class BesoinsService {
  private url = `${environment.apiUrl}/besoins`;

  constructor(private http: HttpClient) {}

  getAll()                          { return this.http.get<BesoinDTO[]>(this.url); }
  getById(id: number)               { return this.http.get<BesoinDTO>(`${this.url}/${id}`); }
  create(dto: CreateBesoinDTO, fichier?: File) {
    const form = new FormData();
    form.append('titre', dto.titre);
    form.append('description', dto.description);
    form.append('niveauImportance', dto.niveauImportance);
    form.append('categorieId', dto.categorieId.toString());
    if (fichier) form.append('fichier', fichier);
    return this.http.post<BesoinDTO>(this.url, form);
  }
  update(id: number, dto: UpdateBesoinDTO) { return this.http.put<BesoinDTO>(`${this.url}/${id}`, dto); }
  delete(id: number)                { return this.http.delete<void>(`${this.url}/${id}`); }
  enregistrer(id: number)           { return this.http.post<BesoinDTO>(`${this.url}/${id}/enregistrer`, {}); }
  soumettre(id: number)             { return this.http.post<BesoinDTO>(`${this.url}/${id}/soumettre`, {}); }
  getHistorique(id: number)         { return this.http.get<HistoriqueDTO[]>(`${this.url}/${id}/historique`); }
  getDocuments(id: number)          { return this.http.get<DocumentDTO[]>(`${this.url}/${id}/pieces-jointes`); }
  getDocumentUrl(besoinId: number, documentId: number): string {
    return `${this.url}/${besoinId}/pieces-jointes/${documentId}`;
  }

  getDocumentUrlWithToken(besoinId: number, documentId: number, token: string): string {
    return `${this.url}/${besoinId}/pieces-jointes/${documentId}?token=${encodeURIComponent(token)}`;
  }

  ajouterPieceJointe(id: number, fichier: File) {
    const form = new FormData();
    form.append('fichier', fichier);
    return this.http.post<DocumentDTO>(`${this.url}/${id}/pieces-jointes`, form);
  }

  supprimerDocument(besoinId: number, documentId: number) {
    return this.http.delete<void>(`${this.url}/${besoinId}/pieces-jointes/${documentId}`);
  }
}
