/**
 * Helper d'authentification pour les tests k6 FinAssist
 * Gère la récupération et le renouvellement du token JWT
 */

import http from 'k6/http';
import { check } from 'k6';
import { BASE_URL, USERS } from '../config.js';

/**
 * Récupère un token JWT pour un utilisateur donné.
 * À appeler dans la fonction setup() de chaque scénario.
 *
 * @param {string} role - 'admin' | 'agent' | 'responsable'
 * @returns {string} Le token JWT
 */
export function getToken(role = 'admin') {
  const user = USERS[role];
  if (!user) throw new Error(`Rôle inconnu : ${role}`);

  const res = http.post(
    `${BASE_URL}/auth/login`,
    JSON.stringify({
      email: user.email,
      motDePasse: user.motDePasse
    }),
    {
      headers: { 'Content-Type': 'application/json' },
      tags: { name: 'auth/login' }
    }
  );

  const ok = check(res, {
    'login — status 200':       r => r.status === 200,
    'login — token présent':    r => {
      try { return !!JSON.parse(r.body).accessToken; }
      catch { return false; }
    }
  });

  if (!ok || res.status !== 200) {
    console.error(`[auth] Échec login pour ${user.email} — status: ${res.status}`);
    return null;
  }

  return JSON.parse(res.body).accessToken;
}

/**
 * Construit les headers HTTP avec le token Bearer.
 *
 * @param {string} token - Le token JWT
 * @returns {Object} Headers HTTP
 */
export function authHeaders(token) {
  return {
    'Authorization': `Bearer ${token}`,
    'Content-Type':  'application/json',
    'ngrok-skip-browser-warning': 'true'
  };
}

/**
 * Construit les headers HTTP pour multipart/form-data (sans Content-Type).
 * k6 génère automatiquement le Content-Type multipart avec boundary.
 *
 * @param {string} token - Le token JWT
 * @returns {Object} Headers HTTP
 */
export function authHeadersMultipart(token) {
  return {
    'Authorization': `Bearer ${token}`,
    'ngrok-skip-browser-warning': 'true'
    // Pas de Content-Type : k6 le génère automatiquement en multipart/form-data
  };
}

/**
 * Récupère les tokens pour plusieurs rôles en une seule passe.
 * Utile pour les scénarios multi-rôles.
 *
 * @returns {{ admin: string, agent: string, responsable: string }}
 */
export function getAllTokens() {
  return {
    admin:       getToken('admin'),
    agent:       getToken('agent'),
    responsable: getToken('responsable')
  };
}
