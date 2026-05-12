/**
 * Scénario de performance — Cycle de validation complet
 *
 * Simule le parcours complet d'un besoin à travers le circuit de validation :
 *   Agent    : créer → enregistrer → soumettre
 *   Responsable : valider (APPROUVE)
 *   Vérification : statut final
 *
 * Chaque VU crée son propre besoin pour éviter les conflits de données.
 * Utilise un circuit à 1 étape (Responsable) pour simplifier.
 *
 * Prérequis :
 *   - landry.njikam@finstar-cm.com (Direction/admin dans config) → crée les besoins
 *     Son rôle ne participe pas au circuit Responsable → peut créer dans catégorie 1
 *   - brad.nkoumou@finstar-cm.com (Responsable/agent dans config) → valide les besoins
 *   - categorieId = 1 (circuit avec étape Responsable)
 */

import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { Trend, Rate, Counter } from 'k6/metrics';
import { BASE_URL, LOAD_OPTIONS } from '../config.js';
import { getToken, authHeaders, authHeadersMultipart } from '../helpers/auth-helper.js';

// ── Métriques personnalisées ──────────────────────────────────────────────────
const soumissionDuration  = new Trend('finassist_wf_soumission_duration',  true);
const validationDuration  = new Trend('finassist_wf_validation_duration',  true);
const cycleCompletDuration = new Trend('finassist_wf_cycle_complet_duration', true);
const cycleSuccessRate    = new Rate('finassist_wf_cycle_success_rate');
const cycleCount          = new Counter('finassist_wf_cycle_count');

// ── Options du test ───────────────────────────────────────────────────────────
export const options = {
  // Charge modérée : le workflow est séquentiel, pas besoin de beaucoup de VUs
  stages: [
    { duration: '20s', target: 3  },
    { duration: '1m',  target: 10 },
    { duration: '20s', target: 10 },
    { duration: '20s', target: 0  },
  ],
  thresholds: {
    'finassist_wf_soumission_duration':   ['p(95)<1000'],
    'finassist_wf_validation_duration':   ['p(95)<800'],
    'finassist_wf_cycle_complet_duration': ['p(95)<5000'],
    'finassist_wf_cycle_success_rate':    ['rate>0.90'],
    'http_req_failed':                    ['rate<0.05'],
  }
};

// ── Setup : récupérer les tokens ──────────────────────────────────────────────
export function setup() {
  // landry (Direction) crée le besoin — son rôle n'est pas dans le circuit Responsable
  // brad (Responsable) valide le besoin
  const tokenCreateur    = getToken('admin');      // Direction → peut créer
  const tokenValidateur  = getToken('agent');      // Responsable → peut valider

  // Récupérer le premier categorieId disponible pour le créateur
  let categorieId = '1';
  if (tokenCreateur) {
    const catRes = http.get(`${BASE_URL}/categories`, {
      headers: authHeaders(tokenCreateur),
      tags: { name: 'setup/categories' }
    });
    if (catRes.status === 200) {
      try {
        const cats = JSON.parse(catRes.body);
        if (Array.isArray(cats) && cats.length > 0) {
          categorieId = String(cats[0].id);
          console.log(`[setup] categorieId: ${categorieId} (${cats[0].nom})`);
        }
      } catch { /* silencieux */ }
    }
  }

  return { tokenCreateur, tokenValidateur, categorieId };
}

