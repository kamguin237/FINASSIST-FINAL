/**
 * Configuration globale des tests de performance FinAssist
 *
 * URL de base : http://localhost:8080/api (backend Docker)
 * Credentials de test : à adapter selon les données en base
 */

export const BASE_URL = 'http://localhost:8080/api';

// ── Credentials de test ───────────────────────────────────────────────────────
export const USERS = {
  admin: {
    email: 'landry.njikam@finstar-cm.com',
    motDePasse: 'landry2002'
  },
  agent: {
    email: 'brad.nkoumou@finstar-cm.com',
    motDePasse: 'brad2002'
  },
  responsable: {
    email: 'martial.nkolo@finstar-cm.com',
    motDePasse: 'martial2002'
  }
};

// ── Seuils globaux (SLA FinAssist) ────────────────────────────────────────────
export const GLOBAL_THRESHOLDS = {
  // 95% des requêtes doivent répondre en moins de 500ms
  http_req_duration: ['p(95)<500', 'p(99)<1000'],
  // Moins de 1% d'erreurs HTTP
  http_req_failed: ['rate<0.01'],
  // Moins de 5% de checks échoués
  checks: ['rate>0.95'],
};

// ── Profils de charge ─────────────────────────────────────────────────────────

/** Test de fumée : 1 utilisateur, 30 secondes — vérifie que tout fonctionne */
export const SMOKE_OPTIONS = {
  vus: 1,
  duration: '30s',
  thresholds: GLOBAL_THRESHOLDS,
};

/** Test de charge normale : montée progressive jusqu'à 20 utilisateurs */
export const LOAD_OPTIONS = {
  stages: [
    { duration: '30s', target: 5  },  // montée douce
    { duration: '1m',  target: 20 },  // charge nominale
    { duration: '30s', target: 20 },  // maintien
    { duration: '30s', target: 0  },  // descente
  ],
  thresholds: GLOBAL_THRESHOLDS,
};

/** Test de stress : pousse jusqu'à la limite */
export const STRESS_OPTIONS = {
  stages: [
    { duration: '30s', target: 10  },
    { duration: '1m',  target: 30  },
    { duration: '1m',  target: 50  },
    { duration: '30s', target: 100 },
    { duration: '1m',  target: 100 },
    { duration: '30s', target: 0   },
  ],
  thresholds: {
    http_req_duration: ['p(95)<2000'],  // seuils plus souples en stress
    http_req_failed:   ['rate<0.05'],
  },
};

/** Test de pic (spike) : montée brutale */
export const SPIKE_OPTIONS = {
  stages: [
    { duration: '10s', target: 1   },
    { duration: '10s', target: 50  },  // pic brutal
    { duration: '1m',  target: 50  },
    { duration: '10s', target: 1   },
  ],
  thresholds: {
    http_req_duration: ['p(95)<3000'],
    http_req_failed:   ['rate<0.10'],
  },
};

/** Test d'endurance : charge modérée sur longue durée */
export const SOAK_OPTIONS = {
  stages: [
    { duration: '2m',  target: 10 },
    { duration: '10m', target: 10 },  // maintien 10 minutes
    { duration: '2m',  target: 0  },
  ],
  thresholds: GLOBAL_THRESHOLDS,
};
