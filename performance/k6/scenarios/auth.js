/**
 * Scénario de performance — Authentification
 *
 * Teste les endpoints :
 *   POST /api/auth/login
 *   POST /api/auth/logout
 *
 * Objectif : mesurer le temps de login sous charge concurrente.
 * Le login est critique car il bloque toute l'application si lent.
 */

import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { Trend, Rate, Counter } from 'k6/metrics';
import { BASE_URL, USERS, LOAD_OPTIONS } from '../config.js';

// ── Métriques personnalisées ──────────────────────────────────────────────────
const loginDuration   = new Trend('finassist_login_duration',   true);
const loginSuccessRate = new Rate('finassist_login_success_rate');
const loginCount      = new Counter('finassist_login_count');

// ── Options du test ───────────────────────────────────────────────────────────
export const options = {
  ...LOAD_OPTIONS,
  thresholds: {
    ...LOAD_OPTIONS.thresholds,
    // Le login doit être rapide — seuil strict
    'finassist_login_duration': ['p(95)<300'],
    'finassist_login_success_rate': ['rate>0.99'],
  }
};

// ── Scénario principal ────────────────────────────────────────────────────────
export default function () {
  // Alterner entre les différents rôles pour simuler la réalité
  const roles = ['admin', 'agent', 'responsable'];
  const role  = roles[Math.floor(Math.random() * roles.length)];
  const user  = USERS[role];

  group('Login', () => {
    const res = http.post(
      `${BASE_URL}/auth/login`,
      JSON.stringify({ email: user.email, motDePasse: user.motDePasse }),
      {
        headers: { 'Content-Type': 'application/json' },
        tags: { name: 'auth/login', role }
      }
    );

    loginDuration.add(res.timings.duration);
    loginCount.add(1);

    const success = check(res, {
      'login — status 200':         r => r.status === 200,
      'login — token présent':      r => {
        try { return !!JSON.parse(r.body).accessToken; }
        catch { return false; }
      },
      'login — utilisateur présent': r => {
        try { return !!JSON.parse(r.body).utilisateur; }
        catch { return false; }
      },
      'login — < 300ms':            r => r.timings.duration < 300,
    });

    loginSuccessRate.add(success);

    if (!success) {
      console.warn(`[auth] Login échoué — role: ${role}, status: ${res.status}`);
    }
  });

  sleep(1);

  // ── Test login avec mauvais credentials (doit retourner 401) ─────────────
  group('Login invalide', () => {
    const res = http.post(
      `${BASE_URL}/auth/login`,
      JSON.stringify({ email: 'inexistant@finstar-cm.com', motDePasse: 'mauvais' }),
      {
        headers: { 'Content-Type': 'application/json' },
        tags: { name: 'auth/login-invalid' }
      }
    );

    check(res, {
      'login invalide — status 401': r => r.status === 401,
      'login invalide — < 200ms':    r => r.timings.duration < 200,
    });
  });

  sleep(0.5);
}
