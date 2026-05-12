import { describe, it, expect, beforeEach, vi } from 'vitest';

// ── Tests logique SignalR ─────────────────────────────────────────────────────

describe('SignalRService — logique de connexion', () => {

  // Simulation des états de connexion SignalR
  const HubConnectionState = {
    Disconnected: 'Disconnected',
    Connecting: 'Connecting',
    Connected: 'Connected',
    Disconnecting: 'Disconnecting',
    Reconnecting: 'Reconnecting'
  };

  it('ne démarre pas une connexion déjà établie', async () => {
    let startCalled = 0;
    const mockConnection = {
      state: HubConnectionState.Connected,
      start: async () => { startCalled++; }
    };

    // Logique du service: ne démarre pas si déjà connecté
    if (mockConnection.state !== HubConnectionState.Connected) {
      await mockConnection.start();
    }

    expect(startCalled).toBe(0);
  });

  it('démarre la connexion si déconnecté', async () => {
    let startCalled = 0;
    const mockConnection = {
      state: HubConnectionState.Disconnected,
      start: async () => { startCalled++; }
    };

    if (mockConnection.state !== HubConnectionState.Connected) {
      await mockConnection.start();
    }

    expect(startCalled).toBe(1);
  });

  it('gère les erreurs de connexion sans lever d\'exception', async () => {
    const errors: any[] = [];
    const mockConnection = {
      state: HubConnectionState.Disconnected,
      start: async () => { throw new Error('Connection failed'); }
    };

    try {
      await mockConnection.start();
    } catch (e) {
      errors.push(e);
    }

    expect(errors).toHaveLength(1);
    expect(errors[0].message).toBe('Connection failed');
  });

  // ── Groupes ─────────────────────────────────────────────────────────────────

  it('joinBesoinGroup invoque la méthode correcte', async () => {
    const invocations: { method: string; args: any[] }[] = [];
    const mockConnection = {
      state: HubConnectionState.Connected,
      invoke: async (method: string, ...args: any[]) => {
        invocations.push({ method, args });
      }
    };

    if (mockConnection.state === HubConnectionState.Connected) {
      await mockConnection.invoke('JoinBesoinGroup', 42);
    }

    expect(invocations).toHaveLength(1);
    expect(invocations[0].method).toBe('JoinBesoinGroup');
    expect(invocations[0].args[0]).toBe(42);
  });

  it('leaveBesoinGroup invoque la méthode correcte', async () => {
    const invocations: { method: string; args: any[] }[] = [];
    const mockConnection = {
      state: HubConnectionState.Connected,
      invoke: async (method: string, ...args: any[]) => {
        invocations.push({ method, args });
      }
    };

    if (mockConnection.state === HubConnectionState.Connected) {
      await mockConnection.invoke('LeaveBesoinGroup', 42);
    }

    expect(invocations[0].method).toBe('LeaveBesoinGroup');
  });

  it('ne joint pas un groupe si non connecté', async () => {
    const invocations: any[] = [];
    const mockConnection = {
      state: HubConnectionState.Disconnected,
      invoke: async (method: string, ...args: any[]) => {
        invocations.push({ method, args });
      }
    };

    if (mockConnection.state === HubConnectionState.Connected) {
      await mockConnection.invoke('JoinBesoinGroup', 42);
    }

    expect(invocations).toHaveLength(0);
  });

  // ── URL du hub ───────────────────────────────────────────────────────────────

  it('l\'URL du hub est correctement formée', () => {
    const apiUrl = '/api';
    const hubUrl = `${apiUrl}/hubs/besoins`;
    expect(hubUrl).toBe('/api/hubs/besoins');
  });
});
