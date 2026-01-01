-- =========================
-- TABLE: role
-- =========================
CREATE TABLE tools_core.role (
    id           BIGSERIAL PRIMARY KEY,
    code         VARCHAR(50)  NOT NULL UNIQUE,
    name         VARCHAR(100) NOT NULL,
    description  TEXT,
    is_active    BOOLEAN     NOT NULL DEFAULT TRUE,
    created_at   TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at   TIMESTAMPTZ NOT NULL DEFAULT now()
);

COMMENT ON TABLE tools_core.role IS
'Table de référence des rôles (globaux ou contextuels). Données pures, sans logique.';

COMMENT ON COLUMN tools_core.role.id IS 'Identifiant technique du rôle';
COMMENT ON COLUMN tools_core.role.code IS 'Code métier stable et unique du rôle (ex: APP_ADMIN, MODULE_USER)';
COMMENT ON COLUMN tools_core.role.name IS 'Nom lisible du rôle';
COMMENT ON COLUMN tools_core.role.description IS 'Description fonctionnelle du rôle';
COMMENT ON COLUMN tools_core.role.is_active IS 'Indique si le rôle est actif et attribuable';
COMMENT ON COLUMN tools_core.role.created_at IS 'Date de création du rôle';
COMMENT ON COLUMN tools_core.role.updated_at IS 'Date de dernière mise à jour du rôle';

-- =========================
-- TABLE: module
-- =========================
CREATE TABLE tools_core.module (
    id           BIGSERIAL PRIMARY KEY,
    code         VARCHAR(50)  NOT NULL UNIQUE,
    name         VARCHAR(100) NOT NULL,
    description  TEXT,
    is_active    BOOLEAN     NOT NULL DEFAULT TRUE,
    created_at   TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at   TIMESTAMPTZ NOT NULL DEFAULT now()
);

COMMENT ON TABLE tools_core.module IS
'Modules fonctionnels de l’application (activation, rattachement des rôles et configs).';

COMMENT ON COLUMN tools_core.module.id IS 'Identifiant technique du module';
COMMENT ON COLUMN tools_core.module.code IS 'Code stable et unique du module';
COMMENT ON COLUMN tools_core.module.name IS 'Nom lisible du module';
COMMENT ON COLUMN tools_core.module.description IS 'Description fonctionnelle du module';
COMMENT ON COLUMN tools_core.module.is_active IS 'Indique si le module est actif (visible/utilisable)';
COMMENT ON COLUMN tools_core.module.created_at IS 'Date de création du module';
COMMENT ON COLUMN tools_core.module.updated_at IS 'Date de dernière mise à jour du module';

-- =========================
-- TABLE: user_role (rôles globaux)
-- =========================
CREATE TABLE tools_core.user_role (
    user_id     BIGINT      NOT NULL,
    role_id     BIGINT      NOT NULL,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    PRIMARY KEY (user_id, role_id)
);

COMMENT ON TABLE tools_core.user_role IS
'Liaison utilisateur ↔ rôle global (application).';

COMMENT ON COLUMN tools_core.user_role.user_id IS 'Identifiant de l’utilisateur';
COMMENT ON COLUMN tools_core.user_role.role_id IS 'Identifiant du rôle global';
COMMENT ON COLUMN tools_core.user_role.created_at IS 'Date d’attribution du rôle à l’utilisateur';

-- =========================
-- TABLE: user_module_role (rôles contextuels)
-- =========================
CREATE TABLE tools_core.user_module_role (
    user_id     BIGINT      NOT NULL,
    module_id   BIGINT      NOT NULL,
    role_id     BIGINT      NOT NULL,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    PRIMARY KEY (user_id, module_id, role_id)
);

COMMENT ON TABLE tools_core.user_module_role IS
'Liaison utilisateur ↔ module ↔ rôle contextuel (par module).';

COMMENT ON COLUMN tools_core.user_module_role.user_id IS 'Identifiant de l’utilisateur';
COMMENT ON COLUMN tools_core.user_module_role.module_id IS 'Identifiant du module';
COMMENT ON COLUMN tools_core.user_module_role.role_id IS 'Identifiant du rôle contextuel';
COMMENT ON COLUMN tools_core.user_module_role.created_at IS 'Date d’attribution du rôle sur le module';

