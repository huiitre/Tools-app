-- Informations libres d'un serveur de jeu, affichées sous son nom dans le dashboard.
--
-- Déclarées par le manifest du serveur (tableau de { label, value }) et publiées par l'extractor.
-- Rien n'y est propre à un jeu : versions Minecraft/NeoForge, empreinte AutoModpack, carte Ark…
-- L'ordre du tableau est l'ordre d'affichage.

ALTER TABLE tools_core.game_servers
ADD COLUMN infos JSONB NOT NULL DEFAULT '[]';

COMMENT ON COLUMN tools_core.game_servers.infos IS
    'Informations affichées sous le nom du serveur, tableau ordonné de { label, value } ; vide si le manifest n''en déclare pas.';
