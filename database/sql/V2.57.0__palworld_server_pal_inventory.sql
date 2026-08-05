ALTER TABLE tools_palworld.pal_instance
    ALTER COLUMN base_id DROP NOT NULL,
    ADD COLUMN storage_location VARCHAR(20) NOT NULL DEFAULT 'base',
    ADD COLUMN container_id UUID,
    ADD COLUMN gender VARCHAR(10),
    ADD COLUMN favorite_index INTEGER,
    ADD COLUMN passive_skill_ids TEXT[] NOT NULL DEFAULT '{}',
    ADD COLUMN is_present BOOLEAN NOT NULL DEFAULT TRUE;

COMMENT ON COLUMN tools_palworld.pal_instance.base_id IS 'Base actuelle lorsque le Pal est affecté à une base ; NULL pour un Pal en Palbox ou dans une équipe';
COMMENT ON COLUMN tools_palworld.pal_instance.storage_location IS 'Emplacement courant extrait du serveur : base, palbox ou party';
COMMENT ON COLUMN tools_palworld.pal_instance.container_id IS 'UUID natif du conteneur de stockage ou de l''équipe';
COMMENT ON COLUMN tools_palworld.pal_instance.gender IS 'Genre natif extrait du serveur : male ou female';
COMMENT ON COLUMN tools_palworld.pal_instance.favorite_index IS 'Position de favori native du serveur, NULL si le Pal ne l''est pas';
COMMENT ON COLUMN tools_palworld.pal_instance.passive_skill_ids IS 'Identifiants techniques des passifs du Pal, dans l''ordre du snapshot';
COMMENT ON COLUMN tools_palworld.pal_instance.is_present IS 'FALSE quand le Pal n''est plus présent dans le dernier snapshot complet traité';

CREATE INDEX idx_pal_instance_present_pal ON tools_palworld.pal_instance(is_present, pal_id);
CREATE INDEX idx_pal_instance_owner_present ON tools_palworld.pal_instance(owner_player_uid, is_present);
