-- host/port pilotent uniquement le poll interne (souvent une IP LAN, un port de statut
-- RCON/REST distinct du port de jeu). Le widget a besoin d'une adresse différente : celle à
-- laquelle un joueur se connecte réellement. client_host/client_port la portent, fournie par
-- le manifest au même titre que le reste.

ALTER TABLE tools_core.game_servers
ADD COLUMN client_host VARCHAR(255) DEFAULT 'games.huiitre.fr';

ALTER TABLE tools_core.game_servers
ADD COLUMN client_port INTEGER;

COMMENT ON COLUMN tools_core.game_servers.client_host IS
    'Hôte public affiché aux joueurs pour se connecter. Par défaut le DNS public du NAS ; peut être une IP LAN pour un environnement non exposé (ex. QA).';
COMMENT ON COLUMN tools_core.game_servers.client_port IS
    'Port de jeu public utilisé par les joueurs, distinct de port qui ne sert qu''au poll de statut.';
