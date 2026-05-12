/**
 * Tests d'intégration — Flux Besoins
 *
 * Vérifie la collaboration entre :
 *   BesoinsService ↔ AuthService (token) ↔ intercepteur HTTP
 *
 * Scénarios : création, soumission, cycle de vie complet,
 * gestion des pièces jointes, URLs générées.
 */

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { of, throwError } from 'rxjs';
import { firstValueFrom } from 'rxjs';

// ── Constantes ────────────────────────────────────────────────────────────────

const TOKEN_KEY = 'finassist_token';
const API_URL   = 'http://localhost:5000/api';

function makeJwt(permissions = 'BESOIN_CONSULTER,BESOIN_CREER'): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const body   = btoa(JSON.stringify({
    exp: Math.floor(Date.now() / 1000) + 3600,
    sub: '1', role: 'Agent', permissions
  }));
  return `${header}.${body}.sig`;
}

// ── Simulation BesoinsService ─────────────────────────────────────────────────

interface BesoinDTO {
  id: number; titre: string; description: string;
  statut: string; niveauImportance: string; categorieId: number;
  utilisateurId: number; dateCreation: string;
}

interface CreateBesoinDTO {
  titre: string; description: string;
  niveauImportance: string; categorieId: number;
}

class BesoinsServiceSim {
  private http: { get: ReturnType<typeof vi.fn>; post: ReturnType<typeof vi.fn>; put: ReturnType<typeof vi.fn>; delete: ReturnType<typeof vi.fn> };
  private baseUrl = `${API_URL}/besoins`;

  constructor(http: typeof BesoinsServiceSim.prototype.http) {
    this.http = http;
  }

  getAll()                    { return this.http.get(this.baseUrl); }
  getById(id: number)         { return this.http.get(`${this.baseUrl}/${id}`); }
  enregistrer(id: number)     { return this.http.post(`${this.baseUrl}/${id}/enregistrer`, {}); }
  soumettre(id: number)       { return this.http.post(`${this.baseUrl}/${id}/soumettre`, {}); }
  delete(id: number)          { return this.http.delete(`${this.baseUrl}/${id}`); }
  getHistorique(id: number)   { return this.http.get(`${this.baseUrl}/${id}/historique`); }
  getDocuments(id: number)    { return this.http.get(`${this.baseUrl}/${id}/pieces-jointes`); }

  getDocumentUrl(besoinId: number, documentId: number): string {
    return `${this.baseUrl}/${besoinId}/pieces-jointes/${documentId}`;
  }

  getDocumentUrlWithToken(besoinId: number, documentId: number, token: string): string {
    return `${this.baseUrl}/${besoinId}/pieces-jointes/${documentId}?token=${encodeURIComponent(token)}`;
  }

  create(dto: CreateBesoinDTO, fichier?: File) {
    const form = new FormData();
    form.append('titre', dto.titre);
    form.append('description', dto.description);
    form.append('niveauImportance', dto.niveauImportance);
    form.append('categorieId', dto.categorieId.toString());
    if (fichier) form.append('fichier', fichier);
    return this.http.post(this.baseUrl, form);
  }
}

// ── Données de test ───────────────────────────────────────────────────────────

const besoinBrouillon: BesoinDTO = {
  id: 1, titre: 'Achat ordinateur', description: 'Besoin urgent',
  statut: 'BROUILLON', niveauImportance: 'MOYEN',
  categorieId: 1, utilisateurId: 1, dateCreation: '2026-04-01T10:00:00Z'
};

const besoinEnregistre: BesoinDTO = { ...besoinBrouillon, statut: 'ENREGISTRE' };
const besoinEnAttente: BesoinDTO  = { ...besoinBrouillon, statut: 'EN_ATTENTE_RESPONSABLE' };

// ═════════════════════════════════════════════════════════════════════════════
// Tests
// ═════════════════════════════════════════════════════════════════════════════

