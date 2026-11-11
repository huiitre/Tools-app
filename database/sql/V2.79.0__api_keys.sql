-- Clés d'API des appelants de service à service (extracteurs, autres services du NAS).
--
-- La clé en clair n'est montrée qu'une fois, à sa création : seul son SHA-256 est stocké. Une clé
-- aléatoire ne se devine pas, un hash lent n'apporterait rien et empêcherait la recherche par index.
--
-- Une clé n'est jamais supprimée mais révoquée : l'historique et les journaux continuent de pointer
-- vers elle. Elle est valide tant qu'elle n'est ni révoquée ni expirée.

CREATE TABLE tools_core.api_keys (
    id            BIGSERIAL    PRIMARY KEY,
    name          VARCHAR(100) NOT NULL,

    key_hash      BYTEA        NOT NULL,
    key_prefix    VARCHAR(32)  NOT NULL,

    created_at    TIMESTAMPTZ  NOT NULL DEFAULT now(),
    created_by    BIGINT       NOT NULL,
    expires_at    TIMESTAMPTZ,
    revoked_at    TIMESTAMPTZ,

    last_used_at  TIMESTAMPTZ,
    last_used_ip  INET,

    CONSTRAINT uq_api_keys_key_hash UNIQUE (key_hash),

    CONSTRAINT ck_api_keys_key_hash_length CHECK (octet_length(key_hash) = 32),
    CONSTRAINT ck_api_keys_expires_after_creation CHECK (expires_at IS NULL OR expires_at > created_at),

    CONSTRAINT fk_api_keys_created_by
        FOREIGN KEY (created_by) REFERENCES tools_core.users (id) ON DELETE RESTRICT
);

-- Un nom ne désigne qu'une clé active ; une clé révoquée libère le sien pour sa remplaçante.
CREATE UNIQUE INDEX uq_api_keys_active_name
    ON tools_core.api_keys (name)
    WHERE revoked_at IS NULL;

COMMENT ON TABLE tools_core.api_keys IS
    'Clés d''API des appelants de service à service ; jamais supprimées, seulement révoquées.';

COMMENT ON COLUMN tools_core.api_keys.name IS
    'Nom donné à la clé dans l''administration, unique parmi les clés non révoquées.';

COMMENT ON COLUMN tools_core.api_keys.key_hash IS
    'SHA-256 de la clé complète (32 octets) ; la clé en clair n''est jamais stockée.';

COMMENT ON COLUMN tools_core.api_keys.key_prefix IS
    'Premiers caractères de la clé, en clair, pour la reconnaître sans la révéler.';

COMMENT ON COLUMN tools_core.api_keys.created_by IS
    'Administrateur qui a créé la clé.';

COMMENT ON COLUMN tools_core.api_keys.expires_at IS
    'Fin de validité ; NULL pour une clé sans expiration.';

COMMENT ON COLUMN tools_core.api_keys.revoked_at IS
    'Date de révocation ; NULL tant que la clé n''est pas révoquée.';

COMMENT ON COLUMN tools_core.api_keys.last_used_at IS
    'Dernière utilisation, rafraîchie au plus une fois par intervalle et non à chaque requête.';

COMMENT ON COLUMN tools_core.api_keys.last_used_ip IS
    'Adresse IP de la dernière utilisation.';

-- Droits d'une clé. Le catalogue des droits vit dans le code : la base ne stocke que leur nom.
-- Une clé n'ouvre que les use cases qui déclarent explicitement l'un de ses droits.

CREATE TABLE tools_core.api_key_scopes (
    api_key_id  BIGINT       NOT NULL,
    scope       VARCHAR(100) NOT NULL,

    CONSTRAINT pk_api_key_scopes PRIMARY KEY (api_key_id, scope),

    -- Forme ressource:action, en minuscules, par exemple core.mail:send ou temtem:sync.
    CONSTRAINT ck_api_key_scopes_format CHECK (scope ~ '^[a-z][a-z0-9_.]*:[a-z][a-z0-9_]*$'),

    CONSTRAINT fk_api_key_scopes_api_key
        FOREIGN KEY (api_key_id) REFERENCES tools_core.api_keys (id) ON DELETE CASCADE
);

COMMENT ON TABLE tools_core.api_key_scopes IS
    'Droits accordés à chaque clé d''API, au format ressource:action défini par le catalogue du code.';

COMMENT ON COLUMN tools_core.api_key_scopes.scope IS
    'Nom du droit, par exemple core.mail:send ; un nom publié ne se renomme jamais.';
