# Tests de performance FinAssist — k6

## Structure

```
performance/
└── k6/
    ├── config.js                    # Configuration globale, seuils, profils de charge
    ├── helpers/
    │   └── auth-helper.js           # Récupération du token JWT
    └── scenarios/
        ├── smoke.js                 # Test de fumée (1 user, 30s) — à lancer en premier
        ├── auth.js                  # Performance du login
        ├── besoins.js               # CRUD besoins (endpoint le plus sollicité)
        ├── workflow.js              # Validation et circuits (lecture)
        ├── workflow-complet.js      # Cycle complet : créer → soumettre → valider ← NOUVEAU
        ├── reporting.js             # Dashboard et statistiques (endpoints lourds)
        ├── admin.js                 # Users, rôles, logs, catégories
        ├── settings-profil.js       # Paramètres, profil, ma-signature ← NOUVEAU
        ├── full-scenario.js         # Parcours utilisateur complet (le plus réaliste)
        ├── spike.js                 # Test de pic — montée brutale ← NOUVEAU
        ├── soak.js                  # Test d'endurance — 14 minutes ← NOUVEAU
        └── stress.js                # Test de stress — trouver le point de rupture
```

---

## Prérequis

### 1. Installer k6

```bash
# Windows (Chocolatey)
choco install k6

# Windows (winget)
winget install k6

# Vérifier l'installation
k6 version
```

### 2. Démarrer l'application

```bash
docker compose up -d
```

Attendre que le backend soit prêt : `http://localhost:8080/api/health` doit répondre.

### 3. Créer les utilisateurs de test

Avant de lancer les tests, créer ces 3 utilisateurs dans la base via l'interface admin ou directement en SQL :

| Email | Mot de passe | Rôle |
|---|---|---|
| `admin@finstar-cm.com` | `Admin@2024!` | Administrateur |
| `agent@finstar-cm.com` | `Agent@2024!` | Agent |
| `responsable@finstar-cm.com` | `Resp@2024!` | Responsable |

> Adapter les credentials dans `performance/k6/config.js` si nécessaire.

---

## Lancer les tests

### Ordre recommandé

```bash
# 1. Test de fumée — vérifier que tout fonctionne (obligatoire en premier)
k6 run performance/k6/scenarios/smoke.js

# 2. Test de charge normale — scénario le plus réaliste
k6 run performance/k6/scenarios/full-scenario.js

# 3. Tests par module
k6 run performance/k6/scenarios/auth.js
k6 run performance/k6/scenarios/besoins.js
k6 run performance/k6/scenarios/reporting.js
k6 run performance/k6/scenarios/workflow.js
k6 run performance/k6/scenarios/admin.js

# 4. Cycle de validation complet (créer → soumettre → valider)
k6 run performance/k6/scenarios/workflow-complet.js

# 5. Paramètres et profil utilisateur
k6 run performance/k6/scenarios/settings-profil.js

# 6. Test de pic (montée brutale)
k6 run performance/k6/scenarios/spike.js

# 7. Test d'endurance (14 minutes — à lancer en dernier)
k6 run performance/k6/scenarios/soak.js

# 8. Test de stress — trouver la limite (en tout dernier)
k6 run performance/k6/scenarios/stress.js
```

### Avec rapport JSON (pour analyse)

```bash
k6 run --out json=performance/results/besoins-results.json performance/k6/scenarios/besoins.js
```

### Avec rapport HTML (nécessite k6-reporter)

```bash
# Installer k6-reporter
npm install -g k6-reporter

# Lancer avec sortie JSON
k6 run --out json=results.json performance/k6/scenarios/full-scenario.js

# Générer le rapport HTML
k6-reporter results.json
```

---

## Seuils (SLA FinAssist)

| Métrique | Seuil normal | Seuil stress |
|---|---|---|
| Temps de réponse p95 | < 500ms | < 2000ms |
| Taux d'erreur HTTP | < 1% | < 5% |
| Taux de checks réussis | > 95% | > 90% |
| Login p95 | < 300ms | — |
| Dashboard p95 | < 800ms | — |
| Export Excel p95 | < 3000ms | — |

---

## Interpréter les résultats

Après chaque test, k6 affiche un résumé :

```
✓ checks.........................: 98.50%  ✓ 985  ✗ 15
  data_received..................: 2.1 MB  35 kB/s
  data_sent......................: 450 kB  7.5 kB/s
  http_req_blocked...............: avg=1.2ms   p(95)=2.1ms
  http_req_duration..............: avg=145ms   p(95)=380ms   p(99)=620ms
  http_req_failed................: 0.50%   ✓ 5    ✗ 995
  http_reqs......................: 1000    16.6/s
  vus............................: 20      min=1  max=20
```

**Points clés à surveiller :**
- `http_req_duration p(95)` — doit rester sous les seuils définis
- `http_req_failed` — doit rester sous 1%
- `checks` — doit rester au-dessus de 95%
- `http_reqs/s` — le débit (requêtes par seconde)

---

## Profils de charge disponibles

Modifier `config.js` pour changer le profil dans chaque scénario :

| Profil | Utilisateurs | Durée | Usage |
|---|---|---|---|
| `SMOKE_OPTIONS` | 1 | 30s | Vérification rapide |
| `LOAD_OPTIONS` | 5→20 | ~2m30 | Test normal |
| `STRESS_OPTIONS` | 10→100 | ~4m | Trouver la limite |
| `SPIKE_OPTIONS` | 1→50 | ~1m30 | Test de pic brutal |
| `SOAK_OPTIONS` | 10 | ~14m | Test d'endurance |
