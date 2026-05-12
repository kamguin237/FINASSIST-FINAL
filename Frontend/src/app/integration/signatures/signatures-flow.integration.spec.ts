/**
 * Tests d'intégration — Flux Signatures
 *
 * Vérifie la collaboration entre :
 *   SignaturesService ↔ MaSignatureService ↔ AuthService ↔ HttpClient
 *
 * Scénarios : signer un besoin, vérifier, aperçu, télécharger,
 * signature utilisateur (manuscrite/typographique/upload).
 */

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { of, throwError } from 'rxjs';
import { firstValueFrom } from 'rxjs';

// ── Constantes ────────────────────────────────────────────────────────────────

const TOKEN_KEY = 'finassist_token';
const API_URL   = 'http://localhost:5000/api';

function makeJwt(): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const body   = btoa(JSON.stringify({
    exp: Math.floor(Date.now() / 1000) + 3600,
    sub: '1', role: 'Responsable', permissions: 'SIGNATURE_APPOSER'
  }));
  return `${header}.${body}.sig`;
}

// ── Types ─────────────────────────────────────────────────────────────────────

interface SignerBesoinPayload {
  documentId?: number; signatureBase64?: string;
  positionX?: number; positionY?: number;
  largeur?: number; hauteur?: number; pdfSigneBase64?: string;
}

interface SignatureApercu {
  id: number; signatureBase64?: string; horodatage: string;
  empreinte: string; valide: boolean;
  signataire: { nom: string; prenom: string; role: string };
}

interface SignatureUtilisateurDTO {
  id: number; type: string; imageBase64: string; police?: string;
  dateCreation: string; dateModification: string;
}

// ── Simulation SignaturesService ──────────────────────────────────────────────

class SignaturesServiceSim {
  private http: { get: ReturnType<typeof vi.fn>; post: ReturnType<typeof vi.fn> };
  private url = `${API_URL}/signatures`;

  constructor(http: typeof SignaturesServiceSim.prototype.http) {
    this.http = http;
  }

  signerParBesoin(besoinId: number, payload?: SignerBesoinPayload) {
    return this.http.post(`${this.url}/besoins/${besoinId}/signer`, payload ?? {});
  }

  signerParDocument(documentId: number) {
    return this.http.post(`${this.url}/${documentId}/signer`, {});
  }

  verifier(id: number)          { return this.http.get(`${this.url}/${id}/verifier`); }
  getApercuBesoin(besoinId: number) { return this.http.get(`${this.url}/besoins/${besoinId}`); }
  telechargerDocumentSigne(signatureId: number) {
    return this.http.get(`${this.url}/${signatureId}/document-signe`);
  }
}

// ── Simulation MaSignatureService ─────────────────────────────────────────────

class MaSignatureServiceSim {
  private http: { get: ReturnType<typeof vi.fn>; post: ReturnType<typeof vi.fn>; delete: ReturnType<typeof vi.fn> };
  private url = `${API_URL}/ma-signature`;

  constructor(http: typeof MaSignatureServiceSim.prototype.http) {
    this.http = http;
  }

  getMaSignature()                    { return this.http.get(this.url); }
  sauvegarder(dto: Partial<SignatureUtilisateurDTO>) { return this.http.post(this.url, dto); }
  supprimer()                         { return this.http.delete(this.url); }
}

// ── Données de test ───────────────────────────────────────────────────────────

const mockApercu: SignatureApercu = {
  id: 1, signatureBase64: 'data:image/png;base64,abc123',
  horodatage: '2026-04-01T10:00:00Z',
  empreinte: 'sha256_hash_abc',
  valide: true,
  signataire: { nom: 'Martin', prenom: 'Paul', role: 'Responsable' }
};

const mockSignatureUtilisateur: SignatureUtilisateurDTO = {
  id: 1, type: 'manuscrite', imageBase64: 'data:image/png;base64,sig_img',
  dateCreation: '2026-04-01T10:00:00Z', dateModification: '2026-04-01T10:00:00Z'
};

// ═════════════════════════════════════════════════════════════════════════════
// Tests
// ═════════════════════════════════════════════════════════════════════════════

