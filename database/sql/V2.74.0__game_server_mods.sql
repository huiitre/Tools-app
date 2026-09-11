-- Mods d'un serveur de jeu et modpack que ses joueurs téléchargent.
--
-- Les deux sont déclarés par le manifest du serveur et publiés par l'extractor : la liste n'est
-- jamais lue sur le serveur de jeu lui-même. Un serveur sans mods n'a aucune ligne, et un jeu qui
-- installe ses mods à la connexion n'a pas de modpack.

ALTER TABLE tools_core.game_servers
ADD COLUMN modpack_url VARCHAR(500);

ALTER TABLE tools_core.game_servers
ADD COLUMN modpack_size BIGINT;

COMMENT ON COLUMN tools_core.game_servers.modpack_url IS
    'URL publique du modpack client, versionnée par son hash ; NULL si les joueurs n''ont rien à télécharger.';
COMMENT ON COLUMN tools_core.game_servers.modpack_size IS
    'Taille du modpack en octets, affichée avant le téléchargement.';

-- Les champs viennent d'un export d'outil tiers (Prism Launcher pour Minecraft) : aucune longueur
-- n'y est garantie, d'où TEXT plutôt qu'un VARCHAR qui ferait échouer le sync entier.
CREATE TABLE tools_core.game_server_mods (
    id BIGSERIAL PRIMARY KEY,
    game_server_id BIGINT NOT NULL,
    position INT NOT NULL,
    name TEXT NOT NULL,
    version TEXT,
    url TEXT,
    authors TEXT[] NOT NULL DEFAULT '{}',
    file_name TEXT,
    icon_url TEXT,

    CONSTRAINT fk_game_server_mods_server
        FOREIGN KEY (game_server_id) REFERENCES tools_core.game_servers (id) ON DELETE CASCADE,

    -- La position est l'ordre du fichier de mods : elle garde l'affichage stable d'un sync à l'autre.
    CONSTRAINT uq_game_server_mods_position UNIQUE (game_server_id, position)
);

COMMENT ON TABLE tools_core.game_server_mods IS
    'Mods d''un serveur de jeu, réécrits par le sync quand la liste du manifest change.';
COMMENT ON COLUMN tools_core.game_server_mods.position IS
    'Rang du mod dans le fichier de mods du manifest.';
COMMENT ON COLUMN tools_core.game_server_mods.url IS
    'Page du mod chez son hébergeur ; elle sert aussi à retrouver son icône.';
COMMENT ON COLUMN tools_core.game_server_mods.file_name IS
    'Nom du fichier du mod, quand le jeu en a un.';
COMMENT ON COLUMN tools_core.game_server_mods.icon_url IS
    'Icône posée dans le manifest, sinon celle de l''hébergeur ; résolue une seule fois par URL de mod.';
