-- =========================================================================
-- MODULE PALWORLD : bascule de pal.json vers la source pak
-- =========================================================================
-- pal.json (référence Pals) est désormais extrait directement du .pak du
-- serveur dédié Palworld, plus le scraper paldb.cc. Ce format n'a plus de
-- notion de variantes/zones de spawn textuelles (remplacées à terme par des
-- données de coordonnées réelles, cf. spawner_placements/wild_spawners/
-- boss_spawns, module séparé) ni d'egg_type. image_url/description/
-- paldex_suffix/food_amount restent en base mais ne sont plus alimentées
-- par le sync (aucun équivalent côté pak) : elles deviennent "sticky",
-- conservées telles quelles pour les Pals déjà connus.
-- =========================================================================

ALTER TABLE tools_palworld.pal RENAME COLUMN gold_coin TO price;
COMMENT ON COLUMN tools_palworld.pal.price IS 'Prix marchand (pak). Anciennement gold_coin (or lâché au K.O., scraper) — sémantique à reconfirmer, remplacé tel quel';

ALTER TABLE tools_palworld.pal DROP COLUMN egg_type;

DROP TABLE tools_palworld.pal_variant;
DROP TABLE tools_palworld.pal_spawn_zone;

COMMENT ON COLUMN tools_palworld.pal.image_url    IS 'URL du portrait (CDN). Non alimentée par le sync pak — conservée telle quelle depuis le dernier import scraper';
COMMENT ON COLUMN tools_palworld.pal.description  IS 'Non alimentée par le sync pak — conservée telle quelle depuis le dernier import scraper';
COMMENT ON COLUMN tools_palworld.pal.food_amount  IS 'Non alimentée par le sync pak — conservée telle quelle depuis le dernier import scraper';
