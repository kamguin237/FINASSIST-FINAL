import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export interface QrSessionResponse {
  token: string;
  urlMobile: string;
  expiration: string;
}

export interface QrSessionStatus {
  completed: boolean;
  expired: boolean;
}

export interface QrSessionInfo {
  nom: string;
  prenom: string;
  role: string;
  expiration: string;
}

@Injectable({ providedIn: 'root' })
export class QrSignatureService {
  private url = `${environment.apiUrl}/signatures/qr`;

  constructor(private http: HttpClient) {}

  createSession()                        { return this.http.post<QrSessionResponse>(`${this.url}/session`, {}); }
  getStatus(token: string)               { return this.http.get<QrSessionStatus>(`${this.url}/session/${token}/status`); }
  getSessionInfo(token: string)          { return this.http.get<QrSessionInfo>(`${this.url}/session/${token}`); }
  submitSignature(token: string, imageBase64: string) {
    return this.http.post(`${this.url}/session/${token}/submit`, { imageBase64 });
  }
}
