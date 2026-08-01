-- Complément de modélisation suite à l'évolution du scraping paldb.cc (2026-08-01) :
-- pals.json expose désormais, par aptitude de travail, le niveau final garanti à
-- 4 étoiles (maxLevel) et le détail de la jauge d'étoiles (starSegments/emptySegments),
-- ainsi qu'un indicateur de priorité par aptitude (isPriority). Le niveau réel actuel
-- (colonne "level") n'est PAS une échelle 1-4 (ex: Anubis a des niveaux 6 et 4) :
-- le commentaire posé en V2.43.0 était erroné, corrigé ici.
ALTER TABLE tools_palworld.pal_work_suitability
    ADD COLUMN max_level     SMALLINT,
    ADD COLUMN star_segments SMALLINT,
    ADD COLUMN empty_segments SMALLINT,
    ADD COLUMN is_priority   BOOLEAN NOT NULL DEFAULT false;

COMMENT ON COLUMN tools_palworld.pal_work_suitability.level          IS 'Niveau actuel réel de l''aptitude, valeur brute source (pas une échelle fixe 1-4)';
COMMENT ON COLUMN tools_palworld.pal_work_suitability.max_level      IS 'Niveau final garanti une fois le Pal condensé à fond (4 étoiles) — un résultat possible, pas une progression connue étoile par étoile';
COMMENT ON COLUMN tools_palworld.pal_work_suitability.star_segments  IS 'Somme cumulée des segments remplis sur les 4 étoiles (pas de détail par palier)';
COMMENT ON COLUMN tools_palworld.pal_work_suitability.empty_segments IS 'Somme cumulée des segments vides sur les 4 étoiles';
COMMENT ON COLUMN tools_palworld.pal_work_suitability.is_priority    IS 'Aptitude de travail prioritaire du Pal (équivalent de Others.BestWorkSuitability, porté ici par aptitude)';

-- Jauge de nourriture affichée par paldb.cc (10 icônes), distincte de la capacité
-- de faim brute déjà stockée dans pal.food_amount (Stats."Quantité de nourriture",
-- utilisée pour le calcul de pourcentage de faim serveur, cf. V2.48.0). Correspond au
-- champ déjà scrapé raw.Others.FoodAmount (jamais mappé jusqu'ici), désormais exposé
-- en top-level sous forme structurée { on, off, icon }.
ALTER TABLE tools_palworld.pal
    ADD COLUMN food_gauge_filled  SMALLINT,
    ADD COLUMN food_gauge_empty   SMALLINT,
    ADD COLUMN food_gauge_icon_url TEXT;

COMMENT ON COLUMN tools_palworld.pal.food_gauge_filled   IS 'Nombre d''icônes pleines dans la jauge de nourriture affichée (sur 10), distinct de food_amount';
COMMENT ON COLUMN tools_palworld.pal.food_gauge_empty    IS 'Nombre d''icônes vides dans la jauge de nourriture affichée (sur 10)';
COMMENT ON COLUMN tools_palworld.pal.food_gauge_icon_url IS 'URL de l''icône utilisée pour la jauge de nourriture (CDN)';