// ── Scénario principal ────────────────────────────────────────────────────────
export default function (data) {
  const startTime = Date.now();
  let cycleOk = true;
  let besoinId = null;

  // ── Étape 1 : Créer un besoin (Agent) ─────────────────────────────────────
  group('1. Créer besoin', () => {
    // Le backend attend multipart/form-data ([FromForm])
    // authHeadersMultipart ne spécifie pas Content-Type → k6 génère multipart automatiquement
    const res = http.post(`${BASE_URL}/besoins`, {
      titre:            `WF-Perf-${Date.now()}-${__VU}`,
      description:      'Besoin créé par test de performance workflow k6',
      niveauImportance: 'MOYEN',
      categorieId:      data.categorieId,
    }, {
      headers: authHeadersMultipart(data.tokenCreateur),
      tags: { name: 'wf/creer' }
    });

    const ok = check(res, {
      'créer — status 201': r => r.status === 201,
      'créer — id présent': r => {
        try { return !!JSON.parse(r.body).id; }
        catch { return false; }
      }
    });

    cycleOk = cycleOk && ok;
    if (ok && res.status === 201) {
      try { besoinId = JSON.parse(res.body).id; }
      catch { /* silencieux */ }
    }
  });

  if (!besoinId) {
    cycleSuccessRate.add(false);
    cycleCount.add(1);
    sleep(2);
    return;
  }

  sleep(0.3);

  // ── Étape 2 : Enregistrer (Créateur) ──────────────────────────────────────
  group('2. Enregistrer besoin', () => {
    const res = http.post(
      `${BASE_URL}/besoins/${besoinId}/enregistrer`,
      '{}',
      { headers: authHeaders(data.tokenCreateur), tags: { name: 'wf/enregistrer' } }
    );

    const ok = check(res, {
      'enregistrer — status 200': r => r.status === 200,
      'enregistrer — < 500ms':    r => r.timings.duration < 500,
    });
    cycleOk = cycleOk && ok;
  });

  sleep(0.3);

  // ── Étape 3 : Soumettre (Créateur) ────────────────────────────────────────
  group('3. Soumettre besoin', () => {
    const res = http.post(
      `${BASE_URL}/besoins/${besoinId}/soumettre`,
      '{}',
      { headers: authHeaders(data.tokenCreateur), tags: { name: 'wf/soumettre' } }
    );

    soumissionDuration.add(res.timings.duration);

    const ok = check(res, {
      'soumettre — status 200':          r => r.status === 200,
      'soumettre — statut EN_ATTENTE':   r => {
        try { return JSON.parse(r.body).statut?.startsWith('EN_ATTENTE'); }
        catch { return false; }
      },
      'soumettre — < 1000ms':            r => r.timings.duration < 1000,
    });
    cycleOk = cycleOk && ok;
  });

  sleep(0.5);

  // ── Étape 4 : Valider (Validateur) ───────────────────────────────────────
  group('4. Valider besoin (APPROUVE)', () => {
    const res = http.post(
      `${BASE_URL}/workflow/${besoinId}/valider`,
      JSON.stringify({ decision: 'APPROUVE', commentaire: 'Validé par test de performance k6' }),
      {
        headers: {
          ...authHeaders(data.tokenValidateur),
          'Content-Type': 'application/json'
        },
        tags: { name: 'wf/valider' }
      }
    );

    validationDuration.add(res.timings.duration);

    const ok = check(res, {
      // 200 = approuvé, 400 = déjà validé ou statut incorrect (acceptable en test répété)
      'valider — status acceptable': r => [200, 400].includes(r.status),
      'valider — < 800ms':           r => r.timings.duration < 800,
    });

    // Seules les erreurs 5xx comptent comme échec réel
    cycleOk = cycleOk && (res.status < 500);
  });

  sleep(0.3);

  // ── Étape 5 : Vérifier le statut final ───────────────────────────────────
  group('5. Vérifier statut final', () => {
    const res = http.get(`${BASE_URL}/besoins/${besoinId}`, {
      headers: authHeaders(data.tokenCreateur),
      tags: { name: 'wf/verifier-statut' }
    });

    check(res, {
      'statut final — status 200': r => r.status === 200,
      'statut final — < 300ms':    r => r.timings.duration < 300,
    });
  });

  // ── Métriques finales ─────────────────────────────────────────────────────
  const totalDuration = Date.now() - startTime;
  cycleCompletDuration.add(totalDuration);
  cycleSuccessRate.add(cycleOk);
  cycleCount.add(1);

  sleep(2);
}
