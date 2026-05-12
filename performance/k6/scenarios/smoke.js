/**
 * Scénario de fumée (smoke test) — Vérification rapide avant les vrais tests
 *
 * 1 seul utilisateur, 30 secondes.
 * Vérifie que tous les endpoints répondent correctement
 * avant de lancer les tests de charge.
 *
 * À lancer en premier : k6 run performance/k6/scenarios/smoke.js
 */

import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { BASE_URL, SMOKE_OPTIONS } from '../config.js';
import { getToken, authHeaders } from '../helpers/auth-helper.js';

// ── Options ───────────────────────────────────────────────────────────────────
export const options = SMOKE_OPTIONS;

// ── Setup ─────────────────────────────────────────────────────────────────────
export function setup() {
  return { token: getToken('admin') };
}

// ── Scénario ──────────────────────────────────────────────────────────────────
export default function (data) {
  const headers = authHeaders(data.token);

  // Vérifier tous les endpoints critiques une fois
  // Note : users/roles/permissions retournent 403 si l'utilisateur n'a pas les permissions admin
  const endpoints = [
    { name: 'besoins/liste',          url: `${BASE_URL}/besoins`,                      adminOnly: false },
    { name: 'reporting/dashboard',    url: `${BASE_URL}/reporting/dashboard`,           adminOnly: false },
    { name: 'reporting/statistiques', url: `${BASE_URL}/reporting/statistiques`,        adminOnly: false },
    { name: 'workflow/circuits',      url: `${BASE_URL}/workflow/circuits`,             adminOnly: false },
    { name: 'users/liste',            url: `${BASE_URL}/users`,                         adminOnly: true  },
    { name: 'roles/liste',            url: `${BASE_URL}/roles`,                         adminOnly: true  },
    { name: 'permissions/liste',      url: `${BASE_URL}/permissions`,                   adminOnly: true  },
    { name: 'categories/liste',       url: `${BASE_URL}/categories`,                    adminOnly: false },
    { name: 'notifications/liste',    url: `${BASE_URL}/notifications`,                 adminOnly: false },
    { name: 'logs/liste',             url: `${BASE_URL}/logs?page=1&pageSize=10`,       adminOnly: false },
    { name: 'besoins/deadlines',      url: `${BASE_URL}/besoins/deadlines`,             adminOnly: false },
  ];

  for (const ep of endpoints) {
    group(ep.name, () => {
      const res = http.get(ep.url, {
        headers,
        tags: { name: ep.name }
      });

      if (ep.adminOnly) {
        // Pour les endpoints admin : 200 (si permissions ok) ou 403 (si non-admin) sont tous les deux acceptables
        check(res, {
          [`${ep.name} — status 200 ou 403`]: r => r.status === 200 || r.status === 403,
          [`${ep.name} — < 1000ms`]:          r => r.timings.duration < 1000,
        });
      } else {
        check(res, {
          [`${ep.name} — status 200`]:        r => r.status === 200,
          [`${ep.name} — < 1000ms`]:          r => r.timings.duration < 1000,
          [`${ep.name} — body non vide`]:     r => r.body && r.body.length > 0,
        });
      }
    });

    sleep(0.2);
  }

  sleep(1);
}
