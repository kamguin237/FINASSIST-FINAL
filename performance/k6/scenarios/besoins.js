/**
 * Scénario de performance — Besoins
 *
 * Teste les endpoints les plus sollicités :
 *   GET  /api/besoins              (liste — requête DB lourde)
 *   GET  /api/besoins/:id          (détail)
 *   POST /api/besoins              (création)
 *   POST /api/besoins/:id/enregistrer
 *   POST /api/besoins/:id/soumettre
 *   GET  /api/besoins/:id/historique
 *   GET  /api/besoins/deadlines
 *
 * Objectif : vérifier que la liste des besoins reste rapide
 * même avec de nombreux utilisateurs simultanés.
 */

import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { Trend, Rate } from 'k6/metrics';
import { BASE_URL, LOAD_OPTIONS } from '../config.js';
import { getToken, authHeaders, authHeadersMultipart } from '../helpers/auth-helper.js';

// ── Métriques personnalisées ──────────────────────────────────────────────────
const listeDuration    = new Trend('finassist_besoins_liste_duration',    true);
const detailDuration   = new Trend('finassist_besoins_detail_duration',   true);
const creationDuration = new Trend('finassist_besoins_creation_duration', true);
const errorRate        = new Rate('finassist_besoins_error_rate');

// ── Options du test ───────────────────────────────────────────────────────────
export const options = {
  ...LOAD_OPTIONS,
  thresholds: {
    ...LOAD_OPTIONS.thresholds,
    'finassist_besoins_liste_duration':    ['p(95)<400'],
    'finassist_besoins_detail_duration':   ['p(95)<300'],
    'finassist_besoins_creation_duration': ['p(95)<500'],
    'finassist_besoins_error_rate':        ['rate<0.01'],
  }
};

// ── Setup : récupérer les tokens et le premier categorieId disponible ─────────
export function setup() {
  const tokenAgent       = getToken('agent');
  const tokenResponsable = getToken('responsable');
  const tokenAdmin       = getToken('admin');

  // Récupérer automatiquement le premier categorieId disponible
  let categorieId = '1'; // valeur par défaut
  if (tokenAgent) {
    const catRes = http.get(`${BASE_URL}/categories`, {
      headers: authHeaders(tokenAgent),
      tags: { name: 'setup/categories' }
    });
    if (catRes.status === 200) {
      try {
        const cats = JSON.parse(catRes.body);
        if (Array.isArray(cats) && cats.length > 0) {
          categorieId = String(cats[0].id);
          console.log(`[setup] categorieId utilisé : ${categorieId} (${cats[0].nom})`);
        }
      } catch { /* silencieux */ }
    }
  }

  return { tokenAgent, tokenResponsable, tokenAdmin, categorieId };
}

// ── Scénario principal ────────────────────────────────────────────────────────
export default function (data) {
  const headers = authHeaders(data.tokenAgent);

  // ── 1. Liste des besoins ──────────────────────────────────────────────────
  group('GET /api/besoins', () => {
    const res = http.get(`${BASE_URL}/besoins`, {
      headers,
      tags: { name: 'besoins/liste' }
    });

    listeDuration.add(res.timings.duration);

    const ok = check(res, {
      'liste — status 200':  r => r.status === 200,
      'liste — body array':  r => {
        try { return Array.isArray(JSON.parse(r.body)); }
        catch { return false; }
      },
      'liste — < 400ms':     r => r.timings.duration < 400,
    });
    errorRate.add(!ok);
  });

  sleep(0.5);

  // ── 2. Créer un besoin ────────────────────────────────────────────────────
  let besoinId = null;

  group('POST /api/besoins', () => {
    // Utiliser FormData pour correspondre à l'API réelle
    const formData = {
      titre:           `Besoin perf ${Date.now()}`,
      description:     'Besoin créé par test de performance k6',
      niveauImportance: 'MOYEN',
      categorieId:     data.categorieId,  // récupéré dynamiquement depuis l'API
    };

    const res = http.post(`${BASE_URL}/besoins`, formData, {
      headers: authHeadersMultipart(data.tokenAgent),
      tags: { name: 'besoins/creation' }
    });

    creationDuration.add(res.timings.duration);

    const ok = check(res, {
      'création — status 200 ou 201': r => r.status === 200 || r.status === 201,
      'création — id présent':        r => {
        try { return !!JSON.parse(r.body).id; }
        catch { return false; }
      },
      'création — < 500ms':           r => r.timings.duration < 500,
    });

    errorRate.add(!ok);

    if (ok && res.status < 300) {
      try { besoinId = JSON.parse(res.body).id; }
      catch { /* silencieux */ }
    }
  });

  sleep(0.3);

  // ── 3. Détail d'un besoin ─────────────────────────────────────────────────
  if (besoinId) {
    group('GET /api/besoins/:id', () => {
      const res = http.get(`${BASE_URL}/besoins/${besoinId}`, {
        headers,
        tags: { name: 'besoins/detail' }
      });

      detailDuration.add(res.timings.duration);

      check(res, {
        'détail — status 200': r => r.status === 200,
        'détail — < 300ms':    r => r.timings.duration < 300,
      });
    });

    sleep(0.3);

    // ── 4. Enregistrer le besoin ────────────────────────────────────────────
    group('POST /api/besoins/:id/enregistrer', () => {
      const res = http.post(
        `${BASE_URL}/besoins/${besoinId}/enregistrer`,
        '{}',
        { headers, tags: { name: 'besoins/enregistrer' } }
      );

      check(res, {
        'enregistrer — status 200': r => r.status === 200,
        'enregistrer — < 400ms':    r => r.timings.duration < 400,
      });
    });

    sleep(0.3);

    // ── 5. Historique ───────────────────────────────────────────────────────
    group('GET /api/besoins/:id/historique', () => {
      const res = http.get(`${BASE_URL}/besoins/${besoinId}/historique`, {
        headers,
        tags: { name: 'besoins/historique' }
      });

      check(res, {
        'historique — status 200': r => r.status === 200,
        'historique — < 300ms':    r => r.timings.duration < 300,
      });
    });
  }

  sleep(0.5);

  // ── 6. Deadlines ──────────────────────────────────────────────────────────
  group('GET /api/besoins/deadlines', () => {
    const res = http.get(`${BASE_URL}/besoins/deadlines`, {
      headers: authHeaders(data.tokenAdmin),
      tags: { name: 'besoins/deadlines' }
    });

    check(res, {
      'deadlines — status 200': r => r.status === 200,
      'deadlines — < 400ms':    r => r.timings.duration < 400,
    });
  });

  sleep(1);
}