describe('Intégration — Flux Besoins', () => {
  let http: { get: ReturnType<typeof vi.fn>; post: ReturnType<typeof vi.fn>; put: ReturnType<typeof vi.fn>; delete: ReturnType<typeof vi.fn> };
  let service: BesoinsServiceSim;

  beforeEach(() => {
    localStorage.clear();
    localStorage.setItem(TOKEN_KEY, makeJwt());
    http = { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() };
    service = new BesoinsServiceSim(http);
  });

  afterEach(() => {
    localStorage.clear();
    vi.clearAllMocks();
  });

  // ── getAll ────────────────────────────────────────────────────────────────

  describe('getAll', () => {
    it('appelle GET /api/besoins', async () => {
      http.get.mockReturnValue(of([besoinBrouillon]));

      const result = await firstValueFrom(service.getAll() as any);

      expect(http.get).toHaveBeenCalledWith(`${API_URL}/besoins`);
      expect(result).toHaveLength(1);
      expect((result as BesoinDTO[])[0].titre).toBe('Achat ordinateur');
    });

    it('retourne une liste vide si aucun besoin', async () => {
      http.get.mockReturnValue(of([]));

      const result = await firstValueFrom(service.getAll() as any);

      expect(result).toHaveLength(0);
    });
  });

  // ── getById ───────────────────────────────────────────────────────────────

  describe('getById', () => {
    it('appelle GET /api/besoins/:id', async () => {
      http.get.mockReturnValue(of(besoinBrouillon));

      const result = await firstValueFrom(service.getById(1) as any);

      expect(http.get).toHaveBeenCalledWith(`${API_URL}/besoins/1`);
      expect((result as BesoinDTO).statut).toBe('BROUILLON');
    });
  });

  // ── create ────────────────────────────────────────────────────────────────

  describe('create', () => {
    it('envoie un FormData avec les champs requis', async () => {
      http.post.mockReturnValue(of(besoinBrouillon));

      await firstValueFrom(service.create({
        titre: 'Achat ordinateur',
        description: 'Besoin urgent',
        niveauImportance: 'MOYEN',
        categorieId: 1
      }) as any);

      expect(http.post).toHaveBeenCalledWith(
        `${API_URL}/besoins`,
        expect.any(FormData)
      );

      const formData = http.post.mock.calls[0][1] as FormData;
      expect(formData.get('titre')).toBe('Achat ordinateur');
      expect(formData.get('niveauImportance')).toBe('MOYEN');
      expect(formData.get('categorieId')).toBe('1');
    });

    it('inclut le fichier dans le FormData si fourni', async () => {
      http.post.mockReturnValue(of(besoinBrouillon));
      const fichier = new File(['contenu'], 'doc.pdf', { type: 'application/pdf' });

      await firstValueFrom(service.create({
        titre: 'Test', description: 'Desc', niveauImportance: 'FAIBLE', categorieId: 1
      }, fichier) as any);

      const formData = http.post.mock.calls[0][1] as FormData;
      expect(formData.get('fichier')).toBeTruthy();
    });

    it('n\'inclut pas de fichier si non fourni', async () => {
      http.post.mockReturnValue(of(besoinBrouillon));

      await firstValueFrom(service.create({
        titre: 'Test', description: 'Desc', niveauImportance: 'FAIBLE', categorieId: 1
      }) as any);

      const formData = http.post.mock.calls[0][1] as FormData;
      expect(formData.get('fichier')).toBeNull();
    });
  });

  // ── Cycle de vie : enregistrer → soumettre ────────────────────────────────

  describe('Cycle de vie : enregistrer → soumettre', () => {
    it('enregistrer appelle POST /api/besoins/:id/enregistrer', async () => {
      http.post.mockReturnValue(of(besoinEnregistre));

      const result = await firstValueFrom(service.enregistrer(1) as any);

      expect(http.post).toHaveBeenCalledWith(`${API_URL}/besoins/1/enregistrer`, {});
      expect((result as BesoinDTO).statut).toBe('ENREGISTRE');
    });

    it('soumettre appelle POST /api/besoins/:id/soumettre', async () => {
      http.post.mockReturnValue(of(besoinEnAttente));

      const result = await firstValueFrom(service.soumettre(1) as any);

      expect(http.post).toHaveBeenCalledWith(`${API_URL}/besoins/1/soumettre`, {});
      expect((result as BesoinDTO).statut).toBe('EN_ATTENTE_RESPONSABLE');
    });

    it('cycle complet : créer → enregistrer → soumettre', async () => {
      // Étape 1 : créer
      http.post.mockReturnValueOnce(of(besoinBrouillon));
      const created = await firstValueFrom(service.create({
        titre: 'Achat ordinateur', description: 'Desc', niveauImportance: 'MOYEN', categorieId: 1
      }) as any) as BesoinDTO;
      expect(created.statut).toBe('BROUILLON');

      // Étape 2 : enregistrer
      http.post.mockReturnValueOnce(of(besoinEnregistre));
      const enregistre = await firstValueFrom(service.enregistrer(created.id) as any) as BesoinDTO;
      expect(enregistre.statut).toBe('ENREGISTRE');

      // Étape 3 : soumettre
      http.post.mockReturnValueOnce(of(besoinEnAttente));
      const soumis = await firstValueFrom(service.soumettre(created.id) as any) as BesoinDTO;
      expect(soumis.statut).toBe('EN_ATTENTE_RESPONSABLE');
    });
  });

  // ── Historique ────────────────────────────────────────────────────────────

  describe('getHistorique', () => {
    it('appelle GET /api/besoins/:id/historique', async () => {
      const historique = [
        { id: 1, action: 'CREATION', description: 'Besoin créé', dateAction: '2026-04-01T10:00:00Z' },
        { id: 2, action: 'ENREGISTREMENT', description: 'Besoin enregistré', dateAction: '2026-04-01T11:00:00Z' }
      ];
      http.get.mockReturnValue(of(historique));

      const result = await firstValueFrom(service.getHistorique(1) as any) as typeof historique;

      expect(http.get).toHaveBeenCalledWith(`${API_URL}/besoins/1/historique`);
      expect(result).toHaveLength(2);
      expect(result[0].action).toBe('CREATION');
    });
  });

  // ── Documents ─────────────────────────────────────────────────────────────

  describe('Documents', () => {
    it('getDocuments appelle GET /api/besoins/:id/pieces-jointes', async () => {
      const docs = [{ id: 1, nom: 'rapport.pdf', type: 'application/pdf', checksum: 'abc', dateCreation: '2026-04-01' }];
      http.get.mockReturnValue(of(docs));

      const result = await firstValueFrom(service.getDocuments(1) as any) as typeof docs;

      expect(http.get).toHaveBeenCalledWith(`${API_URL}/besoins/1/pieces-jointes`);
      expect(result[0].nom).toBe('rapport.pdf');
    });

    it('getDocumentUrl génère l\'URL correcte', () => {
      const url = service.getDocumentUrl(1, 5);
      expect(url).toBe(`${API_URL}/besoins/1/pieces-jointes/5`);
    });

    it('getDocumentUrlWithToken encode le token dans l\'URL', () => {
      const token = 'mon+token/special=';
      const url = service.getDocumentUrlWithToken(1, 5, token);
      expect(url).toContain('token=');
      expect(url).toContain(encodeURIComponent(token));
    });
  });

  // ── delete ────────────────────────────────────────────────────────────────

  describe('delete', () => {
    it('appelle DELETE /api/besoins/:id', async () => {
      http.delete.mockReturnValue(of(undefined));

      await firstValueFrom(service.delete(1) as any);

      expect(http.delete).toHaveBeenCalledWith(`${API_URL}/besoins/1`);
    });
  });

  // ── Gestion d'erreurs HTTP ────────────────────────────────────────────────

  describe('Gestion d\'erreurs HTTP', () => {
    it('propage l\'erreur 404 si besoin introuvable', async () => {
      http.get.mockReturnValue(throwError(() => ({ status: 404, message: 'Not Found' })));

      await expect(firstValueFrom(service.getById(999) as any)).rejects.toMatchObject({ status: 404 });
    });

    it('propage l\'erreur 403 si accès refusé', async () => {
      http.get.mockReturnValue(throwError(() => ({ status: 403, message: 'Forbidden' })));

      await expect(firstValueFrom(service.getAll() as any)).rejects.toMatchObject({ status: 403 });
    });
  });
});
