-- Rattrapage : la colonne user_id de ces 3 tables aurait dû être supprimée dans V2.52.0
-- (devenue redondante depuis le passage à valorant_account_id), mais certains environnements
-- ont exécuté une version antérieure du script avant cette décision.

ALTER TABLE tools_riot.valorant_skin_watchlist DROP CONSTRAINT IF EXISTS fk_valorant_watchlist_user;
ALTER TABLE tools_riot.valorant_skin_watchlist DROP COLUMN IF EXISTS user_id;

ALTER TABLE tools_riot.valorant_user_skins DROP CONSTRAINT IF EXISTS fk_valorant_user_skins_user;
ALTER TABLE tools_riot.valorant_user_skins DROP COLUMN IF EXISTS user_id;

ALTER TABLE tools_riot.valorant_store_history DROP CONSTRAINT IF EXISTS fk_valorant_store_history_user;
ALTER TABLE tools_riot.valorant_store_history DROP COLUMN IF EXISTS user_id;
