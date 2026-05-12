import { describe, it, expect, beforeEach } from 'vitest';

// ── Tests ConfirmService — logique pure ───────────────────────────────────────

// Simulation du service sans DI Angular
function createConfirmService() {
  let visible = false;
  let options = { message: '' };
  let resolveFn: ((v: boolean) => void) | null = null;

  return {
    get visible() { return visible; },
    get options() { return options; },

    confirm(opts: { titre?: string; message: string; labelConfirm?: string; labelCancel?: string; danger?: boolean }): Promise<boolean> {
      options = { labelConfirm: 'Confirmer', labelCancel: 'Annuler', danger: true, ...opts };
      visible = true;
      return new Promise(resolve => { resolveFn = resolve; });
    },

    respond(value: boolean) {
      visible = false;
      resolveFn?.(value);
      resolveFn = null;
    }
  };
}

describe('ConfirmService — logique de confirmation', () => {
  let service: ReturnType<typeof createConfirmService>;

  beforeEach(() => {
    service = createConfirmService();
  });

  // ── confirm ─────────────────────────────────────────────────────────────────

  it('confirm rend le dialog visible', async () => {
    service.confirm({ message: 'Êtes-vous sûr ?' });
    expect(service.visible).toBe(true);
  });

  it('confirm stocke les options avec les valeurs par défaut', async () => {
    service.confirm({ message: 'Supprimer ?' });
    expect((service.options as any).labelConfirm).toBe('Confirmer');
    expect((service.options as any).labelCancel).toBe('Annuler');
    expect((service.options as any).danger).toBe(true);
  });

  it('confirm permet de surcharger les options par défaut', async () => {
    service.confirm({ message: 'Test', labelConfirm: 'Oui', labelCancel: 'Non', danger: false });
    expect((service.options as any).labelConfirm).toBe('Oui');
    expect((service.options as any).labelCancel).toBe('Non');
    expect((service.options as any).danger).toBe(false);
  });

  it('confirm retourne une Promise', () => {
    const result = service.confirm({ message: 'Test' });
    expect(result).toBeInstanceOf(Promise);
  });

  // ── respond ─────────────────────────────────────────────────────────────────

  it('respond(true) résout la Promise avec true', async () => {
    const promise = service.confirm({ message: 'Test' });
    service.respond(true);
    const result = await promise;
    expect(result).toBe(true);
  });

  it('respond(false) résout la Promise avec false', async () => {
    const promise = service.confirm({ message: 'Test' });
    service.respond(false);
    const result = await promise;
    expect(result).toBe(false);
  });

  it('respond cache le dialog', async () => {
    service.confirm({ message: 'Test' });
    expect(service.visible).toBe(true);
    service.respond(true);
    expect(service.visible).toBe(false);
  });

  it('respond remet resolveFn à null après résolution', async () => {
    const promise = service.confirm({ message: 'Test' });
    service.respond(true);
    await promise;
    // Un second respond ne doit pas lever d'erreur
    expect(() => service.respond(false)).not.toThrow();
  });

  // ── scénarios d'usage ────────────────────────────────────────────────────────

  it('scénario complet: confirmer une suppression', async () => {
    const promise = service.confirm({
      titre: 'Supprimer',
      message: 'Voulez-vous supprimer cet élément ?',
      labelConfirm: 'Supprimer',
      danger: true
    });

    expect(service.visible).toBe(true);
    expect((service.options as any).message).toBe('Voulez-vous supprimer cet élément ?');

    service.respond(true);
    const confirmed = await promise;
    expect(confirmed).toBe(true);
    expect(service.visible).toBe(false);
  });

  it('scénario complet: annuler une action', async () => {
    const promise = service.confirm({ message: 'Continuer ?' });
    service.respond(false);
    const confirmed = await promise;
    expect(confirmed).toBe(false);
  });

  it('plusieurs confirmations successives fonctionnent', async () => {
    const p1 = service.confirm({ message: 'Premier' });
    service.respond(true);
    const r1 = await p1;

    const p2 = service.confirm({ message: 'Deuxième' });
    service.respond(false);
    const r2 = await p2;

    expect(r1).toBe(true);
    expect(r2).toBe(false);
  });
});
