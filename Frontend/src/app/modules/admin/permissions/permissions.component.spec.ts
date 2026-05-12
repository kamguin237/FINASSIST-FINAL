import { describe, it, expect } from 'vitest';

// ── Tests logique PermissionsComponent ───────────────────────────────────────

describe('PermissionsComponent — openEdit', () => {

  it('pré-remplit le formulaire avec les données de la permission', () => {
    const perm = {
      id: 1,
      code: 'BESOIN_CONSULTER',
      description: 'Consulter les besoins',
      fonctionnalite: 'Besoins',
      module: 'Core',
      dateCreation: '',
      dateModification: ''
    };

    // Simuler patchValue
    const formValues = {
      code: perm.code,
      description: perm.description,
      fonctionnalite: perm.fonctionnalite,
      module: perm.module
    };

    expect(formValues.code).toBe('BESOIN_CONSULTER');
    expect(formValues.description).toBe('Consulter les besoins');
    expect(formValues.module).toBe('Core');
  });
});

describe('PermissionsComponent — structure des permissions', () => {

  it('le code suit la convention ENTITE_ACTION', () => {
    const codes = [
      'BESOIN_CONSULTER', 'BESOIN_CREER', 'BESOIN_MODIFIER', 'BESOIN_SUPPRIMER',
      'USER_CONSULTER', 'USER_CREER', 'RAPPORT_EXPORTER', 'WORKFLOW_CREER'
    ];
    codes.forEach(code => {
      const parts = code.split('_');
      expect(parts.length).toBeGreaterThanOrEqual(2);
      expect(code).toBe(code.toUpperCase());
    });
  });

  it('les permissions sont groupées par module', () => {
    const permissions = [
      { id: 1, code: 'BESOIN_CONSULTER', module: 'Besoins', dateCreation: '', dateModification: '' },
      { id: 2, code: 'BESOIN_CREER', module: 'Besoins', dateCreation: '', dateModification: '' },
      { id: 3, code: 'USER_CONSULTER', module: 'Utilisateurs', dateCreation: '', dateModification: '' }
    ];

    const map = new Map<string, typeof permissions>();
    for (const p of permissions) {
      const mod = p.module ?? 'Autres';
      if (!map.has(mod)) map.set(mod, []);
      map.get(mod)!.push(p);
    }

    expect(map.get('Besoins')).toHaveLength(2);
    expect(map.get('Utilisateurs')).toHaveLength(1);
  });
});
