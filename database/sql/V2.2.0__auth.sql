DROP SCHEMA IF EXISTS tools_core CASCADE;

CREATE SCHEMA IF NOT EXISTS tools_core;

COMMENT ON SCHEMA tools_core IS 'Schéma principal du core Tools : utilisateurs, authentification, autorisations et données transverses';

CREATE TABLE tools_core.users (
    id              BIGSERIAL PRIMARY KEY,
    name            VARCHAR(255) NOT NULL,
    email           VARCHAR(100) NOT NULL,
    is_active       BOOLEAN NOT NULL DEFAULT FALSE,
    user_type       VARCHAR(20) NOT NULL DEFAULT 'HUMAN',
    created_at      TIMESTAMP NOT NULL DEFAULT now(),
    updated_at      TIMESTAMP
);

COMMENT ON TABLE tools_core.users IS 'Identité interne des utilisateurs (indépendante des méthodes d’authentification)';

COMMENT ON COLUMN tools_core.users.id IS 'Identifiant interne de l’utilisateur';
COMMENT ON COLUMN tools_core.users.name IS 'Nom affiché / pseudo utilisateur';
COMMENT ON COLUMN tools_core.users.email IS 'Email principal de l’utilisateur (identité applicative)';
COMMENT ON COLUMN tools_core.users.is_active IS 'Indique si le compte est autorisé à se connecter';
COMMENT ON COLUMN tools_core.users.user_type IS 'Type de compte : HUMAN, APPLICATION, SYSTEM';
COMMENT ON COLUMN tools_core.users.created_at IS 'Date de création du compte';
COMMENT ON COLUMN tools_core.users.updated_at IS 'Date de dernière modification du compte';

CREATE UNIQUE INDEX uq_users_email
    ON tools_core.users (email);

CREATE TABLE tools_core.user_credentials (
    id              BIGSERIAL PRIMARY KEY,
    user_id         BIGINT NOT NULL,
    password_hash   VARCHAR(255) NOT NULL,
    created_at      TIMESTAMP NOT NULL DEFAULT now(),

    CONSTRAINT fk_user_credentials_user
        FOREIGN KEY (user_id)
        REFERENCES tools_core.users (id)
        ON DELETE CASCADE
);

COMMENT ON TABLE tools_core.user_credentials IS 'Secrets d’authentification locaux (email / mot de passe)';

COMMENT ON COLUMN tools_core.user_credentials.id IS 'Identifiant de la ligne de credentials';
COMMENT ON COLUMN tools_core.user_credentials.user_id IS 'Utilisateur associé au mot de passe';
COMMENT ON COLUMN tools_core.user_credentials.password_hash IS 'Mot de passe hashé (bcrypt, argon2, etc.)';
COMMENT ON COLUMN tools_core.user_credentials.created_at IS 'Date de création des credentials';

CREATE UNIQUE INDEX uq_user_credentials_user
    ON tools_core.user_credentials (user_id);

CREATE TABLE tools_core.user_auth_provider (
    id                  BIGSERIAL PRIMARY KEY,
    user_id             BIGINT NOT NULL,
    provider            VARCHAR(30) NOT NULL,
    provider_user_id    VARCHAR(255) NOT NULL,
    provider_email      VARCHAR(255),
    created_at          TIMESTAMP NOT NULL DEFAULT now(),

    CONSTRAINT fk_user_auth_provider_user
        FOREIGN KEY (user_id)
        REFERENCES tools_core.users (id)
        ON DELETE CASCADE
);

COMMENT ON TABLE tools_core.user_auth_provider IS 'Lien entre un utilisateur interne et une identité externe (Google, GitHub, Password, etc.)';

COMMENT ON COLUMN tools_core.user_auth_provider.id IS 'Identifiant interne';
COMMENT ON COLUMN tools_core.user_auth_provider.user_id IS 'Utilisateur interne associé';
COMMENT ON COLUMN tools_core.user_auth_provider.provider IS 'Fournisseur d’authentification : PASSWORD, GOOGLE, GITHUB, etc.';
COMMENT ON COLUMN tools_core.user_auth_provider.provider_user_id IS 'Identifiant unique fourni par le provider (sub, id, etc.)';
COMMENT ON COLUMN tools_core.user_auth_provider.provider_email IS 'Email fourni par le provider (optionnel)';
COMMENT ON COLUMN tools_core.user_auth_provider.created_at IS 'Date de liaison du provider au compte';

CREATE UNIQUE INDEX uq_auth_provider_identity
    ON tools_core.user_auth_provider (provider, provider_user_id);

CREATE UNIQUE INDEX uq_auth_provider_user
    ON tools_core.user_auth_provider (user_id, provider);

ALTER TABLE tools_core.user_credentials
DROP CONSTRAINT fk_user_credentials_user;

ALTER TABLE tools_core.user_credentials
ADD CONSTRAINT fk_user_credentials_user
FOREIGN KEY (user_id)
REFERENCES tools_core.users (id)
ON DELETE CASCADE
DEFERRABLE INITIALLY DEFERRED;

ALTER TABLE tools_core.user_auth_provider
DROP CONSTRAINT fk_user_auth_provider_user;

ALTER TABLE tools_core.user_auth_provider
ADD CONSTRAINT fk_user_auth_provider_user
FOREIGN KEY (user_id)
REFERENCES tools_core.users (id)
ON DELETE CASCADE
DEFERRABLE INITIALLY DEFERRED;