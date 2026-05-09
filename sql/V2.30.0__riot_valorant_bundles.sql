CREATE TABLE tools_riot.valorant_bundles (
    id BIGSERIAL PRIMARY KEY,
    asset_id UUID NOT NULL,
    name VARCHAR(255) NOT NULL,
    banner_url TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT now(),
    updated_at TIMESTAMP NOT NULL DEFAULT now(),

    CONSTRAINT uq_valorant_bundles_asset_id UNIQUE (asset_id)
);

COMMENT ON TABLE tools_riot.valorant_bundles IS 'Catalogue local des bundles Valorant (cache de valorant-api.com)';
COMMENT ON COLUMN tools_riot.valorant_bundles.id IS 'Identifiant interne unique';
COMMENT ON COLUMN tools_riot.valorant_bundles.asset_id IS 'UUID original provenant de l''API Valorant';
COMMENT ON COLUMN tools_riot.valorant_bundles.name IS 'Nom du bundle (displayName)';
COMMENT ON COLUMN tools_riot.valorant_bundles.banner_url IS 'URL du banner stocké sur assets.tools.huiitre.fr (source: displayIcon2)';
COMMENT ON COLUMN tools_riot.valorant_bundles.created_at IS 'Date de création de l''entrée en base';
COMMENT ON COLUMN tools_riot.valorant_bundles.updated_at IS 'Date de dernière mise à jour des données';
