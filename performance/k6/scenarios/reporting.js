/**
 * Scénario de performance — Reporting & Dashboard
 *
 * Teste les endpoints les plus lourds en DB :
 *   GET /api/reporting/dashboard
 *   GET /api/reporting/dashboard/evolution?periode=jours
 *   GET /api/reporting/statistiques
 *   GET /api/reporting/besoins
 *   POST /api/reporting/exporter (Excel)
 *
 * Objectif : ces endpoints agrègent beaucoup de données.
 * Vérifier qu'ils restent sous 1s même sous charge.
 */

import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { Trend, Rate } from 'k6/metrics';
import { BASE_URL, LOAD_OPTIONS } from '../config.js';
import { getToken, authHeaders } from '../helpers/auth-helper.js';

// ── Métriques personnalisées ──────────────────────────────────────────────────
const dashboardDuration   = new Trend('finassist_dashboard_duration',    true);
const statistiquesDuration = new Trend('finassist_statistiques_duration', true);
const evolutionDuration   = new Trend('finassist_evolution_duration',    true);
const exportDuration      = new Trend('finassist_export_duration',       true);
const errorRate           = new Rate('finassist_reporting_error_rate');

// ── Options du test ───────────────────────────────────────────────────────────
export const options = {
  ...LOAD_OPTIONS,
  thresholds: {
    ...LOAD_OPTIONS.thresholds,
    // Le dashboard est la page d'accueil — seuil strict
    'finassist_dashboard_duration':    ['p(95)<800'],
    'finassist_statistiques_duration': ['p(95)<1000'],
    'finassist_evolution_duration':    ['p(95)<800'],
    // L'export peut être plus lent
    'finassist_export_duration':       ['p(95)<3000'],
    'finassist_reporting_error_rate':  ['rate<0.01'],
  }
};

// ── Setup ─────────────────────────────────────────────────────────────────────
export function setup() {
  return {
    tokenAdmin: getToken('admin'),
    tokenAgent: getToken('agent'),
  };
}

// ── Scénario principal ────────────────────────────────────────────────────────
export default function (data) {

  // ── 1. Dashboard ─────────────────────────────────────────────────────────
  group('GET /api/reporting/dashboard', () => {
    const res = http.get(`${BASE_URL}/reporting/dashboard`, {
      headers: authHeaders(data.tokenAdmin),
      tags: { name: 'reporting/dashboard' }
    });

    dashboardDuration.add(res.timings.duration);

    const ok = check(res, {
      'dashboard — status 200':              r => r.status === 200,
      'dashboard — besoinsEnAttente présent': r => {
        try { return 'besoinsEnAttente' in JSON.parse(r.body); }
        catch { return false; }
      },
      'dashboard — < 800ms':                 r => r.timings.duration < 800,
    });
    errorRate.add(!ok);
  });

  sleep(0.5);

  // ── 2. Évolution des besoins ──────────────────────────────────────────────
  const periodes = ['jours', 'semaines', 'mois'];
  const periode  = periodes[Math.floor(Math.random() * periodes.length)];

  group(`GET /api/reporting/dashboard/evolution?periode=${periode}`, () => {
    const res = http.get(
      `${BASE_URL}/reporting/dashboard/evolution?periode=${periode}`,
      {
        headers: authHeaders(data.tokenAdmin),
        tags: { name: 'reporting/evolution' }
      }
    );

    evolutionDuration.add(res.timings.duration);

    check(res, {
      'évolution — status 200':  r => r.status === 200,
      'évolution — body array':  r => {
        try { return Array.isArray(JSON.parse(r.body)); }
        catch { return false; }
      },
      'évolution — < 800ms':     r => r.timings.duration < 800,
    });
  });

  sleep(0.5);

  // ── 3. Statistiques globales ──────────────────────────────────────────────
  group('GET /api/reporting/statistiques', () => {
    const res = http.get(`${BASE_URL}/reporting/statistiques`, {
      headers: authHeaders(data.tokenAdmin),
      tags: { name: 'reporting/statistiques' }
    });

    statistiquesDuration.add(res.timings.duration);

    check(res, {
      'statistiques — status 200':         r => r.status === 200,
      'statistiques — totalBesoins présent': r => {
        try { return 'totalBesoins' in JSON.parse(r.body); }
        catch { return false; }
      },
      'statistiques — < 1000ms':           r => r.timings.duration < 1000,
    });
  });

  sleep(0.5);

  // ── 4. Rapport besoins avec filtres ──────────────────────────────────────
  group('GET /api/reporting/besoins', () => {
    const res = http.get(`${BASE_URL}/reporting/besoins`, {
      headers: authHeaders(data.tokenAdmin),
      tags: { name: 'reporting/rapport' }
    });

    check(res, {
      'rapport — status 200': r => r.status === 200,
      'rapport — < 1000ms':   r => r.timings.duration < 1000,
    });
  });

  sleep(0.5);

  // ── 5. Export Excel (1 fois sur 5 pour ne pas surcharger) ────────────────
  if (Math.random() < 0.2) {
    group('POST /api/reporting/exporter (Excel)', () => {
      const res = http.post(
        `${BASE_URL}/reporting/exporter`,
        JSON.stringify({ format: 'EXCEL', filtres: {} }),
        {
          headers: authHeaders(data.tokenAdmin),
          tags: { name: 'reporting/export-excel' }
        }
      );

      exportDuration.add(res.timings.duration);

      check(res, {
        'export Excel — status 200': r => r.status === 200,
        'export Excel — < 3000ms':   r => r.timings.duration < 3000,
      });
    });
  }

  sleep(1);
}
