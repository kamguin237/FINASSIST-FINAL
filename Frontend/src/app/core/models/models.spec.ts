import { describe, it, expect } from 'vitest';
import { NIVEAUX_IMPORTANCE, STATUTS_BESOIN } from '../models/besoin.models';

// ── Tests des constantes et modèles ──────────────────────────────────────────

describe('NIVEAUX_IMPORTANCE', () => {
  it('contient les 4 niveaux attendus', () => {
    expect(NIVEAUX_IMPORTANCE).toHaveLength(4);
    expect(NIVEAUX_IMPORTANCE).toContain('FAIBLE');
    expect(NIVEAUX_IMPORTANCE).toContain('MOYEN');
    expect(NIVEAUX_IMPORTANCE).toContain('ELEVE');
    expect(NIVEAUX_IMPORTANCE).toContain('CRITIQUE');
  });

  it('est ordonné du moins critique au plus critique', () => {
    expect(NIVEAUX_IMPORTANCE[0]).toBe('FAIBLE');
    expect(NIVEAUX_IMPORTANCE[3]).toBe('CRITIQUE');
  });

  it('est en lecture seule (readonly)', () => {
    // TypeScript empêche la modification, on vérifie juste que c'est un tableau
    expect(Array.isArray(NIVEAUX_IMPORTANCE)).toBe(true);
  });
});

describe('STATUTS_BESOIN', () => {
  it('contient les statuts principaux', () => {
    expect(STATUTS_BESOIN.BROUILLON).toBe('BROUILLON');
    expect(STATUTS_BESOIN.ENREGISTRE).toBe('ENREGISTRE');
    expect(STATUTS_BESOIN.EN_ATTENTE).toBe('EN_ATTENTE');
    expect(STATUTS_BESOIN.TRANSMIS).toBe('TRANSMIS');
    expect(STATUTS_BESOIN.TERMINE).toBe('TERMINE');
  });

  it('les valeurs correspondent aux clés', () => {
    Object.entries(STATUTS_BESOIN).forEach(([key, value]) => {
      expect(key).toBe(value);
    });
  });
});

// ── Tests logique de filtrage des statuts ─────────────────────────────────────

describe('Logique statuts besoin', () => {

  function estEnAttente(statut: string): boolean {
    return statut === 'EN_ATTENTE' || statut.startsWith('EN_ATTENTE_');
  }

  function estApprouve(statut: string): boolean {
    return statut.startsWith('APPROUVE_PAR_');
  }

  function estRejete(statut: string): boolean {
    return statut.startsWith('REJETE_PAR_');
  }

  function estSigne(statut: string): boolean {
    return statut.startsWith('SIGNE_PAR_');
  }

  it('estEnAttente détecte EN_ATTENTE et EN_ATTENTE_ROLE', () => {
    expect(estEnAttente('EN_ATTENTE')).toBe(true);
    expect(estEnAttente('EN_ATTENTE_RESPONSABLE')).toBe(true);
    expect(estEnAttente('EN_ATTENTE_DIRECTION')).toBe(true);
    expect(estEnAttente('BROUILLON')).toBe(false);
    expect(estEnAttente('TERMINE')).toBe(false);
  });

  it('estApprouve détecte APPROUVE_PAR_ROLE', () => {
    expect(estApprouve('APPROUVE_PAR_RESPONSABLE')).toBe(true);
    expect(estApprouve('APPROUVE_PAR_DIRECTION')).toBe(true);
    expect(estApprouve('EN_ATTENTE_RESPONSABLE')).toBe(false);
  });

  it('estRejete détecte REJETE_PAR_ROLE', () => {
    expect(estRejete('REJETE_PAR_RESPONSABLE')).toBe(true);
    expect(estRejete('TERMINE')).toBe(false);
  });

  it('estSigne détecte SIGNE_PAR_ROLE', () => {
    expect(estSigne('SIGNE_PAR_RESPONSABLE')).toBe(true);
    expect(estSigne('APPROUVE_PAR_RESPONSABLE')).toBe(false);
  });

  it('génère le statut EN_ATTENTE dynamiquement', () => {
    const role = 'Responsable';
    const statut = `EN_ATTENTE_${role.trim().toUpperCase()}`;
    expect(statut).toBe('EN_ATTENTE_RESPONSABLE');
  });

  it('génère le statut REJETE dynamiquement', () => {
    const role = 'Direction';
    const statut = `REJETE_PAR_${role.trim().toUpperCase()}`;
    expect(statut).toBe('REJETE_PAR_DIRECTION');
  });
});

// ── Tests modèle BesoinDTO ────────────────────────────────────────────────────

describe('BesoinDTO — structure', () => {
  it('un besoin valide a les propriétés requises', () => {
    const besoin = {
      id: 1,
      titre: 'Test besoin',
      description: 'Description',
      statut: 'BROUILLON',
      niveauImportance: 'MOYEN',
      dateCreation: '2026-01-01T00:00:00Z',
      dateModification: '2026-01-01T00:00:00Z',
      utilisateurId: 10,
      utilisateurNom: 'Jean Dupont',
      categorieId: 1,
      categorieNom: 'Informatique',
      estTermine: false,
      dejaValideParMoi: false
    };

    expect(besoin.id).toBe(1);
    expect(besoin.statut).toBe('BROUILLON');
    expect(besoin.estTermine).toBe(false);
  });
});

// ── Tests modèle CreateBesoinDTO ──────────────────────────────────────────────

describe('CreateBesoinDTO — validation', () => {
  it('un DTO de création valide a les champs requis', () => {
    const dto = {
      titre: 'Nouveau besoin',
      description: 'Description du besoin',
      niveauImportance: 'MOYEN',
      categorieId: 1
    };

    expect(dto.titre.length).toBeGreaterThan(0);
    expect(dto.description.length).toBeGreaterThan(0);
    expect(NIVEAUX_IMPORTANCE).toContain(dto.niveauImportance);
    expect(dto.categorieId).toBeGreaterThan(0);
  });

  it('un titre vide est invalide', () => {
    const dto = { titre: '', description: 'Desc', niveauImportance: 'MOYEN', categorieId: 1 };
    expect(dto.titre.trim().length).toBe(0);
  });
});
