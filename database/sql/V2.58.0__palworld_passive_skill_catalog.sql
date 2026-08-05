CREATE TABLE tools_palworld.passive_skill (
    id                VARCHAR(255) PRIMARY KEY,
    name              VARCHAR(255) NOT NULL,
    description       TEXT,
    rank              SMALLINT     NOT NULL,
    rank_icon_url     TEXT,
    is_negative       BOOLEAN      NOT NULL,
    is_world_tree     BOOLEAN      NOT NULL,
    raw_payload       JSONB        NOT NULL,
    created_at        TIMESTAMPTZ  NOT NULL DEFAULT now(),
    updated_at        TIMESTAMPTZ  NOT NULL DEFAULT now()
);

COMMENT ON TABLE tools_palworld.passive_skill IS
    'Catalogue des passifs Palworld affichables en jeu, identifié par la clé technique des sauvegardes serveur.';
COMMENT ON COLUMN tools_palworld.passive_skill.id IS
    'Identifiant technique du client et de pal_instance.passive_skill_ids (ex: Legend).';
COMMENT ON COLUMN tools_palworld.passive_skill.raw_payload IS
    'Entrée client brute conservée pour les effets et propriétés non encore exposés.';

CREATE INDEX passive_skill_rank_idx ON tools_palworld.passive_skill (rank);
