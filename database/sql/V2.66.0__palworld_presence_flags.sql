-- Marque les bases, guildes et joueurs disparus du serveur.
--
-- L'import upsertait ces trois tables sans jamais signaler les entités absentes du snapshot :
-- une base détruite en jeu restait affichée sur la carte et dans la liste, indéfiniment. Les
-- pals disposaient déjà de ce mécanisme (pal_instance.is_present), pas leurs conteneurs.
--
-- Marquer plutôt que supprimer : pal_instance_snapshot référence base_id pour tout l'historique
-- des pals, y compris ceux qui vivent aujourd'hui ailleurs. Une suppression effacerait leur
-- passé sans rien apporter à l'écran.

ALTER TABLE tools_palworld.base
ADD COLUMN is_present BOOLEAN NOT NULL DEFAULT TRUE;

ALTER TABLE tools_palworld.guild
ADD COLUMN is_present BOOLEAN NOT NULL DEFAULT TRUE;

ALTER TABLE tools_palworld.player
ADD COLUMN is_present BOOLEAN NOT NULL DEFAULT TRUE;

COMMENT ON COLUMN tools_palworld.base.is_present IS
'FALSE lorsque la base était absente du dernier snapshot importé : détruite en jeu.';

COMMENT ON COLUMN tools_palworld.guild.is_present IS
'FALSE lorsque la guilde était absente du dernier snapshot importé : dissoute.';

COMMENT ON COLUMN tools_palworld.player.is_present IS
'FALSE lorsque le joueur était absent du dernier snapshot importé : personnage supprimé.';

-- Reprise de l'existant. Le repère est la date d'extraction du dernier import, et non now() :
-- si aucun snapshot n'est arrivé depuis plusieurs jours (serveur éteint, extracteur arrêté),
-- comparer à l'heure courante viderait la carte de tout son contenu.
--
-- COALESCE couvre la base neuve, encore sans aucun import : rien n'est alors marqué absent.
UPDATE tools_palworld.base
SET is_present = FALSE
WHERE last_seen_at < COALESCE(
    (SELECT MAX(extracted_at) FROM tools_palworld.server_snapshot_import),
    last_seen_at);

UPDATE tools_palworld.guild
SET is_present = FALSE
WHERE last_seen_at < COALESCE(
    (SELECT MAX(extracted_at) FROM tools_palworld.server_snapshot_import),
    last_seen_at);

UPDATE tools_palworld.player
SET is_present = FALSE
WHERE last_seen_at < COALESCE(
    (SELECT MAX(extracted_at) FROM tools_palworld.server_snapshot_import),
    last_seen_at);

-- Les lectures ne renvoient que les entités présentes : l'index partiel leur évite un scan
-- complet, dans l'esprit de ceux posés par V2.61.0.
CREATE INDEX idx_base_present ON tools_palworld.base (guild_id) WHERE is_present;
CREATE INDEX idx_guild_present ON tools_palworld.guild (guild_id) WHERE is_present;
CREATE INDEX idx_player_present ON tools_palworld.player (guild_id) WHERE is_present;
