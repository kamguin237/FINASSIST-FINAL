import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export interface SignerBesoinPayload {
  documentId: number;
  signatureBase64?: string;
  positionX?: number;
  positionY?: number;
  largeur?: number;
  hauteur?: number;
  pdfSigneBase64?: string; // PDF déjà signé côté frontend (pdf-lib)
}

export interface SignatureApercu {
  id: number;
  signatureBase64?: string;
  positionX?: number;
  positionY?: number;
  largeur?: number;
  hauteur?: number;
  horodatage: string;
  empreinte: string;
  valide: boolean;
  signataire: { nom: string; prenom: string; role: string; };
}

@Injectable({ providedIn: 'root' })
export class SignaturesService {
  private url = `${environment.apiUrl}/signatures`;

  constructor(private http: HttpClient) {}

  signerParBesoin(besoinId: number, payload?: SignerBesoinPayload) {
    return this.http.post(`${this.url}/besoins/${besoinId}/signer`, payload ?? {});
  }

  signerParDocument(documentId: number) {
    return this.http.post(`${this.url}/${documentId}/signer`, {});
  }

  verifier(id: number) {
    return this.http.get(`${this.url}/${id}/verifier`);
  }

  getApercuBesoin(besoinId: number) {
    return this.http.get<SignatureApercu>(`${this.url}/besoins/${besoinId}`);
  }

  telechargerDocumentSigne(signatureId: number) {
    return this.http.get(`${this.url}/${signatureId}/document-signe`, { responseType: 'blob' });
  }
}
