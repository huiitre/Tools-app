-- Référentiel des actions de priorité de travail (scrapé depuis
-- https://paldb.cc/fr/Work_Priority#WorkPriority par l'extractor, work_priority.json).
--
-- Distinct de work_suitability (les 12 métiers/aptitudes qu'un Pal peut avoir) : ici ce sont les
-- actions concrètes que l'IA d'un Pal exécute (ex: "Extraction" / ProductResource_Mining, priorité 14),
-- plusieurs actions pouvant partager le même métier de base (ex: Mining a deux actions différentes,
-- priorités 14 et 15). Certaines actions (Protection, Sauvetage) n'ont aucun métier de base équivalent
-- (work_suitability_id NULL).
--
-- Pas de pivot vers pal : la priorité d'un Pal pour un métier donné se déduit de
-- pal_work_suitability (pal -> métier) jointe à work_priority (métier -> actions/priorités).
CREATE TABLE tools_palworld.work_priority (
    id                  BIGSERIAL PRIMARY KEY,
    code                VARCHAR(100) NOT NULL UNIQUE,
    name                VARCHAR(100) NOT NULL,
    icon_url            TEXT,
    work_suitability_id BIGINT REFERENCES tools_palworld.work_suitability(id),
    priority            SMALLINT NOT NULL
);

COMMENT ON TABLE  tools_palworld.work_priority             IS 'Actions de priorité de travail (échelle globale de l''IA des Pals), scrapées depuis paldb.cc/Work_Priority';
COMMENT ON COLUMN tools_palworld.work_priority.code        IS 'Identifiant technique stable de l''action côté jeu (ex: ProductResource_Mining), clé de matching à la synchro';
COMMENT ON COLUMN tools_palworld.work_priority.work_suitability_id IS 'Métier de base associé, NULL pour les actions sans équivalent (ex: Protection, Sauvetage)';
COMMENT ON COLUMN tools_palworld.work_priority.priority    IS 'Rang de priorité brut de la source (plus petit = prioritaire), échelle partagée par toutes les actions';

CREATE INDEX idx_work_priority_work_suitability ON tools_palworld.work_priority(work_suitability_id);
