CREATE TABLE tools_riot.valorant_skin_levels (
    id BIGSERIAL PRIMARY KEY,
    skin_id BIGINT NOT NULL,
    asset_id UUID NOT NULL,
    level_index INT NOT NULL,
    name VARCHAR(255) NOT NULL,
    level_item VARCHAR(255),
    display_icon_url TEXT,
    streamed_video_url TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT now(),
    updated_at TIMESTAMP NOT NULL DEFAULT now(),

    CONSTRAINT uq_valorant_skin_levels_asset_id UNIQUE (asset_id),

    CONSTRAINT fk_valorant_skin_levels_skin
        FOREIGN KEY (skin_id)
        REFERENCES tools_riot.valorant_weapon_skins(id)
        ON DELETE CASCADE
);

COMMENT ON TABLE tools_riot.valorant_skin_levels IS 'Niveaux (levels) de chaque skin Valorant';
COMMENT ON COLUMN tools_riot.valorant_skin_levels.id IS 'Identifiant interne unique';
COMMENT ON COLUMN tools_riot.valorant_skin_levels.skin_id IS 'Référence vers le skin parent';
COMMENT ON COLUMN tools_riot.valorant_skin_levels.asset_id IS 'UUID original provenant de l''API Valorant';
COMMENT ON COLUMN tools_riot.valorant_skin_levels.level_index IS 'Position du level dans le skin (0-based)';
COMMENT ON COLUMN tools_riot.valorant_skin_levels.name IS 'Nom du level (displayName)';
COMMENT ON COLUMN tools_riot.valorant_skin_levels.level_item IS 'Type d''amélioration du level (ex: SoundEffects, Animation, Finisher)';
COMMENT ON COLUMN tools_riot.valorant_skin_levels.display_icon_url IS 'URL de l''icône stockée sur assets.tools.huiitre.fr (null si absent)';
COMMENT ON COLUMN tools_riot.valorant_skin_levels.streamed_video_url IS 'URL de la vidéo de preview (CDN Riot, nullable)';
COMMENT ON COLUMN tools_riot.valorant_skin_levels.created_at IS 'Date de création de l''entrée en base';
COMMENT ON COLUMN tools_riot.valorant_skin_levels.updated_at IS 'Date de dernière mise à jour des données';
