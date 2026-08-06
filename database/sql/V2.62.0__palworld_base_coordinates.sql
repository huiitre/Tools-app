ALTER TABLE tools_palworld.base
    ADD COLUMN position_x DOUBLE PRECISION,
    ADD COLUMN position_y DOUBLE PRECISION,
    ADD COLUMN position_z DOUBLE PRECISION,
    ADD COLUMN rotation_x DOUBLE PRECISION,
    ADD COLUMN rotation_y DOUBLE PRECISION,
    ADD COLUMN rotation_z DOUBLE PRECISION,
    ADD COLUMN rotation_w DOUBLE PRECISION,
    ADD COLUMN area_range DOUBLE PRECISION;

COMMENT ON COLUMN tools_palworld.base.position_x IS 'Position monde extraite du snapshot serveur';
COMMENT ON COLUMN tools_palworld.base.position_y IS 'Position monde extraite du snapshot serveur';
COMMENT ON COLUMN tools_palworld.base.position_z IS 'Position monde extraite du snapshot serveur';
COMMENT ON COLUMN tools_palworld.base.area_range IS 'Rayon de la zone de base extrait du snapshot serveur';
