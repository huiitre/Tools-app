CREATE TABLE tools_riot.valorant_content_tiers (
    id BIGSERIAL PRIMARY KEY,
    asset_id UUID NOT NULL,
    name VARCHAR(255) NOT NULL,
    dev_name VARCHAR(100) NOT NULL,
    rank INT NOT NULL,
    juice_value INT NOT NULL,
    juice_cost INT NOT NULL,
    highlight_color VARCHAR(20),
    display_icon_url TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT now(),
    updated_at TIMESTAMP NOT NULL DEFAULT now(),

    CONSTRAINT uq_valorant_content_tiers_asset_id UNIQUE (asset_id)
);

COMMENT ON TABLE tools_riot.valorant_content_tiers IS 'Niveaux de rareté des skins Valorant (Content Tiers)';
COMMENT ON COLUMN tools_riot.valorant_content_tiers.id IS 'Identifiant interne unique';
COMMENT ON COLUMN tools_riot.valorant_content_tiers.asset_id IS 'UUID original provenant de l''API Valorant (= contentTierUuid dans les skins)';
COMMENT ON COLUMN tools_riot.valorant_content_tiers.name IS 'Nom affiché de la rareté (ex: Édition Premium)';
COMMENT ON COLUMN tools_riot.valorant_content_tiers.dev_name IS 'Nom technique (ex: Premium, Ultra, Select)';
COMMENT ON COLUMN tools_riot.valorant_content_tiers.rank IS 'Rang de rareté (0 = Select, 4 = Ultra)';
COMMENT ON COLUMN tools_riot.valorant_content_tiers.juice_value IS 'Valeur juice (usage interne Riot)';
COMMENT ON COLUMN tools_riot.valorant_content_tiers.juice_cost IS 'Coût juice (usage interne Riot)';
COMMENT ON COLUMN tools_riot.valorant_content_tiers.highlight_color IS 'Couleur de mise en avant (RGBA hex, ex: d1548d33)';
COMMENT ON COLUMN tools_riot.valorant_content_tiers.display_icon_url IS 'URL de l''icône stockée sur assets.tools.huiitre.fr';
COMMENT ON COLUMN tools_riot.valorant_content_tiers.created_at IS 'Date de création de l''entrée en base';
COMMENT ON COLUMN tools_riot.valorant_content_tiers.updated_at IS 'Date de dernière mise à jour des données';