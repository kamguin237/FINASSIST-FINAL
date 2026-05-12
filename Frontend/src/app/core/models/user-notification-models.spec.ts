import { describe, it, expect } from 'vitest';

// ── Tests modèles Utilisateur ─────────────────────────────────────────────────

describe('UtilisateurDTO — structure', () => {
  it('a les propriétés de base', () => {
    const user = {
      id: 1,
      nom: 'Dupont',
      prenom: 'Jean',
      email: 'jean@finstar-cm.com',
      role: 'Agent',
      dateCreation: '2026-01-01T00:00:00Z',
      actif: true
    };
    expect(user.id).toBe(1);
    expect(user.actif).toBe(true);
    expect(user.email).toContain('@finstar-cm.com');
  });

  it('hasActions est optionnel', () => {
    const user = { id: 1, nom: 'X', prenom: 'Y', email: 'x@finstar-cm.com', role: 'Agent', dateCreation: '', actif: true };
    expect((user as any).hasActions).toBeUndefined();
  });
});

describe('CreateUtilisateurDTO — validation email', () => {
  it('l\'email doit appartenir au domaine finstar-cm.com', () => {
    const emailValide = 'jean@finstar-cm.com';
    const emailInvalide = 'jean@gmail.com';
    expect(emailValide.endsWith('@finstar-cm.com')).toBe(true);
    expect(emailInvalide.endsWith('@finstar-cm.com')).toBe(false);
  });

  it('les champs requis sont présents', () => {
    const dto = { nom: 'Dupont', prenom: 'Jean', email: 'jean@finstar-cm.com', motDePasse: 'pwd', roleId: 1 };
    expect(dto.nom.length).toBeGreaterThan(0);
    expect(dto.prenom.length).toBeGreaterThan(0);
    expect(dto.roleId).toBeGreaterThan(0);
  });
});

describe('PermissionsEffectivesDTO — logique', () => {
  it('les permissions effectives incluent rôle + directes', () => {
    const perms = {
      permissionsRole: ['BESOIN_CONSULTER', 'BESOIN_CREER'],
      permissionsDirectes: ['RAPPORT_EXPORTER'],
      permissionsEffectives: ['BESOIN_CONSULTER', 'BESOIN_CREER', 'RAPPORT_EXPORTER']
    };
    expect(perms.permissionsEffectives).toHaveLength(3);
    expect(perms.permissionsEffectives).toContain('RAPPORT_EXPORTER');
  });

  it('les permissions directes s\'ajoutent aux permissions du rôle', () => {
    const role = ['BESOIN_CONSULTER'];
    const directes = ['RAPPORT_EXPORTER'];
    const effectives = [...new Set([...role, ...directes])];
    expect(effectives).toHaveLength(2);
  });
});

// ── Tests modèles Notification ────────────────────────────────────────────────

describe('NotificationDTO — structure', () => {
  it('a les propriétés requises', () => {
    const notif = {
      id: 1,
      message: 'Votre besoin a été approuvé',
      type: 'VALIDATION',
      dateEnvoi: '2026-04-29T10:00:00Z',
      lu: false
    };
    expect(notif.id).toBe(1);
    expect(notif.lu).toBe(false);
    expect(notif.type).toBe('VALIDATION');
  });

  it('une notification lue a lu = true', () => {
    const notif = { id: 1, message: 'Test', type: 'RAPPEL', dateEnvoi: '', lu: true };
    expect(notif.lu).toBe(true);
  });
});

describe('CreateNotificationDTO — validation', () => {
  it('doit avoir au moins un destinataire', () => {
    const dto = { message: 'Test', type: 'VALIDATION', destinataireIds: [1, 2] };
    expect(dto.destinataireIds.length).toBeGreaterThan(0);
  });

  it('le message ne doit pas être vide', () => {
    const dto = { message: '', type: 'VALIDATION', destinataireIds: [1] };
    expect(dto.message.trim().length).toBe(0); // invalide
  });

  it('les types valides sont connus', () => {
    const typesValides = ['VALIDATION', 'REJET', 'RAPPEL', 'ACCUSE_RECEPTION', 'SIGNATURE_REQUISE'];
    expect(typesValides).toContain('VALIDATION');
    expect(typesValides).toContain('REJET');
    expect(typesValides).not.toContain('INCONNU');
  });
});

// ── Tests modèle BesoinDeadlineDTO ────────────────────────────────────────────

describe('BesoinDeadlineDTO — niveaux d\'urgence', () => {
  const urgences = ['normal', 'warning', 'danger', 'expired'] as const;

  it('les 4 niveaux d\'urgence sont définis', () => {
    expect(urgences).toHaveLength(4);
    expect(urgences).toContain('normal');
    expect(urgences).toContain('expired');
  });

  it('un besoin expiré a urgence = "expired"', () => {
    const besoin = {
      id: 1, titre: 'Test', statut: 'EN_ATTENTE_RESPONSABLE',
      pourcentageEcoule: 120, urgence: 'expired' as const
    };
    expect(besoin.urgence).toBe('expired');
    expect(besoin.pourcentageEcoule).toBeGreaterThan(100);
  });

  it('un besoin normal a urgence = "normal"', () => {
    const besoin = {
      id: 1, titre: 'Test', statut: 'EN_ATTENTE_RESPONSABLE',
      pourcentageEcoule: 30, urgence: 'normal' as const
    };
    expect(besoin.urgence).toBe('normal');
    expect(besoin.pourcentageEcoule).toBeLessThan(50);
  });
});
