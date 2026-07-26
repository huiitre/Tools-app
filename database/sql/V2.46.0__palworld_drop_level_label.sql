-- Complément de modélisation sur pal_drop (V2.43.0).
--
-- Les probabilités scrapées portent parfois un préfixe de palier
-- (ex: "Lv.80 25%"), correspondant aux stats de drop données par paldb.cc
-- pour un niveau de référence (Lv.10 à Lv.80). Plus de 60% des lignes de
-- drop (1053/1643) portent ce préfixe : ce n'est pas un cas marginal,
-- l'information doit être conservée plutôt que d'être tronquée en ne
-- gardant que le pourcentage.
ALTER TABLE tools_palworld.pal_drop ADD COLUMN level_label VARCHAR(20);

COMMENT ON COLUMN tools_palworld.pal_drop.level_label IS 'Palier de référence du taux de drop si présent (ex: "Lv.80"), NULL si la source ne précise pas de palier';
