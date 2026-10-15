-- Dernière sauvegarde d'un serveur de jeu, téléchargeable par les joueurs.
--
-- Déclarée par le manifest (motif de fichiers) et publiée par l'extractor, qui choisit la plus
-- récente sauvegarde terminée. Un jeu sans saveFile dans son manifest n'a rien à télécharger.

ALTER TABLE tools_core.game_servers
ADD COLUMN save_url VARCHAR(500);

ALTER TABLE tools_core.game_servers
ADD COLUMN save_size BIGINT;

ALTER TABLE tools_core.game_servers
ADD COLUMN save_date TIMESTAMPTZ;

COMMENT ON COLUMN tools_core.game_servers.save_url IS
    'URL publique de la dernière sauvegarde, versionnée par sa date ; NULL si le serveur n''en publie pas.';
COMMENT ON COLUMN tools_core.game_servers.save_size IS
    'Taille de la sauvegarde en octets, affichée avant le téléchargement.';
COMMENT ON COLUMN tools_core.game_servers.save_date IS
    'Date de fin d''écriture de la sauvegarde publiée.';
