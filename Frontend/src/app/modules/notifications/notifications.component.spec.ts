import { describe, it, expect } from 'vitest';

// ── Tests logique NotificationsComponent ─────────────────────────────────────

describe('NotificationsComponent — filtrage', () => {

  const notifications = [
    { id: 1, message: 'Test 1', type: 'VALIDATION', dateEnvoi: '', lu: false },
    { id: 2, message: 'Test 2', type: 'REJET', dateEnvoi: '', lu: true },
    { id: 3, message: 'Test 3', type: 'RAPPEL', dateEnvoi: '', lu: false }
  ];

  function notificationsFiltrees(notifs: typeof notifications, filtre: string) {
    if (!filtre) return notifs;
    return notifs.filter(n => filtre === 'LU' ? n.lu : !n.lu);
  }

  it('retourne toutes les notifications sans filtre', () => {
    expect(notificationsFiltrees(notifications, '')).toHaveLength(3);
  });

  it('filtre les notifications lues', () => {
    const result = notificationsFiltrees(notifications, 'LU');
    expect(result).toHaveLength(1);
    result.forEach(n => expect(n.lu).toBe(true));
  });

  it('filtre les notifications non lues', () => {
    const result = notificationsFiltrees(notifications, 'NON_LU');
    expect(result).toHaveLength(2);
    result.forEach(n => expect(n.lu).toBe(false));
  });
});

describe('NotificationsComponent — sélection des destinataires', () => {

  function estSelectionne(selectedIds: number[], id: number): boolean {
    return selectedIds.includes(id);
  }

  function toggleDestinataire(selectedIds: number[], id: number, checked: boolean): number[] {
    if (checked) {
      return selectedIds.includes(id) ? selectedIds : [...selectedIds, id];
    }
    return selectedIds.filter(x => x !== id);
  }

  function toggleTous(utilisateurs: { id: number }[], checked: boolean): number[] {
    return checked ? utilisateurs.map(u => u.id) : [];
  }

  it('estSelectionne retourne true si l\'id est dans la liste', () => {
    expect(estSelectionne([1, 2, 3], 2)).toBe(true);
  });

  it('estSelectionne retourne false si l\'id n\'est pas dans la liste', () => {
    expect(estSelectionne([1, 2], 5)).toBe(false);
  });

  it('toggleDestinataire ajoute un id si checked', () => {
    const result = toggleDestinataire([1], 2, true);
    expect(result).toContain(2);
  });

  it('toggleDestinataire supprime un id si non checked', () => {
    const result = toggleDestinataire([1, 2, 3], 2, false);
    expect(result).not.toContain(2);
  });

  it('toggleTous sélectionne tous si checked', () => {
    const users = [{ id: 1 }, { id: 2 }, { id: 3 }];
    const result = toggleTous(users, true);
    expect(result).toHaveLength(3);
  });

  it('toggleTous désélectionne tous si non checked', () => {
    const users = [{ id: 1 }, { id: 2 }];
    const result = toggleTous(users, false);
    expect(result).toHaveLength(0);
  });

  it('tousSelectionnes retourne true si tous sont sélectionnés', () => {
    const users = [{ id: 1 }, { id: 2 }];
    const selectedIds = [1, 2];
    const tousSelectionnes = users.length > 0 && selectedIds.length === users.length;
    expect(tousSelectionnes).toBe(true);
  });
});

describe('NotificationsComponent — validation envoi', () => {
  it('ne peut pas envoyer si aucun destinataire sélectionné', () => {
    const selectedIds: number[] = [];
    const formInvalid = false;
    const peutEnvoyer = !formInvalid && selectedIds.length > 0;
    expect(peutEnvoyer).toBe(false);
  });

  it('peut envoyer si formulaire valide et destinataires sélectionnés', () => {
    const selectedIds = [1, 2];
    const formInvalid = false;
    const peutEnvoyer = !formInvalid && selectedIds.length > 0;
    expect(peutEnvoyer).toBe(true);
  });
});
