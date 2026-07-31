-- Passage d'un compte Valorant unique par utilisateur Tools à plusieurs comptes liés.
-- Décision assumée : pas de script de migration de données, on repart de zéro
-- (tokens et historique boutique actuels perdus, à relier/reconstituer manuellement).

DROP TABLE IF EXISTS tools_riot.valorant_auth;

CREATE TABLE tools_riot.valorant_account (
    id                  BIGSERIAL PRIMARY KEY,
    user_id             BIGINT NOT NULL,
    puuid               VARCHAR(255) NOT NULL,
    region              VARCHAR(10) NOT NULL,
    game_name           VARCHAR(255),
    tag_line            VARCHAR(16),
    label               VARCHAR(255),

    encrypted_refresh   TEXT NOT NULL,
    encryption_iv       VARCHAR(255) NOT NULL,

    expires_at          TIMESTAMP NOT NULL,
    created_at          TIMESTAMP NOT NULL DEFAULT now(),
    updated_at          TIMESTAMP NOT NULL DEFAULT now(),

    CONSTRAINT fk_valorant_account_user
        FOREIGN KEY (user_id)
        REFERENCES tools_core.users (id)
        ON DELETE CASCADE,

    CONSTRAINT uq_valorant_account_user_puuid
        UNIQUE (user_id, puuid)
);

COMMENT ON TABLE tools_riot.valorant_account IS 'Comptes Valorant liés à un utilisateur Tools (plusieurs comptes possibles par utilisateur)';
COMMENT ON COLUMN tools_riot.valorant_account.user_id IS 'ID de l''utilisateur Tools propriétaire';
COMMENT ON COLUMN tools_riot.valorant_account.puuid IS 'Player UUID unique de Riot Games';
COMMENT ON COLUMN tools_riot.valorant_account.region IS 'Région Riot (eu, na, ap, latam, br, kr)';
COMMENT ON COLUMN tools_riot.valorant_account.game_name IS 'Pseudo Riot ID (partie avant le #), résolu via le name-service Riot';
COMMENT ON COLUMN tools_riot.valorant_account.tag_line IS 'Tag Riot ID (partie après le #), résolu via le name-service Riot';
COMMENT ON COLUMN tools_riot.valorant_account.label IS 'Surnom optionnel donné par l''utilisateur pour différencier ses comptes';
COMMENT ON COLUMN tools_riot.valorant_account.encrypted_refresh IS 'Refresh Token chiffré (AES-GCM)';
COMMENT ON COLUMN tools_riot.valorant_account.encryption_iv IS 'IV aléatoire utilisé pour le chiffrement de cette ligne';
COMMENT ON COLUMN tools_riot.valorant_account.expires_at IS 'Date d''expiration du refresh token actuel';

CREATE INDEX idx_valorant_account_user ON tools_riot.valorant_account (user_id);

-- Watchlist, mes skins et historique boutique deviennent propres à chaque compte Valorant lié
-- (le rattachement à l'utilisateur Tools reste implicite via valorant_account.user_id).
-- On vide les tables existantes (perte assumée) avant d'ajouter la colonne NOT NULL.

TRUNCATE TABLE tools_riot.valorant_skin_watchlist;
ALTER TABLE tools_riot.valorant_skin_watchlist
    ADD COLUMN valorant_account_id BIGINT NOT NULL,
    ADD CONSTRAINT fk_valorant_watchlist_account
        FOREIGN KEY (valorant_account_id)
        REFERENCES tools_riot.valorant_account (id)
        ON DELETE CASCADE,
    DROP CONSTRAINT fk_valorant_watchlist_user,
    DROP CONSTRAINT uq_valorant_watchlist_user_skin,
    DROP COLUMN user_id,
    ADD CONSTRAINT uq_valorant_watchlist_account_skin UNIQUE (valorant_account_id, skin_id);

COMMENT ON COLUMN tools_riot.valorant_skin_watchlist.valorant_account_id IS 'Compte Valorant pour lequel ce skin est surveillé';

TRUNCATE TABLE tools_riot.valorant_user_skins;
ALTER TABLE tools_riot.valorant_user_skins
    ADD COLUMN valorant_account_id BIGINT NOT NULL,
    ADD CONSTRAINT fk_valorant_user_skins_account
        FOREIGN KEY (valorant_account_id)
        REFERENCES tools_riot.valorant_account (id)
        ON DELETE CASCADE,
    DROP CONSTRAINT fk_valorant_user_skins_user,
    DROP CONSTRAINT uq_valorant_user_skins_user_skin,
    DROP COLUMN user_id,
    ADD CONSTRAINT uq_valorant_user_skins_account_skin UNIQUE (valorant_account_id, skin_id);

COMMENT ON COLUMN tools_riot.valorant_user_skins.valorant_account_id IS 'Compte Valorant sur lequel ce skin est possédé';

TRUNCATE TABLE tools_riot.valorant_store_history;
ALTER TABLE tools_riot.valorant_store_history
    ADD COLUMN valorant_account_id BIGINT NOT NULL,
    ADD CONSTRAINT fk_valorant_store_history_account
        FOREIGN KEY (valorant_account_id)
        REFERENCES tools_riot.valorant_account (id)
        ON DELETE CASCADE,
    DROP CONSTRAINT fk_valorant_store_history_user,
    DROP CONSTRAINT uq_valorant_store_history_user_skin_date,
    DROP COLUMN user_id,
    ADD CONSTRAINT uq_valorant_store_history_account_skin_date UNIQUE (valorant_account_id, skin_id, seen_at);

COMMENT ON COLUMN tools_riot.valorant_store_history.valorant_account_id IS 'Compte Valorant sur lequel ce skin a été vu en boutique';
