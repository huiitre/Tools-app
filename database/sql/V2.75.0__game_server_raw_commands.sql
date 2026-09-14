-- Audit des commandes RCON libres exécutées depuis la console (voir IGameServerRawCommand).
-- Cette fonctionnalité équivaut à un accès admin total au jeu : on trace qui a tapé quoi, sur
-- quel serveur, et ce que le jeu a répondu. Aucune purge : c'est un audit, pas un cache — seule
-- la lecture est bornée (LIMIT côté requête).

CREATE TABLE tools_core.game_server_raw_commands (
    id              BIGSERIAL PRIMARY KEY,
    game_server_id  BIGINT NOT NULL,
    user_id         BIGINT NOT NULL,
    command         TEXT NOT NULL,
    answer          TEXT,
    executed_at     TIMESTAMPTZ NOT NULL DEFAULT now(),

    CONSTRAINT fk_game_server_raw_commands_server
        FOREIGN KEY (game_server_id) REFERENCES tools_core.game_servers (id) ON DELETE CASCADE,

    CONSTRAINT fk_game_server_raw_commands_user
        FOREIGN KEY (user_id) REFERENCES tools_core.users (id) ON DELETE CASCADE
);

CREATE INDEX ix_game_server_raw_commands_server
    ON tools_core.game_server_raw_commands (game_server_id, executed_at DESC);

COMMENT ON TABLE tools_core.game_server_raw_commands IS
    'Audit des commandes RCON libres exécutées via la console admin, avec leur réponse.';
COMMENT ON COLUMN tools_core.game_server_raw_commands.answer IS
    'Réponse brute du serveur de jeu ; NULL quand le protocole ferme la connexion sans répondre.';
