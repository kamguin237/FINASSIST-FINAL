/**
 * Scénario complet — Parcours utilisateur réaliste FinAssist
 *
 * Simule le comportement réel d'un utilisateur :
 *   1. Login
 *   2. Consultation du dashboard
 *   3. Liste des besoins
 *   4. Création d'un besoin
 *   5. Enregistrement et soumission
 *   6. Consultation des notifications
 *   7. Logout
 *
 * C'est le test le plus représentatif de la charge réelle.
 */

import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { Trend, Rate, Counter } from 'k6/metrics';
import { BASE_URL, USERS, LOAD_OPTIONS } from '../config.js';
import { authHeaders, authHeadersMultipart } from '../helpers/auth-helper.js';

// ── Métriques personnalisées ──────────────────────────────────────────────────
const parcoursCompletDuration = new Trend('finassist_parcours_complet_duration', true);
const parcoursSuccessRate     = new Rate('finassist_parcours_success_rate');
const parcoursCount           = new Counter('finassist_parcours_count');

// ── Options du test ───────────────────────────────────────────────────────────
export const options = {
  ...LOAD_OPTIONS,
  thresholds: {
    ...LOAD_OPTIONS.thresholds,
    'finassist_parcours_complet_duration': ['p(95)<5000'],
    'finassist_parcours_success_rate':     ['rate>0.95'],
  }
};

// ── Scénario principal ────────────────────────────────────────────────────────
export default function () {
  const startTime = Date.now();
  let allChecksOk = true;

  // ── Étape 1 : Login ───────────────────────────────────────────────────────
  let token = null;
  let categorieId = '1'; // sera récupéré dynamiquement après login

  group('1. Login', () => {
    const user = USERS.agent;
    const res = http.post(
      `${BASE_URL}/auth/login`,
      JSON.stringify({ email: user.email, motDePasse: user.motDePasse }),
      {
        headers: { 'Content-Type': 'application/json' },
        tags: { name: 'parcours/login' }
      }
    );

    const ok = check(res, {
      'login — status 200':    r => r.status === 200,
      'login — token présent': r => {
        try { return !!JSON.parse(r.body).accessToken; }
        catch { return false; }
      }
    });

    allChecksOk = allChecksOk && ok;

    if (ok) {
      try { token = JSON.parse(res.body).accessToken; }
      catch { /* silencieux */ }
    }
  });

  if (!token) {
    parcoursSuccessRate.add(false);
    parcoursCount.add(1);
    return;
  }

  const headers = authHeaders(token);
  sleep(1); // pause naturelle entre les actions

  // Récupérer dynamiquement le premier categorieId disponible
  const catRes = http.get(`${BASE_URL}/categories`, {
    headers,
    tags: { name: 'parcours/categories' }
  });
  if (catRes.status === 200) {
    try {
      const cats = JSON.parse(catRes.body);
      if (Array.isArray(cats) && cats.length > 0) {
        categorieId = String(cats[0].id);
      }
    } catch { /* silencieux */ }
  }

  // ── Étape 2 : Dashboard ───────────────────────────────────────────────────
  group('2. Dashboard', () => {
    const res = http.get(`${BASE_URL}/reporting/dashboard`, {
      headers,
      tags: { name: 'parcours/dashboard' }
    });

    const ok = check(res, {
      'dashboard — status 200': r => r.status === 200,
      'dashboard — < 800ms':    r => r.timings.duration < 800,
    });
    allChecksOk = allChecksOk && ok;
  });

  sleep(2); // l'utilisateur lit le dashboard

  // ── Étape 3 : Liste des besoins ───────────────────────────────────────────
  group('3. Liste des besoins', () => {
    const res = http.get(`${BASE_URL}/besoins`, {
      headers,
      tags: { name: 'parcours/besoins-liste' }
    });

    const ok = check(res, {
      'liste besoins — status 200': r => r.status === 200,
      'liste besoins — < 400ms':    r => r.timings.duration < 400,
    });
    allChecksOk = allChecksOk && ok;
  });

  sleep(1.5);

  // ── Étape 4 : Créer un besoin ─────────────────────────────────────────────
  let besoinId = null;

  group('4. Créer un besoin', () => {
    // Le backend attend multipart/form-data ([FromForm] ASP.NET Core)
    // k6 construit le multipart automatiquement avec http.file() pour les champs texte
    const res = http.post(`${BASE_URL}/besoins`, {
      titre:            `Besoin ${Date.now()}`,
      description:      'Créé par test de performance k6 — parcours complet',
      niveauImportance: 'MOYEN',
      categorieId:      String(categorieId),
    }, {
      headers: authHeadersMultipart(token),
      tags: { name: 'parcours/besoin-creation' }
    });

    const ok = check(res, {
      'création — status 200 ou 201': r => r.status === 200 || r.status === 201,
      'création — < 500ms':           r => r.timings.duration < 500,
    });
    allChecksOk = allChecksOk && ok;

    if (ok && res.status < 300) {
      try { besoinId = JSON.parse(res.body).id; }
      catch { /* silencieux */ }
    }
  });

  sleep(1);

  // ── Étape 5 : Enregistrer le besoin ──────────────────────────────────────
  if (besoinId) {
    group('5. Enregistrer le besoin', () => {
      const res = http.post(
        `${BASE_URL}/besoins/${besoinId}/enregistrer`,
        '{}',
        { headers, tags: { name: 'parcours/besoin-enregistrer' } }
      );

      const ok = check(res, {
        'enregistrer — status 200': r => r.status === 200,
        'enregistrer — < 400ms':    r => r.timings.duration < 400,
      });
      allChecksOk = allChecksOk && ok;
    });

    sleep(0.5);

    // ── Étape 6 : Soumettre le besoin ───────────────────────────────────────
    group('6. Soumettre le besoin', () => {
      const res = http.post(
        `${BASE_URL}/besoins/${besoinId}/soumettre`,
        '{}',
        { headers, tags: { name: 'parcours/besoin-soumettre' } }
      );

      const ok = check(res, {
        'soumettre — status 200': r => r.status === 200,
        'soumettre — < 500ms':    r => r.timings.duration < 500,
      });
      allChecksOk = allChecksOk && ok;
    });

    sleep(1);
  }

  // ── Étape 7 : Notifications ───────────────────────────────────────────────
  group('7. Notifications', () => {
    const res = http.get(`${BASE_URL}/notifications`, {
      headers,
      tags: { name: 'parcours/notifications' }
    });

    check(res, {
      'notifications — status 200': r => r.status === 200,
      'notifications — < 300ms':    r => r.timings.duration < 300,
    });
  });

  sleep(1);

  // ── Métriques finales ─────────────────────────────────────────────────────
  const totalDuration = Date.now() - startTime;
  parcoursCompletDuration.add(totalDuration);
  parcoursSuccessRate.add(allChecksOk);
  parcoursCount.add(1);

  sleep(2); // pause entre les itérations
}
