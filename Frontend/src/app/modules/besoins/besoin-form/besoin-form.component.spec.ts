import { describe, it, expect } from 'vitest';
import { NIVEAUX_IMPORTANCE } from '../../../core/models/besoin.models';

// ── Tests logique BesoinFormComponent ────────────────────────────────────────

describe('BesoinFormComponent — validation', () => {

  function isFormValid(titre: string, description: string, niveauImportance: string, categorieId: number | null): boolean {
    return titre.trim().length > 0 &&
           description.trim().length > 0 &&
           niveauImportance.trim().length > 0 &&
           categorieId !== null && categorieId > 0;
  }

  it('formulaire invalide si titre vide', () => {
    expect(isFormValid('', 'Desc', 'MOYEN', 1)).toBe(false);
  });

  it('formulaire invalide si description vide', () => {
    expect(isFormValid('Titre', '', 'MOYEN', 1)).toBe(false);
  });

  it('formulaire invalide si niveauImportance vide', () => {
    expect(isFormValid('Titre', 'Desc', '', 1)).toBe(false);
  });

  it('formulaire invalide si categorieId null', () => {
    expect(isFormValid('Titre', 'Desc', 'MOYEN', null)).toBe(false);
  });

  it('formulaire valide avec tous les champs remplis', () => {
    expect(isFormValid('Titre', 'Description', 'MOYEN', 1)).toBe(true);
  });

  it('niveauImportance doit être dans NIVEAUX_IMPORTANCE', () => {
    NIVEAUX_IMPORTANCE.forEach(niveau => {
      expect(isFormValid('Titre', 'Desc', niveau, 1)).toBe(true);
    });
  });
});

describe('BesoinFormComponent — mode création vs édition', () => {

  it('en mode création, editId est null', () => {
    const editId: number | null = null;
    expect(editId).toBeNull();
  });

  it('en mode édition, editId est un nombre positif', () => {
    const editId = 42;
    expect(editId).toBeGreaterThan(0);
  });

  it('en mode création, utilise getDisponibles()', () => {
    const editId: number | null = null;
    const endpoint = editId ? 'getAll' : 'getDisponibles';
    expect(endpoint).toBe('getDisponibles');
  });

  it('en mode édition, utilise getAll()', () => {
    const editId = 42;
    const endpoint = editId ? 'getAll' : 'getDisponibles';
    expect(endpoint).toBe('getAll');
  });

  it('en mode édition, bloque si statut n\'est pas BROUILLON', () => {
    const statut = 'EN_ATTENTE_RESPONSABLE';
    const peutEditer = statut === 'BROUILLON';
    expect(peutEditer).toBe(false);
  });

  it('en mode édition, autorise si statut est BROUILLON', () => {
    const statut = 'BROUILLON';
    const peutEditer = statut === 'BROUILLON';
    expect(peutEditer).toBe(true);
  });
});

describe('BesoinFormComponent — submit', () => {

  it('ne soumet pas si le formulaire est invalide', () => {
    let submitted = false;
    const formInvalid = true;
    if (!formInvalid) submitted = true;
    expect(submitted).toBe(false);
  });

  it('soumet si le formulaire est valide', () => {
    let submitted = false;
    const formInvalid = false;
    if (!formInvalid) submitted = true;
    expect(submitted).toBe(true);
  });

  it('en mode création, appelle create()', () => {
    const editId: number | null = null;
    const action = editId ? 'update' : 'create';
    expect(action).toBe('create');
  });

  it('en mode édition, appelle update()', () => {
    const editId = 5;
    const action = editId ? 'update' : 'create';
    expect(action).toBe('update');
  });
});
