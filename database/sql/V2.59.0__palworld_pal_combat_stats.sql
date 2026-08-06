ALTER TABLE tools_palworld.pal_instance
    ADD COLUMN rank INTEGER NOT NULL DEFAULT 0,
    ADD COLUMN iv_hp INTEGER,
    ADD COLUMN iv_attack INTEGER,
    ADD COLUMN iv_defense INTEGER,
    ADD COLUMN current_hp NUMERIC,
    ADD COLUMN base_hp INTEGER,
    ADD COLUMN base_melee_attack INTEGER,
    ADD COLUMN base_shot_attack INTEGER,
    ADD COLUMN base_defense INTEGER,
    ADD COLUMN base_support INTEGER,
    ADD COLUMN base_craft_speed INTEGER;

COMMENT ON COLUMN tools_palworld.pal_instance.rank IS 'Rang de condensation du Pal (étoiles), distinct de favorite_index';
COMMENT ON COLUMN tools_palworld.pal_instance.current_hp IS 'PV actuels du Pal, pas les PV maximums';
