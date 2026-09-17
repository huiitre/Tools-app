-- Journal applicatif transverse. Une ligne décrit un événement fonctionnel ou une interaction
-- utilisateur ; les erreurs et diagnostics techniques restent dans Serilog.
--
-- Le journal est append-only côté application : aucune route ni aucun repository ne doit proposer
-- sa modification ou sa suppression. Une éventuelle suppression reste une opération manuelle en
-- base de données.

CREATE TABLE tools_core.application_logs (
    id           BIGSERIAL PRIMARY KEY,
    created_at   TIMESTAMPTZ NOT NULL DEFAULT now(),

    -- NULL désigne le Core ; une valeur désigne un module fonctionnel de tools_core.module.
    module_id    BIGINT,
    area_code    VARCHAR(50)  NOT NULL,
    action_code  VARCHAR(100) NOT NULL,

    -- NULL pour un événement anonyme ou produit par le système.
    user_id      BIGINT,
    ip_address   INET,
    user_agent   TEXT,

    -- Contexte libre propre à l'événement, sans donnée secrète.
    metadata     JSONB NOT NULL DEFAULT '{}'::jsonb,

    CONSTRAINT fk_application_logs_module
        FOREIGN KEY (module_id) REFERENCES tools_core.module (id) ON DELETE RESTRICT,

    CONSTRAINT fk_application_logs_user
        FOREIGN KEY (user_id) REFERENCES tools_core.users (id) ON DELETE RESTRICT
);

-- Consultation globale, de la plus récente à la plus ancienne.
CREATE INDEX ix_application_logs_created_at
    ON tools_core.application_logs (created_at DESC);

-- Filtres successifs de l'administration : module, zone puis action.
-- PostgreSQL indexe aussi les valeurs NULL : le filtre Core (module_id IS NULL) en bénéficie.
CREATE INDEX ix_application_logs_classification
    ON tools_core.application_logs (module_id, area_code, action_code, created_at DESC);

-- Historique d'activité d'un utilisateur identifié.
CREATE INDEX ix_application_logs_user
    ON tools_core.application_logs (user_id, created_at DESC)
    WHERE user_id IS NOT NULL;

COMMENT ON TABLE tools_core.application_logs IS
    'Journal applicatif transverse, immuable côté application et consultable par l''administration.';

COMMENT ON COLUMN tools_core.application_logs.module_id IS
    'Module fonctionnel concerné ; NULL désigne le Core.';

COMMENT ON COLUMN tools_core.application_logs.area_code IS
    'Sous-module stable de l''événement, par exemple AUTH, USERS, TEAMS ou VALORANT.';

COMMENT ON COLUMN tools_core.application_logs.action_code IS
    'Action stable de l''événement, par exemple LOGIN, LOGOUT ou MEMBER_ADDED.';

COMMENT ON COLUMN tools_core.application_logs.user_id IS
    'Utilisateur à l''origine de l''événement ; NULL pour un visiteur anonyme ou le système.';

COMMENT ON COLUMN tools_core.application_logs.metadata IS
    'Contexte JSON libre propre à l''événement ; ne doit contenir aucun mot de passe, jeton ou secret.';