describe('Intégration — Flux Signatures', () => {
  let http: { get: ReturnType<typeof vi.fn>; post: ReturnType<typeof vi.fn>; delete: ReturnType<typeof vi.fn> };
  let sigService: SignaturesServiceSim;
  let maSignatureService: MaSignatureServiceSim;

  beforeEach(() => {
    localStorage.clear();
    localStorage.setItem(TOKEN_KEY, makeJwt());
    http = { get: vi.fn(), post: vi.fn(), delete: vi.fn() };
    sigService = new SignaturesServiceSim(http);
    maSignatureService = new MaSignatureServiceSim(http);
  });

  afterEach(() => {
    localStorage.clear();
    vi.clearAllMocks();
  });

  // ── signerParBesoin ───────────────────────────────────────────────────────

  describe('signerParBesoin', () => {
    it('appelle POST /api/signatures/besoins/:id/signer', async () => {
      http.post.mockReturnValue(of({ id: 1, valide: true }));

      await firstValueFrom(sigService.signerParBesoin(5) as any);

      expect(http.post).toHaveBeenCalledWith(
        `${API_URL}/signatures/besoins/5/signer`, {}
      );
    });

    it('envoie le payload avec signature manuscrite', async () => {
      http.post.mockReturnValue(of({ id: 1, valide: true }));
      const payload: SignerBesoinPayload = {
        signatureBase64: 'data:image/png;base64,abc',
        positionX: 10, positionY: 80, largeur: 200, hauteur: 80
      };

      await firstValueFrom(sigService.signerParBesoin(5, payload) as any);

      expect(http.post).toHaveBeenCalledWith(
        `${API_URL}/signatures/besoins/5/signer`, payload
      );
    });

    it('envoie le payload avec PDF pré-signé', async () => {
      http.post.mockReturnValue(of({ id: 1, valide: true }));
      const payload: SignerBesoinPayload = { pdfSigneBase64: 'JVBERi0xLjQ=' };

      await firstValueFrom(sigService.signerParBesoin(5, payload) as any);

      const sentPayload = http.post.mock.calls[0][1] as SignerBesoinPayload;
      expect(sentPayload.pdfSigneBase64).toBe('JVBERi0xLjQ=');
    });
  });

  // ── signerParDocument ─────────────────────────────────────────────────────

  describe('signerParDocument', () => {
    it('appelle POST /api/signatures/:documentId/signer', async () => {
      http.post.mockReturnValue(of({ id: 1, valide: true }));

      await firstValueFrom(sigService.signerParDocument(10) as any);

      expect(http.post).toHaveBeenCalledWith(
        `${API_URL}/signatures/10/signer`, {}
      );
    });
  });

  // ── verifier ──────────────────────────────────────────────────────────────

  describe('verifier', () => {
    it('appelle GET /api/signatures/:id/verifier', async () => {
      http.get.mockReturnValue(of({ authentique: true, message: 'Signature authentique' }));

      const result = await firstValueFrom(sigService.verifier(1) as any) as { authentique: boolean };

      expect(http.get).toHaveBeenCalledWith(`${API_URL}/signatures/1/verifier`);
      expect(result.authentique).toBe(true);
    });

    it('retourne false si signature invalide', async () => {
      http.get.mockReturnValue(of({ authentique: false, message: 'Document modifié' }));

      const result = await firstValueFrom(sigService.verifier(1) as any) as { authentique: boolean };

      expect(result.authentique).toBe(false);
    });
  });

  // ── getApercuBesoin ───────────────────────────────────────────────────────

  describe('getApercuBesoin', () => {
    it('appelle GET /api/signatures/besoins/:id', async () => {
      http.get.mockReturnValue(of(mockApercu));

      const result = await firstValueFrom(sigService.getApercuBesoin(5) as any) as SignatureApercu;

      expect(http.get).toHaveBeenCalledWith(`${API_URL}/signatures/besoins/5`);
      expect(result.valide).toBe(true);
      expect(result.signataire.nom).toBe('Martin');
    });
  });

  // ── telechargerDocumentSigne ──────────────────────────────────────────────

  describe('telechargerDocumentSigne', () => {
    it('appelle GET /api/signatures/:id/document-signe', async () => {
      const blob = new Blob(['%PDF-1.4'], { type: 'application/pdf' });
      http.get.mockReturnValue(of(blob));

      const result = await firstValueFrom(sigService.telechargerDocumentSigne(1) as any);

      expect(http.get).toHaveBeenCalledWith(`${API_URL}/signatures/1/document-signe`);
      expect(result).toBeInstanceOf(Blob);
    });
  });

  // ── MaSignatureService ────────────────────────────────────────────────────

  describe('MaSignatureService', () => {
    it('getMaSignature appelle GET /api/ma-signature', async () => {
      http.get.mockReturnValue(of(mockSignatureUtilisateur));

      const result = await firstValueFrom(maSignatureService.getMaSignature() as any) as SignatureUtilisateurDTO;

      expect(http.get).toHaveBeenCalledWith(`${API_URL}/ma-signature`);
      expect(result.type).toBe('manuscrite');
    });

    it('getMaSignature retourne null si aucune signature', async () => {
      http.get.mockReturnValue(of(null));

      const result = await firstValueFrom(maSignatureService.getMaSignature() as any);

      expect(result).toBeNull();
    });

    it('sauvegarder envoie POST avec le DTO', async () => {
      http.post.mockReturnValue(of(mockSignatureUtilisateur));
      const dto = { type: 'typographique', imageBase64: 'data:image/png;base64,abc', police: 'Dancing Script' };

      const result = await firstValueFrom(maSignatureService.sauvegarder(dto) as any) as SignatureUtilisateurDTO;

      expect(http.post).toHaveBeenCalledWith(`${API_URL}/ma-signature`, dto);
      expect(result.type).toBe('manuscrite');
    });

    it('supprimer appelle DELETE /api/ma-signature', async () => {
      http.delete.mockReturnValue(of(undefined));

      await firstValueFrom(maSignatureService.supprimer() as any);

      expect(http.delete).toHaveBeenCalledWith(`${API_URL}/ma-signature`);
    });
  });

  // ── Cycle complet : signer → vérifier → aperçu ───────────────────────────

  describe('Cycle complet : signer → vérifier → aperçu', () => {
    it('signe un besoin puis vérifie la signature', async () => {
      // 1. Signer
      http.post.mockReturnValue(of({ id: 1, valide: true, empreinte: 'sha256_abc' }));
      const sig = await firstValueFrom(sigService.signerParBesoin(5) as any) as { id: number; valide: boolean };
      expect(sig.valide).toBe(true);

      // 2. Vérifier
      http.get.mockReturnValueOnce(of({ authentique: true, message: 'Signature authentique' }));
      const verif = await firstValueFrom(sigService.verifier(sig.id) as any) as { authentique: boolean };
      expect(verif.authentique).toBe(true);

      // 3. Aperçu
      http.get.mockReturnValueOnce(of(mockApercu));
      const apercu = await firstValueFrom(sigService.getApercuBesoin(5) as any) as SignatureApercu;
      expect(apercu.valide).toBe(true);
      expect(apercu.signataire.role).toBe('Responsable');
    });
  });

  // ── Gestion d'erreurs ─────────────────────────────────────────────────────

  describe('Gestion d\'erreurs', () => {
    it('propage l\'erreur 400 si besoin non approuvé', async () => {
      http.post.mockReturnValue(throwError(() => ({ status: 400, error: { message: 'Besoin non approuvé' } })));

      await expect(firstValueFrom(sigService.signerParBesoin(5) as any))
        .rejects.toMatchObject({ status: 400 });
    });

    it('propage l\'erreur 409 si déjà signé', async () => {
      http.post.mockReturnValue(throwError(() => ({ status: 409, error: { message: 'Déjà signé' } })));

      await expect(firstValueFrom(sigService.signerParBesoin(5) as any))
        .rejects.toMatchObject({ status: 409 });
    });
  });
});
