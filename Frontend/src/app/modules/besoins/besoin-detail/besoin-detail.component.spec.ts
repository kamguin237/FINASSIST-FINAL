import { describe, it, expect } from 'vitest';

// ── Tests logique BesoinDetailComponent ──────────────────────────────────────

describe('BesoinDetailComponent — peutValider', () => {

  function peutValider(besoin: { statut: string; dejaValideParMoi: boolean } | null, validerLoading: boolean): boolean {
    if (!besoin) return false;
    return !besoin.dejaValideParMoi &&
           besoin.statut.startsWith('EN_ATTENTE_') &&
           !validerLoading;
  }

  it('retourne false si besoin est null', () => {
    expect(peutValider(null, false)).toBe(false);
  });

  it('retourne false si déjà validé par moi', () => {
    expect(peutValider({ statut: 'EN_ATTENTE_RESPONSABLE', dejaValideParMoi: true }, false)).toBe(false);
  });

  it('retourne false si statut n\'est pas EN_ATTENTE', () => {
    expect(peutValider({ statut: 'BROUILLON', dejaValideParMoi: false }, false)).toBe(false);
    expect(peutValider({ statut: 'TERMINE', dejaValideParMoi: false }, false)).toBe(false);
  });

  it('retourne false si validerLoading est true', () => {
    expect(peutValider({ statut: 'EN_ATTENTE_RESPONSABLE', dejaValideParMoi: false }, true)).toBe(false);
  });

  it('retourne true si toutes les conditions sont remplies', () => {
    expect(peutValider({ statut: 'EN_ATTENTE_RESPONSABLE', dejaValideParMoi: false }, false)).toBe(true);
  });
});

describe('BesoinDetailComponent — estSigne', () => {

  function estSigne(statut: string | undefined): boolean {
    return !!(statut?.startsWith('SIGNE_PAR_'));
  }

  it('retourne true pour SIGNE_PAR_RESPONSABLE', () => {
    expect(estSigne('SIGNE_PAR_RESPONSABLE')).toBe(true);
  });

  it('retourne false pour APPROUVE_PAR_RESPONSABLE', () => {
    expect(estSigne('APPROUVE_PAR_RESPONSABLE')).toBe(false);
  });

  it('retourne false pour undefined', () => {
    expect(estSigne(undefined)).toBe(false);
  });
});

describe('BesoinDetailComponent — validation formulaire valider', () => {

  function isValiderFormValid(decision: string, motif: string): boolean {
    if (!decision) return false;
    if (decision === 'REJETE' && !motif.trim()) return false;
    return true;
  }

  it('valide si décision APPROUVE sans motif', () => {
    expect(isValiderFormValid('APPROUVE', '')).toBe(true);
  });

  it('invalide si décision REJETE sans motif', () => {
    expect(isValiderFormValid('REJETE', '')).toBe(false);
  });

  it('valide si décision REJETE avec motif', () => {
    expect(isValiderFormValid('REJETE', 'Non conforme')).toBe(true);
  });

  it('invalide si décision vide', () => {
    expect(isValiderFormValid('', '')).toBe(false);
  });
});

describe('BesoinDetailComponent — gestion des documents', () => {

  it('isImage retourne true pour un type image', () => {
    const documents = [{ id: 1, nom: 'photo.png', type: 'image/png', checksum: '', dateCreation: '' }];
    const isImage = documents.length > 0 && documents[0].type?.startsWith('image/');
    expect(isImage).toBe(true);
  });

  it('isImage retourne false pour un PDF', () => {
    const documents = [{ id: 1, nom: 'doc.pdf', type: 'application/pdf', checksum: '', dateCreation: '' }];
    const isImage = documents.length > 0 && documents[0].type?.startsWith('image/');
    expect(isImage).toBe(false);
  });

  it('isImage retourne false si pas de documents', () => {
    const documents: any[] = [];
    const isImage = documents.length > 0 && documents[0]?.type?.startsWith('image/');
    expect(isImage).toBe(false);
  });
});

describe('BesoinDetailComponent — onDocClick position encoding', () => {

  it('encode la position page+Y correctement', () => {
    const pageIndex = 2;
    const pctYOnPage = 45.5;
    const encodedY = pageIndex * 1000 + Math.round(pctYOnPage * 10) / 10;
    expect(encodedY).toBe(2045.5);
  });

  it('décode la position page+Y correctement', () => {
    const encodedY = 2045.5;
    const pageIndex = Math.floor(encodedY / 1000);
    const pctYOnPage = encodedY % 1000;
    expect(pageIndex).toBe(2);
    expect(pctYOnPage).toBeCloseTo(45.5, 1);
  });

  it('page 0 encode correctement', () => {
    const pageIndex = 0;
    const pctYOnPage = 30;
    const encodedY = pageIndex * 1000 + pctYOnPage;
    expect(encodedY).toBe(30);
    expect(Math.floor(encodedY / 1000)).toBe(0);
  });
});