-- =========================
-- TABLE: config
-- =========================
CREATE TABLE tools_core.config (
    id                 BIGSERIAL PRIMARY KEY,
    code               VARCHAR(100) NOT NULL UNIQUE,
    scope              VARCHAR(20)  NOT NULL, -- APP | MODULE
    module_id          BIGINT,
    value_type         VARCHAR(20)  NOT NULL,
    default_value      TEXT,
    required_role_id   BIGINT,
    is_active          BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at         TIMESTAMPTZ  NOT NULL DEFAULT now(),
    updated_at         TIMESTAMPTZ  NOT NULL DEFAULT now()
);

COMMENT ON TABLE tools_core.config IS
'Configuration applicative. Les configs ne sont pas des droits.';

COMMENT ON COLUMN tools_core.config.id IS 'Identifiant technique de la configuration';
COMMENT ON COLUMN tools_core.config.code IS 'Code unique de la configuration';
COMMENT ON COLUMN tools_core.config.scope IS 'Portée de la configuration (APP ou MODULE)';
COMMENT ON COLUMN tools_core.config.module_id IS 'Module concerné si scope = MODULE, NULL sinon';
COMMENT ON COLUMN tools_core.config.value_type IS 'Type de valeur (STRING, BOOL, INT, JSON…)';
COMMENT ON COLUMN tools_core.config.default_value IS 'Valeur par défaut de la configuration';
COMMENT ON COLUMN tools_core.config.required_role_id IS 'Rôle requis pour modifier la configuration';
COMMENT ON COLUMN tools_core.config.is_active IS 'Indique si la configuration est active';
COMMENT ON COLUMN tools_core.config.created_at IS 'Date de création de la configuration';
COMMENT ON COLUMN tools_core.config.updated_at IS 'Date de dernière mise à jour de la configuration';

-- =========================
-- TABLE: user_config_override
-- =========================
CREATE TABLE tools_core.user_config_override (
    user_id     BIGINT      NOT NULL,
    config_id   BIGINT      NOT NULL,
    value       TEXT        NOT NULL,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    PRIMARY KEY (user_id, config_id)
);

COMMENT ON TABLE tools_core.user_config_override IS
'Surcharge de configuration par utilisateur (contrôlée par l’application).';

COMMENT ON COLUMN tools_core.user_config_override.user_id IS 'Identifiant de l’utilisateur';
COMMENT ON COLUMN tools_core.user_config_override.config_id IS 'Identifiant de la configuration';
COMMENT ON COLUMN tools_core.user_config_override.value IS 'Valeur surchargée pour l’utilisateur';
COMMENT ON COLUMN tools_core.user_config_override.created_at IS 'Date de création de la surcharge';

-- =========================
-- RÔLES
-- =========================
INSERT INTO tools_core.role (code, name, description)
VALUES
    ('READ_ONLY', 'Lecture seule',       'Accès en lecture seule'),
    ('USER',      'Utilisateur',         'Usage fonctionnel standard'),
    ('MODERATOR', 'Modérateur',           'Actions métier intermédiaires'),
    ('ADMIN',     'Administrateur',       'Administration fonctionnelle'),
    ('TECH',      'Technique',            'Accès technique (imports, jobs, intégrations)'),
    ('OWNER',     'Propriétaire',         'Le GOD');

-- Ajout des contraintes en cascade
-- user / role
ALTER TABLE tools_core.user_role
ADD CONSTRAINT fk_user_role_user
FOREIGN KEY (user_id)
REFERENCES tools_core.users(id)
ON DELETE CASCADE;

ALTER TABLE tools_core.user_role
ADD CONSTRAINT fk_user_role_role
FOREIGN KEY (role_id)
REFERENCES tools_core.role(id)
ON DELETE CASCADE;

-- user / module / role
ALTER TABLE tools_core.user_module_role
ADD CONSTRAINT fk_umr_user
FOREIGN KEY (user_id)
REFERENCES tools_core.users(id)
ON DELETE CASCADE;

ALTER TABLE tools_core.user_module_role
ADD CONSTRAINT fk_umr_module
FOREIGN KEY (module_id)
REFERENCES tools_core.module(id)
ON DELETE CASCADE;

ALTER TABLE tools_core.user_module_role
ADD CONSTRAINT fk_umr_role
FOREIGN KEY (role_id)
REFERENCES tools_core.role(id)
ON DELETE CASCADE;

