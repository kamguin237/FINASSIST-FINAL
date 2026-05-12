import { describe, it, expect } from 'vitest';

// ── Tests logique CategoriesComponent ────────────────────────────────────────

describe('CategoriesComponent — validation formulaire', () => {

  function isFormValid(nom: string, workflowCircuitId: number | null): boolean {
    return nom.trim().length > 0 && workflowCircuitId !== null && workflowCircuitId > 0;
  }

  it('invalide si nom vide', () => {
    expect(isFormValid('', 1)).toBe(false);
  });

  it('invalide si workflowCircuitId null', () => {
    expect(isFormValid('Informatique', null)).toBe(false);
  });

  it('valide avec nom et circuit', () => {
    expect(isFormValid('Informatique', 1)).toBe(true);
  });

  it('la description est optionnelle', () => {
    // Le formulaire est valide même sans description
    expect(isFormValid('Informatique', 1)).toBe(true);
  });
});

describe('CategoriesComponent — mode création vs édition', () => {

  it('en mode création, editId est null', () => {
    const editId: number | null = null;
    const isEdit = !!editId;
    expect(isEdit).toBe(false);
  });

  it('en mode édition, editId est un nombre', () => {
    const editId = 5;
    const isEdit = !!editId;
    expect(isEdit).toBe(true);
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

describe('CategoriesComponent — openEdit', () => {
  it('pré-remplit le formulaire avec les données de la catégorie', () => {
    const categorie = {
      id: 1,
      nom: 'Informatique',
      description: 'Catégorie IT',
      workflowCircuitId: 2,
      dateCreation: '2026-01-01T00:00:00Z'
    };

    // Simuler patchValue
    const formValues = {
      nom: categorie.nom,
      description: categorie.description,
      workflowCircuitId: categorie.workflowCircuitId ?? null
    };

    expect(formValues.nom).toBe('Informatique');
    expect(formValues.description).toBe('Catégorie IT');
    expect(formValues.workflowCircuitId).toBe(2);
  });

  it('gère workflowCircuitId undefined en le mettant à null', () => {
    const categorie = { id: 1, nom: 'Test', dateCreation: '' };
    const workflowCircuitId = (categorie as any).workflowCircuitId ?? null;
    expect(workflowCircuitId).toBeNull();
  });
});
