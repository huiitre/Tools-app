-- =========================
-- SCHEMA : tools_elite_dangerous
-- =========================
CREATE SCHEMA IF NOT EXISTS tools_elite_dangerous;

COMMENT ON SCHEMA tools_elite_dangerous IS 'Schéma du module Elite Dangerous';

-- =========================
-- MODULE
-- =========================
INSERT INTO tools_core.module (code, name, description, is_active)
VALUES ('TOOLS_ELITE_DANGEROUS', 'Elite Dangerous', 'Module pour les outils Elite Dangerous', TRUE);

-- =========================
-- TABLE: r2r_expedition
-- =========================
CREATE TABLE tools_elite_dangerous.r2r_expedition (
    id                   UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id              BIGINT       NOT NULL REFERENCES tools_core.users(id) ON DELETE CASCADE,
    name                 VARCHAR(255) NOT NULL,
    source               VARCHAR(50)  NOT NULL DEFAULT 'spansh',
    route_data           JSONB        NOT NULL,
    current_system_index INTEGER      NOT NULL DEFAULT 0,
    current_bodies_done  BIGINT[]     NOT NULL DEFAULT '{}',
    created_at           TIMESTAMPTZ  NOT NULL DEFAULT now(),
    updated_at           TIMESTAMPTZ  NOT NULL DEFAULT now()
);

COMMENT ON TABLE  tools_elite_dangerous.r2r_expedition                          IS 'Expéditions Road to Riches importées par les utilisateurs';
COMMENT ON COLUMN tools_elite_dangerous.r2r_expedition.id                       IS 'Identifiant unique de l''expédition';
COMMENT ON COLUMN tools_elite_dangerous.r2r_expedition.user_id                  IS 'Utilisateur propriétaire de l''expédition';
COMMENT ON COLUMN tools_elite_dangerous.r2r_expedition.name                     IS 'Nom de l''expédition (généré depuis le nom de fichier ou saisi manuellement)';
COMMENT ON COLUMN tools_elite_dangerous.r2r_expedition.source                   IS 'Site source du JSON importé (ex: spansh)';
COMMENT ON COLUMN tools_elite_dangerous.r2r_expedition.route_data               IS 'Données complètes de la route au format normalisé (JSONB)';
COMMENT ON COLUMN tools_elite_dangerous.r2r_expedition.current_system_index     IS 'Index du système courant — tous les systèmes avant cet index sont implicitement terminés';
COMMENT ON COLUMN tools_elite_dangerous.r2r_expedition.current_bodies_done      IS 'body_id64 des corps déjà cartographiés dans le système courant';
COMMENT ON COLUMN tools_elite_dangerous.r2r_expedition.created_at               IS 'Date d''import de l''expédition';
COMMENT ON COLUMN tools_elite_dangerous.r2r_expedition.updated_at               IS 'Date de dernière mise à jour de la progression';

-- =========================
-- INDEX
-- =========================
CREATE INDEX idx_r2r_expedition_user_id   ON tools_elite_dangerous.r2r_expedition(user_id);