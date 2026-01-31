-- Area (zone géographique principale)
CREATE TABLE tools_dofus.area (
    id BIGSERIAL PRIMARY KEY,
    game_version_id BIGINT NOT NULL REFERENCES tools_dofus.game_version(id),
    asset_id BIGINT NOT NULL,
    name VARCHAR(255) NOT NULL,
    UNIQUE (asset_id, game_version_id)
);

COMMENT ON TABLE tools_dofus.area IS 'Zone géographique principale (ex: Île de Frigost, Amakna)';
COMMENT ON COLUMN tools_dofus.area.id IS 'Identifiant unique interne';
COMMENT ON COLUMN tools_dofus.area.game_version_id IS 'Version du jeu (Retro, 2.0, Touch, etc.)';
COMMENT ON COLUMN tools_dofus.area.asset_id IS 'ID issu de AreaData dans les assets Ankama';
COMMENT ON COLUMN tools_dofus.area.name IS 'Nom de la zone résolu depuis I18N (string FR)';

-- SubArea (sous-zone technique)
CREATE TABLE tools_dofus.subarea (
    id BIGSERIAL PRIMARY KEY,
    game_version_id BIGINT NOT NULL REFERENCES tools_dofus.game_version(id),
    asset_id BIGINT NOT NULL,
    area_id BIGINT NOT NULL REFERENCES tools_dofus.area(id),
    name VARCHAR(255) NOT NULL,
    UNIQUE (asset_id, game_version_id)
);

COMMENT ON TABLE tools_dofus.subarea IS 'Sous-zone technique Ankama (donjon, zone de farm, transporteur)';
COMMENT ON COLUMN tools_dofus.subarea.id IS 'Identifiant unique interne';
COMMENT ON COLUMN tools_dofus.subarea.game_version_id IS 'Version du jeu';
COMMENT ON COLUMN tools_dofus.subarea.asset_id IS 'ID issu de SubAreaData dans les assets Ankama';
COMMENT ON COLUMN tools_dofus.subarea.area_id IS 'Zone parente à laquelle appartient cette sous-zone';
COMMENT ON COLUMN tools_dofus.subarea.name IS 'Nom de la sous-zone résolu depuis I18N (string FR)';

-- Monster (monstre)
CREATE TABLE tools_dofus.monster (
    id BIGSERIAL PRIMARY KEY,
    game_version_id BIGINT NOT NULL REFERENCES tools_dofus.game_version(id),
    asset_id BIGINT NOT NULL,
    name VARCHAR(255) NOT NULL,
    UNIQUE (asset_id, game_version_id)
);

COMMENT ON TABLE tools_dofus.monster IS 'Monstre (pivot technique pour drops, zones de spawn, donjons)';
COMMENT ON COLUMN tools_dofus.monster.id IS 'Identifiant unique interne';
COMMENT ON COLUMN tools_dofus.monster.game_version_id IS 'Version du jeu';
COMMENT ON COLUMN tools_dofus.monster.asset_id IS 'ID issu de MonsterData dans les assets Ankama (data.id)';
COMMENT ON COLUMN tools_dofus.monster.name IS 'Nom du monstre résolu depuis I18N (string FR)';

-- Monster Image (images de monstre)
CREATE TABLE tools_dofus.monster_image (
    id BIGSERIAL PRIMARY KEY,
    monster_id BIGINT NOT NULL REFERENCES tools_dofus.monster(id),
    icon_id BIGINT NOT NULL,
    resolution VARCHAR(2) NOT NULL
);

COMMENT ON TABLE tools_dofus.monster_image IS 'Images des monstres (même logique que item_image)';
COMMENT ON COLUMN tools_dofus.monster_image.id IS 'Identifiant unique interne';
COMMENT ON COLUMN tools_dofus.monster_image.monster_id IS 'Monstre associé à cette image';
COMMENT ON COLUMN tools_dofus.monster_image.icon_id IS 'gfxId utilisé pour construire l''URL de l''image';
COMMENT ON COLUMN tools_dofus.monster_image.resolution IS 'Résolution de l''image (x1, x2)';

-- Monster SubArea (zones de spawn)
CREATE TABLE tools_dofus.monster_subarea (
    monster_id BIGINT NOT NULL REFERENCES tools_dofus.monster(id),
    subarea_id BIGINT NOT NULL REFERENCES tools_dofus.subarea(id),
    PRIMARY KEY (monster_id, subarea_id)
);

COMMENT ON TABLE tools_dofus.monster_subarea IS 'Zones de spawn des monstres';
COMMENT ON COLUMN tools_dofus.monster_subarea.monster_id IS 'Monstre qui spawn dans cette zone';
COMMENT ON COLUMN tools_dofus.monster_subarea.subarea_id IS 'Sous-zone où le monstre peut apparaître';

-- Monster Drop (drops de monstres)
CREATE TABLE tools_dofus.monster_drop (
    monster_id BIGINT NOT NULL REFERENCES tools_dofus.monster(id),
    item_id BIGINT NOT NULL REFERENCES tools_dofus.item(id),
    PRIMARY KEY (monster_id, item_id)
);

COMMENT ON TABLE tools_dofus.monster_drop IS 'Lien monstre → item droppé (sans taux de drop)';
COMMENT ON COLUMN tools_dofus.monster_drop.monster_id IS 'Monstre qui drop l''item';
COMMENT ON COLUMN tools_dofus.monster_drop.item_id IS 'Item droppé (objectId depuis MonsterData.drops)';

-- Index pour optimiser les requêtes fréquentes
CREATE INDEX idx_area_game_version ON tools_dofus.area(game_version_id);
CREATE INDEX idx_subarea_game_version ON tools_dofus.subarea(game_version_id);
CREATE INDEX idx_subarea_area ON tools_dofus.subarea(area_id);
CREATE INDEX idx_monster_game_version ON tools_dofus.monster(game_version_id);
CREATE INDEX idx_monster_subarea_monster ON tools_dofus.monster_subarea(monster_id);
CREATE INDEX idx_monster_subarea_subarea ON tools_dofus.monster_subarea(subarea_id);
CREATE INDEX idx_monster_drop_monster ON tools_dofus.monster_drop(monster_id);
CREATE INDEX idx_monster_drop_item ON tools_dofus.monster_drop(item_id);