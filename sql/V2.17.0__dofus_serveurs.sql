-- ajout des serveurs
ALTER TABLE tools_dofus.game_server
ADD COLUMN name TEXT NOT NULL,
ADD COLUMN code TEXT NOT NULL;

INSERT INTO tools_dofus.game_server (game_version_id, name, code)
VALUES
    ((select id from tools_dofus.game_version where code = 'dofus3'), 'Salar', 'salar'),
    ((select id from tools_dofus.game_version where code = 'dofus3'), 'Brial', 'brial'),
    ((select id from tools_dofus.game_version where code = 'dofus3'), 'Rafal', 'rafal');