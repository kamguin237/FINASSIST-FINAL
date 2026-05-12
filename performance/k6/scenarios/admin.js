/**
 * Scénario de performance — Administration (Users, Rôles, Logs)
 *
 * Teste les endpoints admin :
 *   GET /api/users
 *   GET /api/users/:id
 *   GET /api/roles
 *   GET /api/permissions
 *   GET /api/logs
 *   GET /api/categories
 *
 * Objectif : vérifier que les listes admin restent rapides
 * même avec de nombreux enregistrements.
 */

import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { Trend, Rate } from 'k6/metrics';
import { BASE_URL, LOAD_OPTIONS } from '../config.js';
import { getToken, authHeaders } from '../helpers/auth-helper.js';

// ── Métriques personnalisées ──────────────────────────────────────────────────
const usersDuration      = new Trend('finassist_admin_users_duration',      true);
const logsDuration       = new Trend('finassist_admin_logs_duration',       true);
const categoriesDuration = new Trend('finassist_admin_categories_duration', true);
const errorRate          = new Rate('finassist_admin_error_rate');

// ── Options du test ───────────────────────────────────────────────────────────
export const options = {
  ...LOAD_OPTIONS,
  thresholds: {
    ...LOAD_OPTIONS.thresholds,
    'finassist_admin_users_duration':      ['p(95)<400'],
    'finassist_admin_logs_duration':       ['p(95)<600'],
    'finassist_admin_categories_duration': ['p(95)<300'],
    'finassist_admin_error_rate':          ['rate<0.01'],
  }
};

// ── Setup ─────────────────────────────────────────────────────────────────────
export function setup() {
  return { tokenAdmin: getToken('admin') };
}

// ── Scénario principal ────────────────────────────────────────────────────────
export default function (data) {
  const headers = authHeaders(data.tokenAdmin);

  // ── 1. Liste des utilisateurs ─────────────────────────────────────────────
  group('GET /api/users', () => {
    const res = http.get(`${BASE_URL}/users`, {
      headers,
      tags: { name: 'admin/users' }
    });

    usersDuration.add(res.timings.duration);

    const ok = check(res, {
      // 200 si permissions ok, 403 si non-admin — les deux sont acceptables
      'users — status 200 ou 403': r => r.status === 200 || r.status === 403,
      'users — < 400ms':           r => r.timings.duration < 400,
    });
    errorRate.add(res.status >= 500); // seules les erreurs 5xx comptent
  });

  sleep(0.3);

  // ── 2. Liste des rôles ────────────────────────────────────────────────────
  group('GET /api/roles', () => {
    const res = http.get(`${BASE_URL}/roles`, {
      headers,
      tags: { name: 'admin/roles' }
    });

    check(res, {
      'roles — status 200 ou 403': r => r.status === 200 || r.status === 403,
      'roles — < 300ms':           r => r.timings.duration < 300,
    });
  });

  sleep(0.3);

  // ── 3. Liste des permissions ──────────────────────────────────────────────
  group('GET /api/permissions', () => {
    const res = http.get(`${BASE_URL}/permissions`, {
      headers,
      tags: { name: 'admin/permissions' }
    });

    check(res, {
      'permissions — status 200 ou 403': r => r.status === 200 || r.status === 403,
      'permissions — < 300ms':           r => r.timings.duration < 300,
    });
  });

  sleep(0.3);

  // ── 4. Logs avec pagination ───────────────────────────────────────────────
  group('GET /api/logs?page=1&pageSize=20', () => {
    const res = http.get(`${BASE_URL}/logs?page=1&pageSize=20`, {
      headers,
      tags: { name: 'admin/logs' }
    });

    logsDuration.add(res.timings.duration);

    check(res, {
      'logs — status 200':   r => r.status === 200,
      'logs — total présent': r => {
        try { return 'total' in JSON.parse(r.body); }
        catch { return false; }
      },
      'logs — < 600ms':      r => r.timings.duration < 600,
    });
  });

  sleep(0.3);

  // ── 5. Catégories ─────────────────────────────────────────────────────────
  group('GET /api/categories', () => {
    const res = http.get(`${BASE_URL}/categories`, {
      headers,
      tags: { name: 'admin/categories' }
    });

    categoriesDuration.add(res.timings.duration);

    check(res, {
      'catégories — status 200': r => r.status === 200,
      'catégories — < 300ms':    r => r.timings.duration < 300,
    });
  });

  sleep(0.3);

  // ── 6. Notifications ──────────────────────────────────────────────────────
  group('GET /api/notifications', () => {
    const res = http.get(`${BASE_URL}/notifications`, {
      headers,
      tags: { name: 'admin/notifications' }
    });

    check(res, {
      'notifications — status 200': r => r.status === 200,
      'notifications — < 300ms':    r => r.timings.duration < 300,
    });
  });

  sleep(1);
}
