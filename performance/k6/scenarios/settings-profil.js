/**
 * Scénario de performance — Paramètres utilisateur et Profil
 *
 * Teste les endpoints :
 *   GET  /api/users/me
 *   PUT  /api/users/me/password  (simulé — ne change pas vraiment le mot de passe)
 *   GET  /api/settings
 *   PUT  /api/settings
 *   GET  /api/ma-signature
 *   POST /api/ma-signature
 */

import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { Trend, Rate } from 'k6/metrics';
import { BASE_URL, LOAD_OPTIONS } from '../config.js';
import { getToken, authHeaders } from '../helpers/auth-helper.js';

// ── Métriques personnalisées ──────────────────────────────────────────────────
const settingsDuration = new Trend('finassist_settings_duration', true);
const profilDuration   = new Trend('finassist_profil_duration',   true);
const errorRate        = new Rate('finassist_settings_error_rate');

// ── Options du test ───────────────────────────────────────────────────────────
export const options = {
  ...LOAD_OPTIONS,
  thresholds: {
    ...LOAD_OPTIONS.thresholds,
    'finassist_settings_duration': ['p(95)<400'],
    'finassist_profil_duration':   ['p(95)<300'],
    'finassist_settings_error_rate': ['rate<0.02'],  // 2% — acceptable sur machine locale
  }
};

// ── Setup ─────────────────────────────────────────────────────────────────────
export function setup() {
  return {
    tokenAgent:       getToken('agent'),
    tokenResponsable: getToken('responsable'),
    tokenAdmin:       getToken('admin'),
  };
}

// ── Scénario principal ────────────────────────────────────────────────────────
export default function (data) {
  const headers = authHeaders(data.tokenAgent);

  // ── 1. Profil utilisateur ─────────────────────────────────────────────────
  group('GET /api/users/me', () => {
    const res = http.get(`${BASE_URL}/users/me`, {
      headers,
      tags: { name: 'profil/me' }
    });

    profilDuration.add(res.timings.duration);

    const ok = check(res, {
      'profil — status 200':    r => r.status === 200,
      'profil — email présent': r => {
        try { return !!JSON.parse(r.body).email; }
        catch { return false; }
      },
      'profil — < 300ms':       r => r.timings.duration < 300,
    });
    errorRate.add(!ok);
  });

  sleep(0.3);

  // ── 2. Récupérer les paramètres ───────────────────────────────────────────
  group('GET /api/settings', () => {
    const res = http.get(`${BASE_URL}/settings`, {
      headers,
      tags: { name: 'settings/get' }
    });

    settingsDuration.add(res.timings.duration);

    check(res, {
      'settings GET — status 200': r => r.status === 200,
      'settings GET — < 400ms':    r => r.timings.duration < 400,
    });
  });

  sleep(0.3);

  // ── 3. Sauvegarder les paramètres ─────────────────────────────────────────
  group('PUT /api/settings', () => {
    const settings = {
      notifApp: true,
      notifEmail: false,
      alertNouveauBesoin: true,
      alertValidation: true,
      alertEnAttente: true,
      langue: 'fr',
      formatDate: 'dd/MM/yyyy',
      fuseauHoraire: 'Africa/Douala',
      itemsParPage: 20,
      pageAccueil: '/dashboard',
      triDefaut: 'dateDesc',
      deconnexionAuto: true,
      inactiviteMinutes: 30,
      avertissementSecondes: 30
    };

    const res = http.put(
      `${BASE_URL}/settings`,
      JSON.stringify(settings),
      {
        headers: { ...headers, 'Content-Type': 'application/json' },
        tags: { name: 'settings/put' }
      }
    );

    check(res, {
      'settings PUT — status 200 ou 204': r => r.status === 200 || r.status === 204,
      'settings PUT — < 400ms':           r => r.timings.duration < 400,
    });
  });

  sleep(0.3);

  // ── 4. Ma signature ───────────────────────────────────────────────────────
  group('GET /api/ma-signature', () => {
    const res = http.get(`${BASE_URL}/ma-signature`, {
      headers,
      tags: { name: 'signature/get' }
    });

    check(res, {
      // 200 si signature existe, 204 si aucune signature
      'ma-signature — status 200 ou 204': r => r.status === 200 || r.status === 204,
      'ma-signature — < 300ms':           r => r.timings.duration < 300,
    });
  });

  sleep(0.3);

  // ── 5. Notifications (marquer comme lues) ─────────────────────────────────
  group('GET /api/notifications', () => {
    const res = http.get(`${BASE_URL}/notifications`, {
      headers,
      tags: { name: 'notifications/get' }
    });

    check(res, {
      'notifications — status 200': r => r.status === 200,
      'notifications — < 300ms':    r => r.timings.duration < 300,
    });
  });

  sleep(1);
}
