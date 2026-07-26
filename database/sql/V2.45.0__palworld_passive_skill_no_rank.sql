-- Correctif de modélisation sur pal_passive_skill (V2.43.0).
--
-- Le champ "rank" avait été inventé côté modèle (position dans le tableau
-- source) en supposant que les talents passifs avaient des paliers comme les
-- compétences de partenaire. Vérification sur la donnée réelle (ex: Cattiva) :
-- le même talent ("Lâche") apparaît 2 à 4 fois dans passiveSkills avec le
-- même nom et alternativement un tooltip rempli/vide — ce sont des doublons
-- de scraping paldb.cc, pas des paliers. Le dédoublonnage se fait donc côté
-- code d'import (distinct sur name+tooltip), plus besoin de "rank".
ALTER TABLE tools_palworld.pal_passive_skill DROP CONSTRAINT pal_passive_skill_pal_id_name_rank_key;
ALTER TABLE tools_palworld.pal_passive_skill DROP COLUMN rank;

COMMENT ON TABLE tools_palworld.pal_passive_skill IS 'Talents passifs d''un Pal. Pas de slug stable côté source, dédoublonnés (name+tooltip) à l''import';
