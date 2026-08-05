-- =========================================================================
-- MODULE PALWORLD : moteur de reproduction (breeding)
-- =========================================================================
-- combi_duplicate_priority / ignore_combi viennent du pak (pals.json), au
-- même titre que combi_rank déjà présent. La table breeding_exception porte
-- les 258 combinaisons spéciales de breeding.json, prioritaires sur la
-- formule combiRank. Aucune table de toutes les paires possibles n'est
-- persistée : l'API reconstruit un index en mémoire à partir de ces deux
-- sources (cf. modules/palworld/breeding/**).
-- =========================================================================

ALTER TABLE tools_palworld.pal ADD COLUMN combi_duplicate_priority INTEGER;
ALTER TABLE tools_palworld.pal ADD COLUMN ignore_combi BOOLEAN NOT NULL DEFAULT FALSE;

COMMENT ON COLUMN tools_palworld.pal.combi_duplicate_priority IS 'Départage en cas d''égalité de distance au combiRank cible (breeding) : le plus élevé gagne';
COMMENT ON COLUMN tools_palworld.pal.ignore_combi IS 'Si vrai, ce Pal ne peut jamais être choisi comme enfant issu de la formule normale de reproduction (uniquement via une exception explicite de breeding_exception)';

CREATE TABLE tools_palworld.breeding_exception (
    id                BIGSERIAL PRIMARY KEY,
    parent_a_pal_id   BIGINT NOT NULL REFERENCES tools_palworld.pal(id) ON DELETE CASCADE,
    parent_a_gender   VARCHAR(10) CHECK (parent_a_gender IN ('Male', 'Female')),
    parent_b_pal_id   BIGINT NOT NULL REFERENCES tools_palworld.pal(id) ON DELETE CASCADE,
    parent_b_gender   VARCHAR(10) CHECK (parent_b_gender IN ('Male', 'Female')),
    child_pal_id      BIGINT NOT NULL REFERENCES tools_palworld.pal(id) ON DELETE CASCADE,
    UNIQUE (parent_a_pal_id, parent_a_gender, parent_b_pal_id, parent_b_gender)
);

COMMENT ON TABLE tools_palworld.breeding_exception IS 'Combinaisons de reproduction explicites (source: breeding.json), prioritaires sur la formule combiRank. parent_a/parent_b sans genre = exception symétrique';
COMMENT ON COLUMN tools_palworld.breeding_exception.parent_a_gender IS 'NULL = s''applique quel que soit le sexe du parent A';
COMMENT ON COLUMN tools_palworld.breeding_exception.parent_b_gender IS 'NULL = s''applique quel que soit le sexe du parent B';
