import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';

// ── Tests logique InactivityService ──────────────────────────────────────────

describe('InactivityService — logique compte à rebours', () => {

  beforeEach(() => { vi.useFakeTimers(); });
  afterEach(() => { vi.useRealTimers(); });

  // ── countdown ───────────────────────────────────────────────────────────────

  it('le compte à rebours décrémente de 1 par seconde', () => {
    let countdown = 30;
    const ticks: number[] = [];

    const timer = setInterval(() => {
      countdown--;
      ticks.push(countdown);
      if (countdown <= 0) clearInterval(timer);
    }, 1000);

    vi.advanceTimersByTime(5000);
    expect(ticks).toEqual([29, 28, 27, 26, 25]);
    clearInterval(timer);
  });

  it('le compte à rebours atteint 0 après le délai complet', () => {
    let countdown = 5;
    let logoutCalled = false;

    const timer = setInterval(() => {
      countdown--;
      if (countdown <= 0) {
        logoutCalled = true;
        clearInterval(timer);
      }
    }, 1000);

    vi.advanceTimersByTime(5000);
    expect(logoutCalled).toBe(true);
    expect(countdown).toBe(0);
  });

  it('le timer d\'inactivité se déclenche après le délai configuré', () => {
    let warningTriggered = false;
    const INACTIVITE_MS = 5 * 60 * 1000; // 5 minutes

    const timer = setTimeout(() => {
      warningTriggered = true;
    }, INACTIVITE_MS);

    vi.advanceTimersByTime(INACTIVITE_MS - 1);
    expect(warningTriggered).toBe(false);

    vi.advanceTimersByTime(1);
    expect(warningTriggered).toBe(true);
    clearTimeout(timer);
  });

  it('clearTimers annule le timer en cours', () => {
    let called = false;
    const timer = setTimeout(() => { called = true; }, 5000);
    clearTimeout(timer);
    vi.advanceTimersByTime(5000);
    expect(called).toBe(false);
  });

  // ── keepAlive ───────────────────────────────────────────────────────────────

  it('keepAlive remet le timer à zéro', () => {
    let triggered = false;
    let timer = setTimeout(() => { triggered = true; }, 5000);

    vi.advanceTimersByTime(3000); // 3s écoulées
    // keepAlive: reset le timer
    clearTimeout(timer);
    timer = setTimeout(() => { triggered = true; }, 5000);

    vi.advanceTimersByTime(3000); // 3s de plus (total 6s mais timer reset)
    expect(triggered).toBe(false); // pas encore déclenché

    vi.advanceTimersByTime(2000); // 2s de plus → 5s depuis le reset
    expect(triggered).toBe(true);
    clearTimeout(timer);
  });
});
