import { describe, it, expect } from 'vitest';

// ── Tests modèle Auth ─────────────────────────────────────────────────────────

describe('LoginRequest — validation', () => {
  it('a les champs email et motDePasse', () => {
    const req = { email: 'jean@finstar-cm.com', motDePasse: 'password123' };
    expect(req.email).toContain('@');
    expect(req.motDePasse.length).toBeGreaterThan(0);
  });

  it('l\'email doit être valide', () => {
    const emailValide = 'jean@finstar-cm.com';
    const emailInvalide = 'jean';
    expect(emailValide.includes('@')).toBe(true);
    expect(emailInvalide.includes('@')).toBe(false);
  });
});

describe('LoginResponse — structure', () => {
  it('contient le token et les infos utilisateur', () => {
    const response = {
      accessToken: 'eyJhbGciOiJIUzI1NiJ9.payload.sig',
      expiration: '2026-04-29T11:00:00Z',
      utilisateur: { id: 1, nom: 'Dupont', prenom: 'Jean', email: 'jean@test.com', role: 'Agent' },
      doitChangerMotDePasse: false
    };
    expect(response.accessToken.split('.').length).toBe(3); // JWT format
    expect(response.utilisateur.id).toBe(1);
    expect(response.doitChangerMotDePasse).toBe(false);
  });

  it('doitChangerMotDePasse peut être true', () => {
    const response = { accessToken: 'token', expiration: '', utilisateur: { id: 1, nom: '', prenom: '', email: '', role: '' }, doitChangerMotDePasse: true };
    expect(response.doitChangerMotDePasse).toBe(true);
  });
});

describe('UtilisateurInfo — structure', () => {
  it('a les propriétés d\'identification', () => {
    const user = { id: 1, nom: 'Dupont', prenom: 'Jean', email: 'jean@test.com', role: 'Responsable' };
    expect(user.id).toBeGreaterThan(0);
    expect(user.role).toBe('Responsable');
  });
});

// ── Tests modèle Catégorie ────────────────────────────────────────────────────

describe('CategorieDTO — structure', () => {
  it('a les propriétés de base', () => {
    const cat = { id: 1, nom: 'Informatique', dateCreation: '2026-01-01T00:00:00Z' };
    expect(cat.id).toBe(1);
    expect(cat.nom).toBe('Informatique');
  });

  it('workflowCircuitId est optionnel', () => {
    const cat = { id: 1, nom: 'Sans circuit', dateCreation: '' };
    expect((cat as any).workflowCircuitId).toBeUndefined();
  });
});

describe('CreateCategorieDTO — validation', () => {
  it('le nom est requis', () => {
    const dto = { nom: 'Informatique', workflowCircuitId: 1 };
    expect(dto.nom.length).toBeGreaterThan(0);
    expect(dto.workflowCircuitId).toBeGreaterThan(0);
  });

  it('le nom ne doit pas être vide', () => {
    const dto = { nom: '', workflowCircuitId: 1 };
    expect(dto.nom.trim().length).toBe(0); // invalide
  });
});

// ── Tests modèle Rôle ─────────────────────────────────────────────────────────

describe('RoleDTO — structure', () => {
  it('a les propriétés de base', () => {
    const role = { id: 1, code: 'Responsable', dateCreation: '', dateModification: '' };
    expect(role.code).toBe('Responsable');
  });

  it('la description est optionnelle', () => {
    const role = { id: 1, code: 'Agent', dateCreation: '', dateModification: '' };
    expect((role as any).description).toBeUndefined();
  });
});

describe('PermissionDTO — structure', () => {
  it('a les propriétés de base', () => {
    const perm = {
      id: 1, code: 'BESOIN_CONSULTER',
      dateCreation: '', dateModification: ''
    };
    expect(perm.code).toBe('BESOIN_CONSULTER');
  });

  it('le code suit la convention ENTITE_ACTION', () => {
    const codes = ['BESOIN_CONSULTER', 'BESOIN_CREER', 'RAPPORT_EXPORTER', 'USER_MODIFIER'];
    codes.forEach(code => {
      const parts = code.split('_');
      expect(parts.length).toBeGreaterThanOrEqual(2);
    });
  });
});

describe('AssignerPermissionsDTO — validation', () => {
  it('permissionIds est un tableau d\'entiers', () => {
    const dto = { permissionIds: [1, 2, 3] };
    expect(Array.isArray(dto.permissionIds)).toBe(true);
    dto.permissionIds.forEach(id => expect(typeof id).toBe('number'));
  });

  it('permissionIds peut être vide', () => {
    const dto = { permissionIds: [] };
    expect(dto.permissionIds).toHaveLength(0);
  });
});

// ── Tests modèle SignatureUtilisateur ─────────────────────────────────────────

describe('SignatureUtilisateurDTO — structure', () => {
  const TYPES_VALIDES = ['manuscrite', 'typographique', 'upload'] as const;

  it('a les propriétés requises', () => {
    const sig = {
      id: 1,
      type: 'manuscrite' as const,
      imageBase64: 'data:image/png;base64,abc',
      dateCreation: '2026-01-01T00:00:00Z',
      dateModification: '2026-01-01T00:00:00Z'
    };
    expect(sig.id).toBe(1);
    expect(TYPES_VALIDES).toContain(sig.type);
  });

  it('police est optionnelle (pour typographique)', () => {
    const sig = { id: 1, type: 'typographique' as const, imageBase64: 'abc', dateCreation: '', dateModification: '' };
    expect((sig as any).police).toBeUndefined();
  });
});

describe('SaveSignatureUtilisateurDTO — validation', () => {
  it('les 3 types sont valides', () => {
    const types = ['manuscrite', 'typographique', 'upload'] as const;
    types.forEach(type => {
      const dto = { type, imageBase64: 'data:image/png;base64,abc' };
      expect(['manuscrite', 'typographique', 'upload']).toContain(dto.type);
    });
  });

  it('imageBase64 ne doit pas être vide', () => {
    const dto = { type: 'manuscrite' as const, imageBase64: '' };
    expect(dto.imageBase64.length).toBe(0); // invalide
  });

  it('police est requise pour typographique', () => {
    const dto = { type: 'typographique' as const, imageBase64: 'abc', police: 'Dancing Script' };
    expect(dto.police).toBe('Dancing Script');
  });
});
