DROP SCHEMA IF EXISTS tools_health CASCADE;

CREATE SCHEMA IF NOT EXISTS tools_health;

COMMENT ON SCHEMA tools_health IS
'Module Health : suivi des données de santé utilisateur (ex: poids).';

-- Table
CREATE TABLE tools_health.weight_log (
    id            BIGSERIAL PRIMARY KEY,
    user_id       BIGINT NOT NULL,
    logged_at     TIMESTAMPTZ NOT NULL,
    weight     NUMERIC(5,2) NOT NULL CHECK (weight > 0),
    notes         TEXT,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at    TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Index utiles
CREATE INDEX idx_weight_log_user_id
    ON tools_health.weight_log (user_id);

CREATE INDEX idx_weight_log_user_logged_at
    ON tools_health.weight_log (user_id, logged_at);

-- Comments
COMMENT ON TABLE tools_health.weight_log IS
'Historique des poids saisis par un utilisateur.';

COMMENT ON COLUMN tools_health.weight_log.id IS
'Identifiant technique.';

COMMENT ON COLUMN tools_health.weight_log.user_id IS
'Identifiant de l’utilisateur (tools_core).';

COMMENT ON COLUMN tools_health.weight_log.logged_at IS
'Date/heure effective de la mesure du poids.';

COMMENT ON COLUMN tools_health.weight_log.weight IS
'Poids mesuré en kilogrammes.';

COMMENT ON COLUMN tools_health.weight_log.notes IS
'Commentaire libre optionnel saisi par l’utilisateur.';

COMMENT ON COLUMN tools_health.weight_log.created_at IS
'Date de création de l’enregistrement.';

COMMENT ON COLUMN tools_health.weight_log.updated_at IS
'Date de dernière mise à jour de l’enregistrement.';

INSERT INTO tools_core.module (
    code,
    name,
    description,
    is_active
)
VALUES (
    'TOOLS_HEALTH',
    'Health',
    'Suivi des données de santé personnelles (poids, historique, visualisation).',
    TRUE
);