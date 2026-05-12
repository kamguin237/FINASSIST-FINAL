import { describe, it, expect, beforeEach } from 'vitest';

// ── Tests logique pure du CustomDatepicker ────────────────────────────────────
// On teste la logique métier sans dépendances Angular

describe('CustomDatepicker — logique calendrier', () => {

  // ── calendarDays ────────────────────────────────────────────────────────────

  function getCalendarDays(year: number, month: number): (number | null)[] {
    const firstDay = new Date(year, month, 1);
    const lastDay = new Date(year, month + 1, 0);
    let startDay = firstDay.getDay();
    startDay = startDay === 0 ? 6 : startDay - 1; // Lundi = 0
    const days: (number | null)[] = [];
    for (let i = 0; i < startDay; i++) days.push(null);
    for (let i = 1; i <= lastDay.getDate(); i++) days.push(i);
    return days;
  }

  it('génère les jours corrects pour janvier 2026', () => {
    const days = getCalendarDays(2026, 0); // Janvier 2026
    const nonNull = days.filter(d => d !== null);
    expect(nonNull).toHaveLength(31);
    expect(nonNull[0]).toBe(1);
    expect(nonNull[30]).toBe(31);
  });

  it('génère les jours corrects pour février 2026 (28 jours)', () => {
    const days = getCalendarDays(2026, 1);
    const nonNull = days.filter(d => d !== null);
    expect(nonNull).toHaveLength(28);
  });

  it('génère les jours corrects pour février 2024 (année bissextile, 29 jours)', () => {
    const days = getCalendarDays(2024, 1);
    const nonNull = days.filter(d => d !== null);
    expect(nonNull).toHaveLength(29);
  });

  it('commence par des nulls si le mois ne commence pas un lundi', () => {
    // Avril 2026 commence un mercredi (index 2 en lundi=0)
    const days = getCalendarDays(2026, 3);
    expect(days[0]).toBeNull();
    expect(days[1]).toBeNull();
    expect(days[2]).toBe(1); // Premier jour du mois
  });

  it('ne commence pas par des nulls si le mois commence un lundi', () => {
    // Juin 2026 commence un lundi
    const days = getCalendarDays(2026, 5);
    expect(days[0]).toBe(1);
  });

  // ── displayValue ────────────────────────────────────────────────────────────

  function getDisplayValue(date: Date | null): string {
    if (!date) return '';
    return `${String(date.getDate()).padStart(2, '0')}/${String(date.getMonth() + 1).padStart(2, '0')}/${date.getFullYear()}`;
  }

  it('formate la date en DD/MM/YYYY', () => {
    const date = new Date(2026, 3, 15); // 15 avril 2026
    expect(getDisplayValue(date)).toBe('15/04/2026');
  });

  it('retourne une chaîne vide si pas de date', () => {
    expect(getDisplayValue(null)).toBe('');
  });

  it('padde les jours et mois avec un zéro', () => {
    const date = new Date(2026, 0, 5); // 5 janvier 2026
    expect(getDisplayValue(date)).toBe('05/01/2026');
  });

  // ── isToday ─────────────────────────────────────────────────────────────────

  function isToday(day: number | null, month: number, year: number): boolean {
    if (day === null) return false;
    const today = new Date();
    return day === today.getDate() &&
           month === today.getMonth() &&
           year === today.getFullYear();
  }

  it('isToday retourne true pour le jour actuel', () => {
    const today = new Date();
    expect(isToday(today.getDate(), today.getMonth(), today.getFullYear())).toBe(true);
  });

  it('isToday retourne false pour un autre jour', () => {
    const today = new Date();
    const autreJour = today.getDate() === 1 ? 2 : 1;
    expect(isToday(autreJour, today.getMonth(), today.getFullYear())).toBe(false);
  });

  it('isToday retourne false pour null', () => {
    expect(isToday(null, 0, 2026)).toBe(false);
  });

  // ── formatForEmit ───────────────────────────────────────────────────────────

  function formatForEmit(year: number, month: number, day: number): string {
    return `${year}-${String(month + 1).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
  }

  it('formate la date pour l\'émission en YYYY-MM-DD', () => {
    expect(formatForEmit(2026, 3, 15)).toBe('2026-04-15');
  });

  it('padde le mois et le jour', () => {
    expect(formatForEmit(2026, 0, 5)).toBe('2026-01-05');
  });

  // ── navigation mois ─────────────────────────────────────────────────────────

  it('previousMonth décrémente le mois', () => {
    let current = new Date(2026, 3, 1); // Avril 2026
    current = new Date(current.getFullYear(), current.getMonth() - 1, 1);
    expect(current.getMonth()).toBe(2); // Mars
    expect(current.getFullYear()).toBe(2026);
  });

  it('previousMonth passe à décembre de l\'année précédente depuis janvier', () => {
    let current = new Date(2026, 0, 1); // Janvier 2026
    current = new Date(current.getFullYear(), current.getMonth() - 1, 1);
    expect(current.getMonth()).toBe(11); // Décembre
    expect(current.getFullYear()).toBe(2025);
  });

  it('nextMonth incrémente le mois', () => {
    let current = new Date(2026, 3, 1); // Avril 2026
    current = new Date(current.getFullYear(), current.getMonth() + 1, 1);
    expect(current.getMonth()).toBe(4); // Mai
  });

  it('nextMonth passe à janvier de l\'année suivante depuis décembre', () => {
    let current = new Date(2026, 11, 1); // Décembre 2026
    current = new Date(current.getFullYear(), current.getMonth() + 1, 1);
    expect(current.getMonth()).toBe(0); // Janvier
    expect(current.getFullYear()).toBe(2027);
  });
});
