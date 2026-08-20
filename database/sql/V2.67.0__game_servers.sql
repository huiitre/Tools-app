CREATE TABLE tools_core.game_servers (
    id              BIGSERIAL PRIMARY KEY,
    slug            VARCHAR(100) NOT NULL UNIQUE,
    game_code       VARCHAR(50) NOT NULL,
    protocol_type   VARCHAR(50) NOT NULL,
    server_name     VARCHAR(150) NOT NULL,
    steam_app_id    INTEGER,
    game_name       VARCHAR(150),
    picture_url     VARCHAR(255),
    host            VARCHAR(100) NOT NULL,
    port            INTEGER NOT NULL,
    protocol_config JSONB NOT NULL DEFAULT '{}'::jsonb,
    is_visible      BOOLEAN NOT NULL DEFAULT true,
    last_synced_at  TIMESTAMPTZ NOT NULL,
    online          BOOLEAN,
    num_players     INTEGER,
    max_players     INTEGER,
    checked_at      TIMESTAMPTZ,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);

COMMENT ON COLUMN tools_core.game_servers.id IS
    'Identifiant technique de la ligne.';
COMMENT ON COLUMN tools_core.game_servers.slug IS
    'Nom du dossier NAS et clé d''upsert du manifest.';
COMMENT ON COLUMN tools_core.game_servers.game_code IS
    'Code d''identité du jeu pour l''affichage et le regroupement.';
COMMENT ON COLUMN tools_core.game_servers.protocol_type IS
    'Protocole qui sélectionne l''adapter de poll (STEAM_A2S, PALWORLD_REST, SOURCE_RCON…).';
COMMENT ON COLUMN tools_core.game_servers.server_name IS
    'Nom de cette instance de serveur, fourni par le manifest.';
COMMENT ON COLUMN tools_core.game_servers.steam_app_id IS
    'Identifiant Steam optionnel utilisé pour enrichir les métadonnées du jeu.';
COMMENT ON COLUMN tools_core.game_servers.game_name IS
    'Nom officiel du jeu obtenu depuis Steam.';
COMMENT ON COLUMN tools_core.game_servers.picture_url IS
    'URL de l''image du jeu, issue d''un fichier local ou de Steam.';
COMMENT ON COLUMN tools_core.game_servers.host IS
    'Hôte ou adresse IP utilisée par le poll.';
COMMENT ON COLUMN tools_core.game_servers.port IS
    'Port de poll ; il peut différer du port de jeu des joueurs.';
COMMENT ON COLUMN tools_core.game_servers.protocol_config IS
    'Configuration propre au protocole, notamment credentials et valeurs d''override.';
COMMENT ON COLUMN tools_core.game_servers.is_visible IS
    'Contrôle l''affichage du serveur dans le dashboard sans arrêter sa gestion technique.';
COMMENT ON COLUMN tools_core.game_servers.last_synced_at IS
    'Date du dernier manifest reçu par le flux de synchronisation.';
COMMENT ON COLUMN tools_core.game_servers.online IS
    'Dernier statut de disponibilité déterminé par le poll ; NULL avant son premier passage.';
COMMENT ON COLUMN tools_core.game_servers.num_players IS
    'Nombre de joueurs observé lors du dernier poll.';
COMMENT ON COLUMN tools_core.game_servers.max_players IS
    'Nombre maximal de joueurs observé ou déterminé par le protocole lors du dernier poll.';
COMMENT ON COLUMN tools_core.game_servers.checked_at IS
    'Date du dernier poll ayant écrit le statut.';
COMMENT ON COLUMN tools_core.game_servers.created_at IS
    'Date de création de la ligne en base.';
