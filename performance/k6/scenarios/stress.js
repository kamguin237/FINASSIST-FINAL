/**
 * Scénario de stress — Trouver le point de rupture de FinAssist
 *
 * Pousse progressivement la charge jusqu'à ce que les seuils soient dépassés.
 * Identifie le nombre maximum d'utilisateurs simultanés supportés.
 *
 * ⚠️  À lancer uniquement en environnement de test, jamais en production.
 */

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Trend, Rate } from 'k6/metrics';
import { BASE_URL, STRESS_OPTIONS } from '../config.js';
import { getToken, authHeaders } from '../helpers/auth-helper.js';

// ── Métriques ─────────────────────────────────────────────────────────────────
const responseDuration = new Trend('finassist_stress_response_duration', true);
const errorRate        = new Rate('finassist_stress_error_rate');

// ── Options ───────────────────────────────────────────────────────────────────
export const options = STRESS_OPTIONS;

// ── Setup ─────────────────────────────────────────────────────────────────────
export function setup() {
  return { token: getToken('admin') };
}

// ── Scénario ──────────────────────────────────────────────────────────────────
export default function (data) {
  const headers = authHeaders(data.token);

  // Alterner entre les endpoints les plus lourds
  const endpoints = [
    `${BASE_URL}/besoins`,
    `${BASE_URL}/reporting/dashboard`,
    `${BASE_URL}/reporting/statistiques`,
    `${BASE_URL}/users`,
    `${BASE_URL}/logs?page=1&pageSize=20`,
  ];

  const url = endpoints[Math.floor(Math.random() * endpoints.length)];

  const res = http.get(url, {
    headers,
    tags: { name: 'stress/mixed' }
  });

  responseDuration.add(res.timings.duration);

  const ok = check(res, {
    'stress — status < 500':  r => r.status < 500,
    'stress — < 2000ms':      r => r.timings.duration < 2000,
  });

  errorRate.add(!ok);

  sleep(0.1); // pause minimale pour maximiser la charge
}
