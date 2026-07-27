-- Remplace le scraping palworld.gg de la tierlist (fait en live à chaque
-- affichage, une seule source) par une ingestion en base d'un JSON produit
-- en amont et pouvant contenir plusieurs sources et catégories à la fois.
--
-- On résout systématiquement vers notre pal_id (via tribe) à l'import,
-- jamais de tribe/slug externe stocké ici : on garde des identifiants
-- propres en interne, la table pal_source existante trace déjà la
-- provenance externe par Pal si besoin de recouper.
--
-- Le champ "speed" du JSON tierlist (ex: "500 - 900") est déjà couvert par
-- pal.run_speed / pal.ride_sprint_speed (V2.49.0, confirmé identique sur
-- Melpaca), donc pas de colonne dédiée ici.
--
-- Contrainte unique (pal_id, category, source) : empêche un pal d'avoir
-- deux lignes pour la même catégorie+source, donc mécaniquement aussi deux
-- tiers différents pour la même catégorie+source (tier n'est qu'une colonne
-- de cette même ligne).
CREATE TABLE tools_palworld.pal_tier_ranking (
    pal_id   BIGINT      NOT NULL REFERENCES tools_palworld.pal(id) ON DELETE CASCADE,
    category VARCHAR(50) NOT NULL,
    source   VARCHAR(50) NOT NULL,
    tier     VARCHAR(10) NOT NULL,
    PRIMARY KEY (pal_id, category, source)
);

COMMENT ON TABLE  tools_palworld.pal_tier_ranking          IS 'Classement d''un Pal dans une tierlist, par catégorie et par source (plusieurs sources possibles)';
COMMENT ON COLUMN tools_palworld.pal_tier_ranking.category IS 'Catégorie de classement (ex: general, base-work, flying-mounts, ground-mounts, combat), vocabulaire libre car dépendant de la source';
COMMENT ON COLUMN tools_palworld.pal_tier_ranking.source   IS 'Code de la source du classement (ex: palworld_gg), vocabulaire libre tant qu''une seule source n''impose pas de table de référence';
COMMENT ON COLUMN tools_palworld.pal_tier_ranking.tier     IS 'Lettre de tier brute de la source (ex: S, A, B, C, D), pas de contrainte de valeur car l''échelle peut varier selon la source';

CREATE INDEX idx_pal_tier_ranking_category_source ON tools_palworld.pal_tier_ranking(category, source);
