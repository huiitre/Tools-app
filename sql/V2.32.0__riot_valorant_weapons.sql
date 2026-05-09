CREATE TABLE tools_riot.valorant_weapons (
    id BIGSERIAL PRIMARY KEY,
    asset_id UUID NOT NULL,
    name VARCHAR(255) NOT NULL,
    category VARCHAR(100),
    default_skin_asset_id UUID,
    display_icon_url TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT now(),
    updated_at TIMESTAMP NOT NULL DEFAULT now(),

    CONSTRAINT uq_valorant_weapons_asset_id UNIQUE (asset_id)
);

COMMENT ON TABLE tools_riot.valorant_weapons IS 'Catalogue des armes Valorant';
COMMENT ON COLUMN tools_riot.valorant_weapons.id IS 'Identifiant interne unique';
COMMENT ON COLUMN tools_riot.valorant_weapons.asset_id IS 'UUID original provenant de l''API Valorant';
COMMENT ON COLUMN tools_riot.valorant_weapons.name IS 'Nom de l''arme (displayName)';
COMMENT ON COLUMN tools_riot.valorant_weapons.category IS 'Catégorie de l''arme (ex: EEquippableCategory::Heavy)';
COMMENT ON COLUMN tools_riot.valorant_weapons.default_skin_asset_id IS 'UUID du skin par défaut de l''arme (defaultSkinUuid), non FK pour éviter la référence circulaire';
COMMENT ON COLUMN tools_riot.valorant_weapons.display_icon_url IS 'URL de l''icône stockée sur assets.tools.huiitre.fr';
COMMENT ON COLUMN tools_riot.valorant_weapons.created_at IS 'Date de création de l''entrée en base';
COMMENT ON COLUMN tools_riot.valorant_weapons.updated_at IS 'Date de dernière mise à jour des données';

ALTER TABLE tools_riot.valorant_weapon_skins
    ADD COLUMN weapon_id BIGINT,
    ADD CONSTRAINT fk_valorant_weapon_skins_weapon
        FOREIGN KEY (weapon_id)
        REFERENCES tools_riot.valorant_weapons(id)
        ON DELETE CASCADE;

COMMENT ON COLUMN tools_riot.valorant_weapon_skins.weapon_id IS 'Référence vers l''arme parente';
COMMENT ON COLUMN tools_riot.valorant_weapon_skins.tier_uuid IS 'UUID de la collection/thème du skin (themeUuid), ex: Altitude, Glitchpop';
