import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { SignatureUtilisateurDTO, SaveSignatureUtilisateurDTO } from '../models/signature-utilisateur.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class MaSignatureService {
  private url = `${environment.apiUrl}/ma-signature`;

  constructor(private http: HttpClient) {}

  get()                                    { return this.http.get<SignatureUtilisateurDTO>(this.url); }
  save(dto: SaveSignatureUtilisateurDTO)   { return this.http.post<SignatureUtilisateurDTO>(this.url, dto); }
  delete()                                 { return this.http.delete<void>(this.url); }
}
