-- Complément de modélisation sur pal (V2.43.0).
--
-- pal_instance.full_stomach (snapshot serveur) est une valeur brute de faim,
-- pas déjà un pourcentage : sa capacité maximale varie par espèce de Pal
-- (stat "Quantité de nourriture" côté paldb.cc, ex: 150 pour Lamball).
-- Sans cette capacité max, impossible de calculer un pourcentage de faim
-- correct (on affichait des valeurs du style 300%).
ALTER TABLE tools_palworld.pal ADD COLUMN food_amount INTEGER;

COMMENT ON COLUMN tools_palworld.pal.food_amount IS 'Capacité de faim maximale de l''espèce (stat "Quantité de nourriture"). full_stomach / food_amount * 100 = pourcentage de faim réel';
