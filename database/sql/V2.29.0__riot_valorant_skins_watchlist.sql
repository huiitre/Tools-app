CREATE SCHEMA IF NOT EXISTS tools_riot;

-- Table: valorant_weapon_skins
CREATE TABLE tools_riot.valorant_weapon_skins (
    id BIGSERIAL PRIMARY KEY,
    asset_id UUID NOT NULL,
    name VARCHAR(255) NOT NULL,
    icon_url TEXT,
    tier_uuid UUID,
    content_tier_uuid UUID,
    created_at TIMESTAMP NOT NULL DEFAULT now(),
    updated_at TIMESTAMP NOT NULL DEFAULT now(),

    CONSTRAINT uq_valorant_weapon_skins_asset_id UNIQUE (asset_id)
);

COMMENT ON TABLE tools_riot.valorant_weapon_skins IS 'Catalogue local des skins d''armes Valorant (cache de valorant-api.com)';
COMMENT ON COLUMN tools_riot.valorant_weapon_skins.id IS 'Identifiant interne unique';
COMMENT ON COLUMN tools_riot.valorant_weapon_skins.asset_id IS 'UUID original provenant de l''API Valorant';
COMMENT ON COLUMN tools_riot.valorant_weapon_skins.name IS 'Nom complet du skin (displayName)';
COMMENT ON COLUMN tools_riot.valorant_weapon_skins.icon_url IS 'URL de l''image du skin (utilisé pour le téléchargement local)';
COMMENT ON COLUMN tools_riot.valorant_weapon_skins.tier_uuid IS 'UUID du tier (amélioration/level)';
COMMENT ON COLUMN tools_riot.valorant_weapon_skins.content_tier_uuid IS 'UUID de la rareté commerciale (Select, Premium, etc.)';
COMMENT ON COLUMN tools_riot.valorant_weapon_skins.created_at IS 'Date de création de l''entrée en base';
COMMENT ON COLUMN tools_riot.valorant_weapon_skins.updated_at IS 'Date de dernière mise à jour des données';

-- Table: valorant_skin_watchlist
CREATE TABLE tools_riot.valorant_skin_watchlist (
    id BIGSERIAL PRIMARY KEY,
    user_id BIGINT NOT NULL,
    skin_id BIGINT NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT now(),

    CONSTRAINT fk_valorant_watchlist_user
        FOREIGN KEY (user_id)
        REFERENCES tools_core.users(id)
        ON DELETE CASCADE,

    CONSTRAINT fk_valorant_watchlist_skin
        FOREIGN KEY (skin_id)
        REFERENCES tools_riot.valorant_weapon_skins(id)
        ON DELETE CASCADE,

    CONSTRAINT uq_valorant_watchlist_user_skin
        UNIQUE (user_id, skin_id)
);

COMMENT ON TABLE tools_riot.valorant_skin_watchlist IS 'Skins suivis par les utilisateurs pour alertes boutique';
COMMENT ON COLUMN tools_riot.valorant_skin_watchlist.id IS 'Identifiant interne unique';
COMMENT ON COLUMN tools_riot.valorant_skin_watchlist.user_id IS 'ID de l''utilisateur propriétaire du suivi';
COMMENT ON COLUMN tools_riot.valorant_skin_watchlist.skin_id IS 'ID du skin suivi';
COMMENT ON COLUMN tools_riot.valorant_skin_watchlist.created_at IS 'Date d''ajout à la liste de suivi';

-- Table: valorant_user_skins
CREATE TABLE tools_riot.valorant_user_skins (
    id BIGSERIAL PRIMARY KEY,
    user_id BIGINT NOT NULL,
    skin_id BIGINT NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT now(),

    CONSTRAINT fk_valorant_user_skins_user
        FOREIGN KEY (user_id)
        REFERENCES tools_core.users(id)
        ON DELETE CASCADE,

    CONSTRAINT fk_valorant_user_skins_skin
        FOREIGN KEY (skin_id)
        REFERENCES tools_riot.valorant_weapon_skins(id)
        ON DELETE CASCADE,

    CONSTRAINT uq_valorant_user_skins_user_skin
        UNIQUE (user_id, skin_id)
);

COMMENT ON TABLE tools_riot.valorant_user_skins IS 'Skins possédés par les utilisateurs (pour filtrer la watchlist)';
COMMENT ON COLUMN tools_riot.valorant_user_skins.id IS 'Identifiant interne unique';
COMMENT ON COLUMN tools_riot.valorant_user_skins.user_id IS 'ID de l''utilisateur possédant le skin';
COMMENT ON COLUMN tools_riot.valorant_user_skins.skin_id IS 'ID du skin possédé';
COMMENT ON COLUMN tools_riot.valorant_user_skins.created_at IS 'Date d''acquisition ou d''enregistrement';

-- Table: valorant_store_history
CREATE TABLE tools_riot.valorant_store_history (
    id BIGSERIAL PRIMARY KEY,
    user_id BIGINT NOT NULL,
    skin_id BIGINT NOT NULL,
    seen_at DATE NOT NULL DEFAULT CURRENT_DATE,

    CONSTRAINT fk_valorant_store_history_user
        FOREIGN KEY (user_id)
        REFERENCES tools_core.users(id)
        ON DELETE CASCADE,

    CONSTRAINT fk_valorant_store_history_skin
        FOREIGN KEY (skin_id)
        REFERENCES tools_riot.valorant_weapon_skins(id)
        ON DELETE CASCADE,

    CONSTRAINT uq_valorant_store_history_user_skin_date
        UNIQUE (user_id, skin_id, seen_at)
);

COMMENT ON TABLE tools_riot.valorant_store_history IS 'Historique des rotations de la boutique par utilisateur';
COMMENT ON COLUMN tools_riot.valorant_store_history.id IS 'Identifiant interne unique';
COMMENT ON COLUMN tools_riot.valorant_store_history.user_id IS 'ID de l''utilisateur ayant vu le skin en boutique';
COMMENT ON COLUMN tools_riot.valorant_store_history.skin_id IS 'ID du skin apparu en boutique';
COMMENT ON COLUMN tools_riot.valorant_store_history.seen_at IS 'Date d''apparition en boutique';
