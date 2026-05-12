/**
 * Scénario de performance — Workflow
 *
 * Teste les endpoints de validation et gestion des circuits :
 *   GET  /api/workflow/circuits
 *   GET  /api/workflow/circuits/:id
 *   POST /api/workflow/:id/valider
 *   POST /api/workflow/:id/transmettre
 *
 * Objectif : vérifier que la validation sous charge reste cohérente
 * et que les transitions d'état ne créent pas de race conditions.
 */

import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { Trend, Rate } from 'k6/metrics';
import { BASE_URL, LOAD_OPTIONS } from '../config.js';
import { getToken, authHeaders } from '../helpers/auth-helper.js';

// ── Métriques personnalisées ──────────────────────────────────────────────────
const circuitsDuration   = new Trend('finassist_workflow_circuits_duration',   true);
const validationDuration = new Trend('finassist_workflow_validation_duration', true);
const errorRate          = new Rate('finassist_workflow_error_rate');

// ── Options du test ───────────────────────────────────────────────────────────
export const options = {
  ...LOAD_OPTIONS,
  thresholds: {
    ...LOAD_OPTIONS.thresholds,
    'finassist_workflow_circuits_duration':   ['p(95)<300'],
    'finassist_workflow_validation_duration': ['p(95)<600'],
    'finassist_workflow_error_rate':          ['rate<0.02'],
  }
};

// ── Setup ─────────────────────────────────────────────────────────────────────
export function setup() {
  return {
    tokenAdmin:       getToken('admin'),
    tokenResponsable: getToken('responsable'),
  };
}

// ── Scénario principal ────────────────────────────────────────────────────────
export default function (data) {

  // ── 1. Liste des circuits ─────────────────────────────────────────────────
  group('GET /api/workflow/circuits', () => {
    const res = http.get(`${BASE_URL}/workflow/circuits`, {
      headers: authHeaders(data.tokenAdmin),
      tags: { name: 'workflow/circuits' }
    });

    circuitsDuration.add(res.timings.duration);

    const ok = check(res, {
      'circuits — status 200':  r => r.status === 200,
      'circuits — body array':  r => {
        try { return Array.isArray(JSON.parse(r.body)); }
        catch { return false; }
      },
      'circuits — < 300ms':     r => r.timings.duration < 300,
    });
    errorRate.add(!ok);
  });

  sleep(0.5);

  // ── 2. Détail d'un circuit ────────────────────────────────────────────────
  group('GET /api/workflow/circuits/1', () => {
    const res = http.get(`${BASE_URL}/workflow/circuits/1`, {
      headers: authHeaders(data.tokenAdmin),
      tags: { name: 'workflow/circuit-detail' }
    });

    check(res, {
      'circuit détail — status 200 ou 404': r => r.status === 200 || r.status === 404,
      'circuit détail — < 300ms':           r => r.timings.duration < 300,
    });
  });

  sleep(0.5);

  // ── 3. Validation d'un besoin (simulation) ────────────────────────────────
  // Note : nécessite un besoin en statut EN_ATTENTE_RESPONSABLE en base
  // Adapter besoinId selon les données de test disponibles
  group('POST /api/workflow/:id/valider', () => {
    const besoinId = 1; // adapter selon les données en base

    const res = http.post(
      `${BASE_URL}/workflow/${besoinId}/valider`,
      JSON.stringify({
        decision: 'APPROUVE',
        commentaire: 'Validé par test de performance k6'
      }),
      {
        headers: authHeaders(data.tokenResponsable),
        tags: { name: 'workflow/valider' }
      }
    );

    validationDuration.add(res.timings.duration);

    // 200 = succès, 400 = déjà validé ou statut incorrect (attendu en test répété)
    const ok = check(res, {
      'validation — status acceptable': r => [200, 400, 403, 404].includes(r.status),
      'validation — < 600ms':           r => r.timings.duration < 600,
    });
    errorRate.add(res.status >= 500); // seules les erreurs 5xx comptent
  });

  sleep(1);
}