-- config / module
ALTER TABLE tools_core.config
ADD CONSTRAINT fk_config_module
FOREIGN KEY (module_id)
REFERENCES tools_core.module(id)
ON DELETE CASCADE;

-- config / role
-- si un rôle disparait, la config reste mais n'est plus modifiable
ALTER TABLE tools_core.config
ADD CONSTRAINT fk_config_required_role
FOREIGN KEY (required_role_id)
REFERENCES tools_core.role(id)
ON DELETE SET NULL;

-- user / config
ALTER TABLE tools_core.user_config_override
ADD CONSTRAINT fk_uco_user
FOREIGN KEY (user_id)
REFERENCES tools_core.users(id)
ON DELETE CASCADE;

ALTER TABLE tools_core.user_config_override
ADD CONSTRAINT fk_uco_config
FOREIGN KEY (config_id)
REFERENCES tools_core.config(id)
ON DELETE CASCADE;

-- correctif au niveau des FK pour qu'elles soient vérifiés au commit et pas avant
ALTER TABLE tools_core.user_role
DROP CONSTRAINT fk_user_role_user;

ALTER TABLE tools_core.user_role
ADD CONSTRAINT fk_user_role_user
FOREIGN KEY (user_id)
REFERENCES tools_core.users(id)
ON DELETE CASCADE
DEFERRABLE INITIALLY DEFERRED;


ALTER TABLE tools_core.user_role
DROP CONSTRAINT fk_user_role_role;

ALTER TABLE tools_core.user_role
ADD CONSTRAINT fk_user_role_role
FOREIGN KEY (role_id)
REFERENCES tools_core.role(id)
ON DELETE CASCADE
DEFERRABLE INITIALLY DEFERRED;

ALTER TABLE tools_core.user_module_role
DROP CONSTRAINT fk_umr_user;

ALTER TABLE tools_core.user_module_role
ADD CONSTRAINT fk_umr_user
FOREIGN KEY (user_id)
REFERENCES tools_core.users(id)
ON DELETE CASCADE
DEFERRABLE INITIALLY DEFERRED;


ALTER TABLE tools_core.user_module_role
DROP CONSTRAINT fk_umr_module;

ALTER TABLE tools_core.user_module_role
ADD CONSTRAINT fk_umr_module
FOREIGN KEY (module_id)
REFERENCES tools_core.module(id)
ON DELETE CASCADE
DEFERRABLE INITIALLY DEFERRED;


ALTER TABLE tools_core.user_module_role
DROP CONSTRAINT fk_umr_role;

ALTER TABLE tools_core.user_module_role
ADD CONSTRAINT fk_umr_role
FOREIGN KEY (role_id)
REFERENCES tools_core.role(id)
ON DELETE CASCADE
DEFERRABLE INITIALLY DEFERRED;

ALTER TABLE tools_core.config
DROP CONSTRAINT fk_config_module;

ALTER TABLE tools_core.config
ADD CONSTRAINT fk_config_module
FOREIGN KEY (module_id)
REFERENCES tools_core.module(id)
ON DELETE CASCADE
DEFERRABLE INITIALLY DEFERRED;

ALTER TABLE tools_core.config
DROP CONSTRAINT fk_config_required_role;

ALTER TABLE tools_core.config
ADD CONSTRAINT fk_config_required_role
FOREIGN KEY (required_role_id)
REFERENCES tools_core.role(id)
ON DELETE SET NULL
DEFERRABLE INITIALLY DEFERRED;

ALTER TABLE tools_core.user_config_override
DROP CONSTRAINT fk_uco_user;

ALTER TABLE tools_core.user_config_override
ADD CONSTRAINT fk_uco_user
FOREIGN KEY (user_id)
REFERENCES tools_core.users(id)
ON DELETE CASCADE
DEFERRABLE INITIALLY DEFERRED;


ALTER TABLE tools_core.user_config_override
DROP CONSTRAINT fk_uco_config;

ALTER TABLE tools_core.user_config_override
ADD CONSTRAINT fk_uco_config
FOREIGN KEY (config_id)
REFERENCES tools_core.config(id)
ON DELETE CASCADE
DEFERRABLE INITIALLY DEFERRED;