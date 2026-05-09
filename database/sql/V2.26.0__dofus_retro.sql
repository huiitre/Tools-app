insert into tools_dofus.game_version (code, name) values ('retro', 'Dofus Retro');

INSERT INTO tools_dofus.game_server (game_version_id, name, code)
VALUES
    ((select id from tools_dofus.game_version where code = 'retro'), 'Fallanster', 'fallanster'),
    ((select id from tools_dofus.game_version where code = 'retro'), 'Allisteria', 'allisteria'),
    ((select id from tools_dofus.game_version where code = 'retro'), 'Boune', 'boune');