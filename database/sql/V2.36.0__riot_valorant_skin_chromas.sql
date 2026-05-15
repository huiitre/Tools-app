CREATE TABLE tools_riot.valorant_skin_chromas (
    id BIGSERIAL PRIMARY KEY,
    skin_id BIGINT NOT NULL,
    asset_id UUID NOT NULL,
    chroma_index INT NOT NULL,
    name VARCHAR(255) NOT NULL,
    display_icon_url TEXT,
    full_render_url TEXT,
    swatch_url TEXT,
    streamed_video_url TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT now(),
    updated_at TIMESTAMP NOT NULL DEFAULT now(),

    CONSTRAINT uq_valorant_skin_chromas_asset_id UNIQUE (asset_id),

    CONSTRAINT fk_valorant_skin_chromas_skin
        FOREIGN KEY (skin_id)
        REFERENCES tools_riot.valorant_weapon_skins(id)
        ON DELETE CASCADE
);

COMMENT ON TABLE tools_riot.valorant_skin_chromas IS 'Chromas (variantes de couleur) de chaque skin Valorant';
COMMENT ON COLUMN tools_riot.valorant_skin_chromas.id IS 'Identifiant interne unique';
COMMENT ON COLUMN tools_riot.valorant_skin_chromas.skin_id IS 'Référence vers le skin parent';
COMMENT ON COLUMN tools_riot.valorant_skin_chromas.asset_id IS 'UUID original provenant de l''API Valorant';
COMMENT ON COLUMN tools_riot.valorant_skin_chromas.chroma_index IS 'Position du chroma dans le skin (0-based)';
COMMENT ON COLUMN tools_riot.valorant_skin_chromas.name IS 'Nom du chroma (displayName)';
COMMENT ON COLUMN tools_riot.valorant_skin_chromas.display_icon_url IS 'URL de l''icône stockée sur assets.tools.huiitre.fr (null si absent)';
COMMENT ON COLUMN tools_riot.valorant_skin_chromas.full_render_url IS 'URL du rendu complet du chroma (null si absent)';
COMMENT ON COLUMN tools_riot.valorant_skin_chromas.swatch_url IS 'URL de la pastille de couleur (null si absent)';
COMMENT ON COLUMN tools_riot.valorant_skin_chromas.streamed_video_url IS 'URL de la vidéo de preview (CDN Riot, nullable)';
COMMENT ON COLUMN tools_riot.valorant_skin_chromas.created_at IS 'Date de création de l''entrée en base';
COMMENT ON COLUMN tools_riot.valorant_skin_chromas.updated_at IS 'Date de dernière mise à jour des données';
