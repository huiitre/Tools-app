-- ajout de la colonne game_version_id
ALTER TABLE tools_dofus.workshop_tag 
ADD COLUMN game_version_id BIGINT NOT NULL;

COMMENT ON COLUMN tools_dofus.workshop_tag.game_version_id IS 'ID de la version du jeu';

ALTER TABLE tools_dofus.workshop_tag
ADD CONSTRAINT fk_workshop_tag_game_version
FOREIGN KEY (game_version_id) REFERENCES tools_dofus.game_version(id);

-- modification de la contrainte unique pour inclure game_version_id
ALTER TABLE tools_dofus.workshop_tag
DROP CONSTRAINT workshop_tag_user_id_name_key;

ALTER TABLE tools_dofus.workshop_tag
ADD CONSTRAINT workshop_tag_user_game_name_unique
UNIQUE (user_id, game_version_id, name);

ALTER TABLE tools_dofus.workshop
ADD CONSTRAINT workshop_game_version_user_name_unique 
UNIQUE(game_version_id, user_id, name);

-- rename ingredient_id vers item_id pour plus de clarté
ALTER TABLE tools_dofus.workshop_item_ingredient RENAME COLUMN ingredient_id TO item_id;