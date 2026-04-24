-- ============================================================
-- Migration dynamique : convertit les anciens statuts ROLE{N}
-- en statuts basés sur le RoleRequis réel de l'étape correspondante
-- ============================================================
-- À exécuter UNE SEULE FOIS sur la base de données de production/dev
-- Auteur : FinAssist
-- Date   : 2026-04-08
-- ============================================================

-- Étape 1 : créer une table temporaire de correspondance
-- ordre → rôle réel, basée sur les étapes des circuits actuels
CREATE TABLE #correspondance_statuts (
    ordre      INT,
    role_upper NVARCHAR(100)
);

INSERT INTO #correspondance_statuts (ordre, role_upper)
SELECT DISTINCT
    e.ordre,
    UPPER(e.role_requis)
FROM etapes_circuit e
WHERE e.role_requis IS NOT NULL AND e.role_requis != '';

-- Étape 2 : mettre à jour les statuts EN_ATTENTE_ROLE{N}
UPDATE b
SET b.statut = 'EN_ATTENTE_' + c.role_upper
FROM besoins b
JOIN #correspondance_statuts c
    ON b.statut = 'EN_ATTENTE_ROLE' + CAST(c.ordre AS NVARCHAR)
WHERE b.statut LIKE 'EN_ATTENTE_ROLE%';

-- Étape 3 : mettre à jour les statuts APPROUVE_ROLE{N}
UPDATE b
SET b.statut = 'APPROUVE_PAR_' + c.role_upper
FROM besoins b
JOIN #correspondance_statuts c
    ON b.statut = 'APPROUVE_ROLE' + CAST(c.ordre AS NVARCHAR)
WHERE b.statut LIKE 'APPROUVE_ROLE%';

-- Étape 4 : mettre à jour les statuts REJETE_ROLE{N}
UPDATE b
SET b.statut = 'REJETE_PAR_' + c.role_upper
FROM besoins b
JOIN #correspondance_statuts c
    ON b.statut = 'REJETE_ROLE' + CAST(c.ordre AS NVARCHAR)
WHERE b.statut LIKE 'REJETE_ROLE%';

-- Étape 5 : mettre à jour les statuts SIGNE_ROLE{N}
UPDATE b
SET b.statut = 'SIGNE_PAR_' + c.role_upper
FROM besoins b
JOIN #correspondance_statuts c
    ON b.statut = 'SIGNE_ROLE' + CAST(c.ordre AS NVARCHAR)
WHERE b.statut LIKE 'SIGNE_ROLE%';

-- Étape 6 : nettoyer
DROP TABLE #correspondance_statuts;

-- Étape 7 : vérification — afficher les statuts résiduels non migrés
-- (si ce SELECT retourne des lignes, il y a des besoins orphelins
--  dont l'étape a été supprimée du circuit)
SELECT id, titre, statut
FROM besoins
WHERE statut LIKE '%ROLE%';
