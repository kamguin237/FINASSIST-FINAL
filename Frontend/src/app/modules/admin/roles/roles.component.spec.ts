import { describe, it, expect } from 'vitest';

// ── Tests logique RolesComponent ─────────────────────────────────────────────

describe('RolesComponent — gestion des permissions', () => {

  // Simulation de la logique togglePerm
  function createPermSet(initial: number[] = []): Set<number> {
    return new Set(initial);
  }

  function togglePerm(set: Set<number>, permId: number, checked: boolean): Set<number> {
    const newSet = new Set(set);
    if (checked) newSet.add(permId);
    else newSet.delete(permId);
    return newSet;
  }

  function hasPerm(set: Set<number>, permId: number): boolean {
    return set.has(permId);
  }

  it('hasPerm retourne true si la permission est présente', () => {
    const set = createPermSet([1, 2, 3]);
    expect(hasPerm(set, 2)).toBe(true);
  });

  it('hasPerm retourne false si la permission est absente', () => {
    const set = createPermSet([1, 2]);
    expect(hasPerm(set, 5)).toBe(false);
  });

  it('togglePerm ajoute une permission si checked = true', () => {
    const set = createPermSet([1]);
    const newSet = togglePerm(set, 5, true);
    expect(hasPerm(newSet, 5)).toBe(true);
  });

  it('togglePerm supprime une permission si checked = false', () => {
    const set = createPermSet([1, 5]);
    const newSet = togglePerm(set, 5, false);
    expect(hasPerm(newSet, 5)).toBe(false);
  });

  it('togglePerm ne modifie pas les autres permissions', () => {
    const set = createPermSet([1, 2, 3]);
    const newSet = togglePerm(set, 2, false);
    expect(hasPerm(newSet, 1)).toBe(true);
    expect(hasPerm(newSet, 3)).toBe(true);
    expect(hasPerm(newSet, 2)).toBe(false);
  });
});

describe('RolesComponent — isModuleAllChecked', () => {

  function isModuleAllChecked(permissions: { id: number }[], set: Set<number>): boolean {
    return permissions.every(p => set.has(p.id));
  }

  function toggleModule(permissions: { id: number }[], set: Set<number>): Set<number> {
    const newSet = new Set(set);
    if (isModuleAllChecked(permissions, set)) {
      permissions.forEach(p => newSet.delete(p.id));
    } else {
      permissions.forEach(p => newSet.add(p.id));
    }
    return newSet;
  }

  const perms = [{ id: 1 }, { id: 2 }, { id: 3 }];

  it('retourne true si toutes les permissions du module sont cochées', () => {
    const set = new Set([1, 2, 3]);
    expect(isModuleAllChecked(perms, set)).toBe(true);
  });

  it('retourne false si une permission manque', () => {
    const set = new Set([1, 2]);
    expect(isModuleAllChecked(perms, set)).toBe(false);
  });

  it('toggleModule coche toutes si aucune n\'est cochée', () => {
    const set = new Set<number>();
    const newSet = toggleModule(perms, set);
    expect(newSet.has(1)).toBe(true);
    expect(newSet.has(2)).toBe(true);
    expect(newSet.has(3)).toBe(true);
  });

  it('toggleModule décoche toutes si toutes sont cochées', () => {
    const set = new Set([1, 2, 3]);
    const newSet = toggleModule(perms, set);
    expect(newSet.has(1)).toBe(false);
    expect(newSet.has(2)).toBe(false);
    expect(newSet.has(3)).toBe(false);
  });
});

describe('RolesComponent — permissionsByModule', () => {

  function groupByModule(permissions: { id: number; module?: string }[]) {
    const map = new Map<string, typeof permissions>();
    for (const p of permissions) {
      const mod = p.module ?? 'Autres';
      if (!map.has(mod)) map.set(mod, []);
      map.get(mod)!.push(p);
    }
    return Array.from(map.entries()).map(([module, perms]) => ({ module, permissions: perms }));
  }

  it('groupe les permissions par module', () => {
    const perms = [
      { id: 1, module: 'Besoins' },
      { id: 2, module: 'Besoins' },
      { id: 3, module: 'Reporting' }
    ];
    const grouped = groupByModule(perms);
    expect(grouped).toHaveLength(2);
    expect(grouped.find(g => g.module === 'Besoins')?.permissions).toHaveLength(2);
  });

  it('utilise "Autres" si module est undefined', () => {
    const perms = [{ id: 1 }];
    const grouped = groupByModule(perms);
    expect(grouped[0].module).toBe('Autres');
  });
});
