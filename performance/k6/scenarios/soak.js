/**
 * Scénario d'endurance (soak test) — Charge modérée sur longue durée
 *
 * Détecte les fuites mémoire, dégradations progressives et problèmes
 * de connexion DB qui n'apparaissent qu'après un usage prolongé.
 *
 * Durée : ~14 minutes (2 min montée + 10 min maintien + 2 min descente)
 * Charge : 10 utilisateurs simultanés
 *
 * ⚠️  À lancer uniquement en environnement de test.
 */

import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { Trend, Rate } from 'k6/metrics';
import { BASE_URL, SOAK_OPTIONS } from '../config.js';
import { getToken, authHeaders } from '../helpers/auth-helper.js';

// ── Métriques ─────────────────────────────────────────────────────────────────
const responseDuration = new Trend('finassist_soak_response_duration', true);
const errorRate        = new Rate('finassist_soak_error_rate');

// ── Options ───────────────────────────────────────────────────────────────────
export const options = SOAK_OPTIONS;

// ── Setup ─────────────────────────────────────────────────────────────────────
export function setup() {
  return {
    tokenAdmin: getToken('admin'),
    tokenAgent: getToken('agent'),
  };
}

// ── Scénario ──────────────────────────────────────────────────────────────────
export default function (data) {
  // Alterner entre les deux utilisateurs pour simuler la réalité
  const isAdmin = __VU % 2 === 0;
  const headers = authHeaders(isAdmin ? data.tokenAdmin : data.tokenAgent);

  // ── Requêtes de lecture (80% du trafic réel) ──────────────────────────────
  group('Lecture', () => {
    const readEndpoints = [
      `${BASE_URL}/besoins`,
      `${BASE_URL}/reporting/dashboard`,
      `${BASE_URL}/notifications`,
      `${BASE_URL}/categories`,
      `${BASE_URL}/workflow/circuits`,
    ];

    const url = readEndpoints[Math.floor(Math.random() * readEndpoints.length)];
    const res = http.get(url, { headers, tags: { name: 'soak/read' } });

    responseDuration.add(res.timings.duration);

    const ok = check(res, {
      'soak lecture — status 200': r => r.status === 200,
      'soak lecture — < 1000ms':   r => r.timings.duration < 1000,
    });
    errorRate.add(!ok);
  });

  sleep(1);

  // ── Requêtes d'écriture légères (20% du trafic) ───────────────────────────
  if (Math.random() < 0.2) {
    group('Écriture légère', () => {
      // Marquer une notification comme lue (opération légère)
      const notifRes = http.get(`${BASE_URL}/notifications`, {
        headers,
        tags: { name: 'soak/notif-list' }
      });

      if (notifRes.status === 200) {
        try {
          const notifs = JSON.parse(notifRes.body);
          const nonLues = notifs.filter((n) => !n.lu);
          if (nonLues.length > 0) {
            http.put(
              `${BASE_URL}/notifications/${nonLues[0].id}/lire`,
              '{}',
              { headers: { ...headers, 'Content-Type': 'application/json' }, tags: { name: 'soak/marquer-lu' } }
            );
          }
        } catch { /* silencieux */ }
      }
    });
  }

  sleep(2);
}
