/**
 * Scénario de pic (spike test) — Montée brutale de charge
 *
 * Simule un pic soudain d'utilisateurs (ex: début de journée, annonce importante).
 * Teste la capacité de l'application à absorber une charge brutale
 * et à revenir à la normale.
 *
 * ⚠️  À lancer uniquement en environnement de test.
 */

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Trend, Rate } from 'k6/metrics';
import { BASE_URL, SPIKE_OPTIONS } from '../config.js';
import { getToken, authHeaders } from '../helpers/auth-helper.js';

// ── Métriques ─────────────────────────────────────────────────────────────────
const responseDuration = new Trend('finassist_spike_response_duration', true);
const errorRate        = new Rate('finassist_spike_error_rate');

// ── Options ───────────────────────────────────────────────────────────────────
export const options = SPIKE_OPTIONS;

// ── Setup ─────────────────────────────────────────────────────────────────────
export function setup() {
  return { token: getToken('admin') };
}

// ── Scénario ──────────────────────────────────────────────────────────────────
export default function (data) {
  const headers = authHeaders(data.token);

  // Simuler les requêtes les plus fréquentes lors d'un pic
  // (les utilisateurs se connectent et consultent le dashboard)
  const endpoints = [
    `${BASE_URL}/besoins`,
    `${BASE_URL}/reporting/dashboard`,
    `${BASE_URL}/notifications`,
    `${BASE_URL}/categories`,
  ];

  const url = endpoints[Math.floor(Math.random() * endpoints.length)];

  const res = http.get(url, {
    headers,
    tags: { name: 'spike/mixed' }
  });

  responseDuration.add(res.timings.duration);

  const ok = check(res, {
    'spike — status < 500':  r => r.status < 500,
    'spike — < 3000ms':      r => r.timings.duration < 3000,
  });

  errorRate.add(!ok);

  sleep(0.2);
}
